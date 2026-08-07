using MediatR;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.API.Reminders;

public class ReminderNotificationService(IServiceScopeFactory scopeFactory, ILogger<ReminderNotificationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun();
            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            logger.LogInformation("Running reminder notification job at {Time}", DateTime.UtcNow);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(
                    new SendDueRemindersCommand(),
                    stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Reminder notification job failed");
            }
        }
    }

    internal static TimeSpan TimeUntilNextRun(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var nextRun = now.Date.AddHours(8);
        if (now >= nextRun) nextRun = nextRun.AddDays(1);
        return nextRun - now;
    }
}
