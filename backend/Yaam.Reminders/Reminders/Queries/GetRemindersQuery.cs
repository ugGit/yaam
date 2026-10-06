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
        var active = db.Reminders
            .Include(r => r.Application)
            .Where(r => r.CompletedAt == null && !r.Application.IsDeleted);

        if (query.ApplicationId is { } applicationId)
            active = active.Where(r => r.ApplicationId == applicationId);

        var reminders = await active
            .OrderBy(r => r.DueDate)
            .ToListAsync(cancellationToken);
        return reminders.Select(ReminderMapper.ToDto).ToList();
    }
}
