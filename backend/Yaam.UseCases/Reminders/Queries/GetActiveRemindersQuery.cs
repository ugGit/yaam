using MediatR;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Reminders.Queries;

public record GetActiveRemindersQuery : IRequest<List<ReminderSummaryDto>>;

public class GetActiveRemindersQueryHandler(IReminderRepository repository)
    : IRequestHandler<GetActiveRemindersQuery, List<ReminderSummaryDto>>
{
    public async Task<List<ReminderSummaryDto>> Handle(
        GetActiveRemindersQuery query, CancellationToken cancellationToken)
    {
        var reminders = await repository.GetAllActiveAsync(cancellationToken);
        return reminders.Select(ReminderMapper.ToSummaryDto).ToList();
    }
}
