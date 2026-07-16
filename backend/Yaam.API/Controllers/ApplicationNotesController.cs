using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.UseCases.Applications.Commands.Notes;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/applications/{applicationId:guid}/notes")]
public class ApplicationNotesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [EndpointName("AddApplicationNote")]
    public async Task<IActionResult> Add(
        Guid applicationId, [FromBody] CreateApplicationNoteInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddApplicationNoteCommand(applicationId, input.Body), cancellationToken);
        return Created(string.Empty, result);
    }

    [HttpPut("{noteId:guid}")]
    [EndpointName("UpdateApplicationNote")]
    public async Task<IActionResult> Update(
        Guid applicationId, Guid noteId,
        [FromBody] UpdateApplicationNoteInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateApplicationNoteCommand(applicationId, noteId, input.Body), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{noteId:guid}")]
    [EndpointName("DeleteApplicationNote")]
    public async Task<IActionResult> Delete(
        Guid applicationId, Guid noteId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteApplicationNoteCommand(applicationId, noteId), cancellationToken);
        return NoContent();
    }
}

public record CreateApplicationNoteInputModel(string Body);
public record UpdateApplicationNoteInputModel(string Body);
