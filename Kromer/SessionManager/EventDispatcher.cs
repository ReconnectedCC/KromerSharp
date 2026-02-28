using System.Threading.Channels;

namespace Kromer.SessionManager;

public class EventDispatcher(SessionManager sessionManager, Channel<KristEvent> reader) : BackgroundService
{
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var kristEvent in reader.Reader.ReadAllAsync(stoppingToken))
        {
            
        }
    }
}