using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Google.Api;
using NuGet.SampleSharedModels.DTO;
using NuGet.SampleSharedModels.DTO.ServiceBus;
using NuGet.SampleSharedModels.Enums.EnumsServiceBus;
using NuGet.SampleSharedModels.Interfaces.ServiceBus;
using SampleApi.Services.MovieServices;
using SampleDataAccessApi.Interfaces.MovieInterfaces;

public class BusReceiver: IBusReceiver
{
    private readonly ServiceBusReceiver _receiver;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<BusReceiver> _logger;
    private readonly IConfiguration _config;
    private readonly int _maxDeliveryCount;

    private readonly IBusRequestService _busRequestService;

    public BusReceiver(
        ServiceBusReceiver receiver,
        JsonSerializerOptions jsonOptions,
        IConfiguration config,
        IBusRequestService busRequestService,
        ILogger<BusReceiver> logger)
    {
        _receiver = receiver;
        _jsonOptions = jsonOptions;
        _config = config;
        _logger = logger;
        _busRequestService = busRequestService;
        // ✅ Robuster Zugriff mit Default
        _maxDeliveryCount = config.GetValue<int>("ServiceBus:MaxDeliveryCount", 10);
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
        Func<T?, Guid, Task> onUpdate, // expects a delegate a function that returns a value an not an action that returns nothing. We are passing as a parameter what the code will be executing when an update happens
        Func<T?, Guid, Task> onDelete,     // ditto for delete
        Func<T?, Guid, Task> onAdd,            // ditto for add
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
            _logger.LogError(ex, "Error when receiving from the queue '{Queue}'", queueName);
            throw;
        }

        if (message == null)
            return; // Timeout ohne Message → Schleife im Hintergrund-Service macht weiter

        // ── 2. Phase 1: Deserialisieren (nicht reparabel bei Fehler) ──
        BusPayloadDTO<T>? payload;
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
                "invalid JSON for MessageId '{MessageId}'. → Dead Letter.",
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
                        _logger.LogWarning("Add : Data is null for MessageId: {MessageId}. Ignoriere.", message.MessageId);
                        break;
                    }
                    await onAdd(payload.Data, Guid.Parse(message.MessageId));
                    break;

                case MessageAction.Update:
                    var recordToUpdate = payload.Data as MovieDTORead;
                    if (recordToUpdate == null || recordToUpdate.Id == null )
                    {
                        _logger.LogWarning("Update without is/Data for MessageId: {MessageId}. Ignored.", message.MessageId);
                        break;
                    }
                    await onUpdate(payload.Data, Guid.Parse(message.MessageId));
                    break;

                case MessageAction.Delete:
                    var recordToDelete = payload.Data as MovieDTORead;
                    if (recordToDelete == null || recordToDelete.Id == null)
                    {
                        _logger.LogWarning("Delete without id for MessageId: {MessageId}. Ignored.", message.MessageId);
                        break;
                    }
                    await onDelete(payload.Data, Guid.Parse(message.MessageId));
                    break;

                case MessageAction.None:
                default:
                    _logger.LogInformation("Aktion 'None' for MessageId: {MessageId}. Ignored.", message.MessageId);
                    break;
            }

            // Erfolgreich → Acknowledge
            await _receiver.CompleteMessageAsync(message, cancellationToken);
            _logger.LogInformation(
                "Message processed. Action={Action}, MessageId={MessageId}",
                payload.Action, message.MessageId);
        }
        catch (OperationCanceledException)
        {
            // Shutdown → Lock läuft aus, Message kommt automatisch zurück
            _logger.LogInformation("Shutdown while Processing MessageId '{MessageId}'.",
                message.MessageId);
            throw;
        }
        catch (Exception ex)
        {
            // Transienter Fehler (DB-Timeout, HTTP etc.) → Retry
            _logger.LogError(ex,
                "Error while processing MessageId '{MessageId}', DeliveryCount={Count}. → Abandon.",
                message.MessageId, message.ApplicationProperties.Count);
            if (message.DeliveryCount >= _maxDeliveryCount)
            {
                // Letzter Versuch fehlgeschlagen → Explizit in DLQ + Fail-Status setzen
                await MarkAsFailed(Guid.Parse(message.MessageId), ex.Message);
                await _receiver.DeadLetterMessageAsync(
                    message,
                    deadLetterReason: "MaxRetriesExceeded",
                    deadLetterErrorDescription: ex.Message,
                    cancellationToken: cancellationToken);
            }
            else
            {
                // Noch Versuche übrig → Zurück in Queue
                //Was AbandonMessageAsync tut
                //Lock sofort aufgehoben: Der Message-Lock wird augenblicklich freigegeben.
                //Nachricht bleibt in Queue: Die Nachricht geht zurück in die normale Warteschlange (nicht DLQ).
                //DeliveryCount erhöht: Service Bus zählt DeliveryCount hoch (z.B. von 1 auf 2).
                //Erneute Zustellung: Ein Consumer (derselbe oder ein anderer) bekommt die Nachricht erneut zugestellt – meist nach einer kurzen Backoff-Zeit.
                await _receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
                //Versuch N → DeliveryCount erreicht MaxDeliveryCount (Standard: 10) → Service Bus verschiebt AUTOMATISCH in die DLQ
                //Service Bus macht das automatisch! Du musst nicht selbst DeadLetterMessageAsync aufrufen, solange deine Queue mit einem konfigurierten MaxDeliveryCount läuft und EnableDeadLetteringOnMessageExpiration aktiv ist.
    
            }
        }
    }

    private async Task MarkAsFailed(Guid messageId, string exceptionMessage)
    {
        const int maxLength = 2000;
        // truncate the error message to max 2000 characters.
        // if it is over it the database could throuw an exception because we have limited the length to 2000 characters for performance reasons.
        if (exceptionMessage.Length > maxLength)
        {
            exceptionMessage = exceptionMessage[..(maxLength - 3)] + "...";
        }

        var serviceResult = await this._busRequestService.GetByMessageId(messageId);
        if (serviceResult.Success)
        {
            var busRequestDTO = serviceResult.Data;
            if (busRequestDTO != null)
            {
                busRequestDTO.MessageStatus = MessageStatus.Fail;
                busRequestDTO.ErrorMessage = exceptionMessage;
                busRequestDTO.UpdatedOn = DateTime.UtcNow;
                await this._busRequestService.Update(busRequestDTO);                
            }
        }
    }

    private BusPayloadDTO<T>? DeserializePayload<T>(ServiceBusReceivedMessage message)
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

            return JsonSerializer.Deserialize<BusPayloadDTO<T>>(json, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON Parse Error. Raw Content: {Content}", 
                message.Body.ToString());
            throw; // Wird oben gefangen -> DLQ
        }
    }
}



