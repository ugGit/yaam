using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.API.Reminders;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.API.Applications;

[ApiController]
[Route("api/applications/{applicationId:guid}/reminder")]
public class ApplicationReminderController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [EndpointName("SetReminder")]
    public async Task<ActionResult<ApplicationReminderViewModel>> Set(
        Guid applicationId, [FromBody] SetReminderInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SetReminderCommand(applicationId, input.DelayDays, input.Note),
            cancellationToken);
        return CreatedAtAction(nameof(Set), new { applicationId }, ReminderViewModelMapper.ToViewModel(result));
    }

    [HttpPatch("{reminderId:guid}/complete")]
    [EndpointName("CompleteReminder")]
    public async Task<IActionResult> Complete(
        Guid applicationId, Guid reminderId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new CompleteReminderCommand(reminderId),
            cancellationToken);
        return NoContent();
    }

    [HttpPatch("{reminderId:guid}/reschedule")]
    [EndpointName("RescheduleReminder")]
    public async Task<ActionResult<ApplicationReminderViewModel>> Reschedule(
        Guid applicationId, Guid reminderId, [FromBody] RescheduleReminderInputModel input,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RescheduleReminderCommand(reminderId, input.NewDueDate),
            cancellationToken);
        return Ok(ReminderViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("{reminderId:guid}")]
    [EndpointName("DeleteReminder")]
    public async Task<IActionResult> Delete(
        Guid applicationId, Guid reminderId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteReminderCommand(reminderId),
            cancellationToken);
        return NoContent();
    }
}

public record SetReminderInputModel(int DelayDays, string? Note);
public record RescheduleReminderInputModel(DateOnly NewDueDate);
