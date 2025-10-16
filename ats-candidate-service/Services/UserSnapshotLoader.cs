using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CandidateService.Services;

public class UserSnapshotLoader : BackgroundService
{
    private readonly UserCache _cache;
    private readonly ILogger<UserSnapshotLoader> _logger;

    public UserSnapshotLoader(UserCache cache, ILogger<UserSnapshotLoader> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       
        await Task.Delay(3000, stoppingToken);

        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("🚀 Starting async user snapshot load...");
                await _cache.LoadSnapshotAsync(stoppingToken);
                _logger.LogInformation("✅ User snapshot loaded successfully ({Count} users).", _cache.Users.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load user snapshot on startup");
            }
        }, stoppingToken);
    }
}