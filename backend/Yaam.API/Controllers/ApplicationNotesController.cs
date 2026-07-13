using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.Application.Applications.Commands.Notes;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/applications/{applicationId:guid}/notes")]
public class ApplicationNotesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [EndpointName("AddApplicationNote")]
    public async Task<IActionResult> Add(
        Guid applicationId, [FromBody] NoteBodyRequest request, CancellationToken ct = default)
    {
        var result = await mediator.Send(new AddApplicationNoteCommand(applicationId, request.Body), ct);
        return result is null ? NotFound() : Created(string.Empty, result);
    }

    [HttpPut("{noteId:guid}")]
    [EndpointName("UpdateApplicationNote")]
    public async Task<IActionResult> Update(
        Guid applicationId, Guid noteId, [FromBody] NoteBodyRequest request, CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateApplicationNoteCommand(applicationId, noteId, request.Body), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{noteId:guid}")]
    [EndpointName("DeleteApplicationNote")]
    public async Task<IActionResult> Delete(
        Guid applicationId, Guid noteId, CancellationToken ct = default)
    {
        await mediator.Send(new DeleteApplicationNoteCommand(applicationId, noteId), ct);
        return NoContent();
    }
}

public record NoteBodyRequest(string Body);
