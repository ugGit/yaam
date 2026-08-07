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

    [HttpPatch("complete")]
    [EndpointName("CompleteReminder")]
    public async Task<IActionResult> Complete(Guid applicationId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new CompleteReminderCommand(applicationId),
            cancellationToken);
        return NoContent();
    }

    [HttpPatch("reschedule")]
    [EndpointName("RescheduleReminder")]
    public async Task<ActionResult<ApplicationReminderViewModel>> Reschedule(
        Guid applicationId, [FromBody] RescheduleReminderInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RescheduleReminderCommand(applicationId, input.NewDueDate),
            cancellationToken);
        return Ok(ReminderViewModelMapper.ToViewModel(result));
    }

    [HttpDelete]
    [EndpointName("DeleteReminder")]
    public async Task<IActionResult> Delete(Guid applicationId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteReminderCommand(applicationId),
            cancellationToken);
        return NoContent();
    }
}

public record SetReminderInputModel(int DelayDays, string? Note);
public record RescheduleReminderInputModel(DateOnly NewDueDate);
