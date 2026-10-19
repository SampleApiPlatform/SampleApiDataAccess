using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuGet.SampleSharedModels.DTO;
using SampleApi.Services.MovieServices;
using SampleDataAccessApi.Interfaces.MovieInterfaces;
using System.Threading;
using System.Threading.Tasks;

public class MessageProcessorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageProcessorBackgroundService> _logger;

    public MessageProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MessageProcessorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageProcessor gestartet.");

        // this while is running constantly
        // an action will be executed (await receiver.ReceiveAsync) only every 30 seconds or when a new record has arrived in the queue
        // otherwise the calls are blocked 
        // if the parameter is not provided this action will be called constanty making the server to work too hard. 
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // this hosted service runs as a singleton (lives eternally)
                //
                // the services that need to be excuted to process the records, BusReceiver and the MoviesBusService, are scoped (they live per work request)
                // a singleton cannot inject scoped services in the constructor (error: captive dependency)
                // for this reason we need to generate an scope for each Message
                using var scope = _scopeFactory.CreateScope();
                
                // DI-Objekte aus dem Scope holen
                var receiver = scope.ServiceProvider.GetRequiredService<IBusReceiver>();
                var service = scope.ServiceProvider.GetRequiredService<IMovieBusService>();

                // Aufruf von ReceiveAsync
                await receiver.ReceiveAsync<MovieDTORead>(
                    onAdd: data => data != null ? service.Add(data) : Task.CompletedTask,
                    onUpdate: data => data != null ? service.Update(data) : Task.CompletedTask,
                    onDelete: data => data != null ? service.Delete(data) : Task.CompletedTask, 
                    longPollTimeout: TimeSpan.FromSeconds(30),
                    cancellationToken: stoppingToken
                );
            }
            catch (OperationCanceledException)
            {
                // Normaler Shutdown
                _logger.LogInformation("MessageProcessor wird gestoppt.");
                break;
            }
            catch (Exception ex)
            {
                // Fehler beim Empfangen oder Verarbeiten
                _logger.LogError(ex, "Fehler im MessageProcessor. Warte 5 Sekunden.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("MessageProcessor gestoppt.");
    }


}