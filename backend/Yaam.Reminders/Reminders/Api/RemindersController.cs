using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yaam.Reminders.Reminders.Commands;
using Yaam.Reminders.Reminders.Queries;

namespace Yaam.Reminders.Reminders.Api;

[Authorize]
[ApiController]
[Route("api/reminders")]
public class RemindersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListReminders")]
    public async Task<ActionResult<List<ReminderViewModel>>> List(
        [FromQuery] Guid? applicationId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetRemindersQuery(applicationId),
            cancellationToken);
        return Ok(result.Select(ReminderViewModelMapper.ToViewModel).ToList());
    }

    [HttpPost]
    [EndpointName("CreateReminder")]
    public async Task<ActionResult<ReminderViewModel>> Create(
        [FromBody] CreateReminderInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateReminderCommand(input.ApplicationId, input.DelayDays, input.Note),
            cancellationToken);
        return CreatedAtAction(
            nameof(List), new { applicationId = result.ApplicationId }, ReminderViewModelMapper.ToViewModel(result));
    }

    [HttpPatch("{reminderId:guid}/complete")]
    [EndpointName("CompleteReminder")]
    public async Task<IActionResult> Complete(Guid reminderId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new CompleteReminderCommand(reminderId),
            cancellationToken);
        return NoContent();
    }

    [HttpPatch("{reminderId:guid}/reschedule")]
    [EndpointName("RescheduleReminder")]
    public async Task<ActionResult<ReminderViewModel>> Reschedule(
        Guid reminderId, [FromBody] RescheduleReminderInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RescheduleReminderCommand(reminderId, input.NewDueDate),
            cancellationToken);
        return Ok(ReminderViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("{reminderId:guid}")]
    [EndpointName("DeleteReminder")]
    public async Task<IActionResult> Delete(Guid reminderId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteReminderCommand(reminderId),
            cancellationToken);
        return NoContent();
    }
}

public record CreateReminderInputModel(Guid ApplicationId, int DelayDays, string? Note);
public record RescheduleReminderInputModel(DateOnly NewDueDate);
