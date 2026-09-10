using Couppa.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Services;

/// <summary>
/// Removes only anonymous carts which have not been touched for the configured retention period.
/// User carts are never handled by this job.
/// </summary>
public sealed class GuestCartCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GuestCartCleanupService> _logger;

    public GuestCartCleanupService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<GuestCartCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The first run is delayed so application startup is not held up by maintenance work.
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Guest cart cleanup failed.");
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var expiryDays = Math.Max(1, _configuration.GetValue<int?>("Cart:GuestCartExpiryDays") ?? 7);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-expiryDays);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var carts = await db.Carts
            .Where(cart => cart.SessionId != null && cart.UpdatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (carts.Count == 0)
        {
            return;
        }

        db.Carts.RemoveRange(carts);
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Removed {Count} expired guest carts.", carts.Count);
    }
}
