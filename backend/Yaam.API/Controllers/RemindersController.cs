using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.API.Reminders;
using Yaam.UseCases.Reminders.Queries;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/reminders")]
public class RemindersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListReminders")]
    public async Task<ActionResult<List<ReminderViewModel>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetActiveRemindersQuery(),
            cancellationToken);
        return Ok(result.Select(ReminderViewModelMapper.ToViewModel).ToList());
    }
}
