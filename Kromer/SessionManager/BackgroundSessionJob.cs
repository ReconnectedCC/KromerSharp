namespace Kromer.SessionManager;

public class BackgroundSessionJob(SessionManager sessionManager) : BackgroundService
{
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await sessionManager.PingSessionsAsync();
            sessionManager.CleanupSessions();
            
            await Task.Delay(10_000, stoppingToken);
        }
    }
}