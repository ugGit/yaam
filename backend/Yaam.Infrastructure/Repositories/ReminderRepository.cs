using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;

namespace Yaam.Infrastructure.Repositories;

public class ReminderRepository(AppDbContext db) : IReminderRepository
{
    public async Task<Reminder?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken)
        => await db.Reminders
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId && r.CompletedAt == null, cancellationToken);

    public async Task<List<Reminder>> GetAllActiveAsync(CancellationToken cancellationToken)
        => await db.Reminders
            .Include(r => r.Application)
            .Where(r => r.CompletedAt == null)
            .OrderBy(r => r.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<List<Reminder>> GetDueAsync(DateOnly today, CancellationToken cancellationToken)
        => await db.Reminders
            .Include(r => r.Application)
            .Where(r => r.CompletedAt == null && r.NotifiedAt == null && r.DueDate <= today)
            .ToListAsync(cancellationToken);

    public async Task<Reminder> AddAsync(Reminder reminder, CancellationToken cancellationToken)
    {
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(cancellationToken);
        return reminder;
    }

    public async Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken)
        => await db.SaveChangesAsync(cancellationToken);

    public async Task DeleteByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var reminder = await db.Reminders
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId, cancellationToken);
        if (reminder is not null)
        {
            db.Reminders.Remove(reminder);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
