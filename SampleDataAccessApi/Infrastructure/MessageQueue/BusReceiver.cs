using System.Text.Json;
using Azure.Messaging.ServiceBus;
using NuGet.SampleSharedModels.DTO;

public class BusReceiver: IBusReceiver
{
    private readonly ServiceBusReceiver _receiver;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<BusReceiver> _logger;
    private readonly IConfiguration _config;

    public BusReceiver(
        ServiceBusReceiver receiver,
        JsonSerializerOptions jsonOptions,
        IConfiguration config,
        ILogger<BusReceiver> logger)
    {
        _receiver = receiver;
        _jsonOptions = jsonOptions;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Liest einen einzelnen Bericht aus der Warteschlange (Long-Poll), 
    /// deserialisiert ihn und leitet die Aktion an die Handler weiter.
    /// </summary>
    /// This method internally implements a long polling
    /// It uses a TryTimeout, which is a property of the Azure Service Bus
    /// If no TryTimeout is used the whole system works like a polling. Periodically the program checks if there is a new message. This leads to unnecessary requests.
    ///  
    /// When the TryTimeout is set (Long Polling) if there is no message there with this timeframe the connections are left open
    /// Then the server blocks the calls until the timeout has expired or until a new message has arrived.
    /// So the what happens I guess is that the server blocks the calls of the client and only accept them when there is anew record or when a timeout has expired.
    public async Task ReceiveAsync<T>(
        // Delegate = function stored in a variable 
        // Func<T?, Task>: Function pass as a variable = Delegate with input parameter and retruns a Task
        Func<T?, Task> onUpdate, // expects a delegate a function that returns a value an not an action that returns nothing. We are passing as a parameter what the code will be executing when an update happens
        Func<T?, Task> onDelete,     // ditto for delete
        Func<T?, Task> onAdd,            // ditto for add
        TimeSpan? longPollTimeout = null, //
        CancellationToken cancellationToken = default)
    {
        // ── 1. Empfangen (Long-Polling via tryTimeout) ────────────────
        var timeout = longPollTimeout ?? TimeSpan.FromSeconds(30);

        ServiceBusReceivedMessage? message;
        try
        {
            message = await _receiver.ReceiveMessageAsync(timeout, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var queueName = _config["ServiceBus:QueueName"];
            _logger.LogError(ex, "Fehler beim Empfangen aus '{Queue}'", queueName);
            throw;
        }

        if (message == null)
            return; // Timeout ohne Message → Schleife im Hintergrund-Service macht weiter

        // ── 2. Phase 1: Deserialisieren (nicht reparabel bei Fehler) ──
        BusPayload<T>? payload;
        try
        {
            payload = DeserializePayload<T>(message);
            if (payload == null)
            {
                // Leere/unbrauchbare Payload → verwerfen, damit Queue nicht zustopft
                await _receiver.CompleteMessageAsync(message, cancellationToken);
                return;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Ungültiges JSON für MessageId '{MessageId}'. → Dead Letter.",
                message.MessageId);
            await _receiver.DeadLetterMessageAsync(message,
                deadLetterReason: "InvalidPayload",
                deadLetterErrorDescription: ex.Message,
                cancellationToken: cancellationToken);
            return;
        }

        // ── 3. Phase 2: Geschäftslogik (transiente Fehler → Abandon/Retry) ──
        try
        {
            switch (payload.Action)
            {
                case MessageAction.Add:
                    
                    if (payload.Data == null)
                    {
                        _logger.LogWarning("Add : Data is null. Ignoriere.");
                        break;
                    }
                    await onAdd(payload.Data);
                    break;

                case MessageAction.Update:
                    var recordToUpdate = payload.Data as MovieDTORead;
                    if (recordToUpdate == null || recordToUpdate.Id == null )
                    {
                        _logger.LogWarning("Update ohne RecordId/Data. Ignoriere.");
                        break;
                    }
                    await onUpdate(payload.Data);
                    break;

                case MessageAction.Delete:
                    var recordToDelete = payload.Data as MovieDTORead;
                    if (recordToDelete == null || recordToDelete.Id == null)
                    {
                        _logger.LogWarning("Delete ohne RecordId. Ignoriere.");
                        break;
                    }
                    await onDelete(payload.Data);
                    break;

                case MessageAction.None:
                default:
                    _logger.LogInformation("Aktion 'None' für RecordId. Ignoriere.");
                    break;
            }

            // Erfolgreich → Acknowledge
            await _receiver.CompleteMessageAsync(message, cancellationToken);
            _logger.LogInformation(
                "Bericht verarbeitet. Action={Action}, MessageId={MessageId}",
                payload.Action, message.MessageId);
        }
        catch (OperationCanceledException)
        {
            // Shutdown → Lock läuft aus, Message kommt automatisch zurück
            _logger.LogInformation("Shutdown während Verarbeitung MessageId '{MessageId}'.",
                message.MessageId);
            throw;
        }
        catch (Exception ex)
        {
            // Transienter Fehler (DB-Timeout, HTTP etc.) → Retry
            _logger.LogError(ex,
                "Verarbeitungsfehler MessageId '{MessageId}', DeliveryCount={Count}. → Abandon.",
                message.MessageId, message.ApplicationProperties.Count);
            await _receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
        }
    }

    private BusPayload<T>? DeserializePayload<T>(ServiceBusReceivedMessage message)
{
    if (message.Body.IsEmpty || message.Body.Length == 0)
    {
        _logger.LogWarning("Emty Payload. MessageId '{MessageId}'. Discard.",
            message.MessageId);
        return null;
    }

    try
    {
        // Option A: Explizit als String lesen (robusteste Methode bei Fehlern)
        string json = message.Body.ToString(); 
        
        // Debug-Log: Prüfen, ob das JSON wirklich ankommt
        _logger.LogDebug("Raw JSON: {Json}", json);

        var options = new JsonSerializerOptions(_jsonOptions)
        {
            PropertyNameCaseInsensitive = true, // Wichtig!
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        return JsonSerializer.Deserialize<BusPayload<T>>(json, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON Parse Error. Raw Content: {Content}", 
                message.Body.ToString());
            throw; // Wird oben gefangen -> DLQ
        }
    }
}

//TODO: this code needs to move somewhere else
public interface IBusReceiver
{
    //?? Func
    Task ReceiveAsync<T>(
        Func<T?, Task> onUpdate,
        Func<T?, Task> onDelete,          // RecordId wird durchgereicht
        Func<T?, Task> onAdd,
        TimeSpan? longPollTimeout = null,
        CancellationToken cancellationToken = default);
}
public enum MessageAction {Add, Update, Delete, None};

//This is the version to use not the one defined in sampleappapi.
public record BusPayload<T>
{
    public MessageAction Action { get; init; } = MessageAction.None;
    public T? Data { get; init; } // Data is always required. Always will be there
}


