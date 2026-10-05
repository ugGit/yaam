using MediatR;
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Persistence;
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders.Queries;

public record GetRemindersQuery(Guid? ApplicationId) : IRequest<List<ReminderDto>>;

public class GetRemindersQueryHandler(RemindersDbContext db)
    : IRequestHandler<GetRemindersQuery, List<ReminderDto>>
{
    public async Task<List<ReminderDto>> Handle(GetRemindersQuery query, CancellationToken cancellationToken)
    {
        var active =
            from reminder in db.Reminders
            join snapshot in db.ApplicationSnapshots on reminder.ApplicationId equals snapshot.ApplicationId
            where reminder.CompletedAt == null && !snapshot.IsDeleted
            select new { reminder, snapshot };

        if (query.ApplicationId is { } applicationId)
            active = active.Where(row => row.reminder.ApplicationId == applicationId);

        var rows = await active
            .OrderBy(row => row.reminder.DueDate)
            .ToListAsync(cancellationToken);
        return rows.Select(row => ReminderMapper.ToDto(row.reminder, row.snapshot)).ToList();
    }
}
