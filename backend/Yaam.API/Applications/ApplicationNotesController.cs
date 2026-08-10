using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.UseCases.Applications.Commands.Notes;

namespace Yaam.API.Applications;

[ApiController]
[Route("api/applications/{applicationId:guid}/notes")]
public class ApplicationNotesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [EndpointName("AddApplicationNote")]
    public async Task<ActionResult<ApplicationNoteViewModel>> Add(
        Guid applicationId, [FromBody] CreateApplicationNoteInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddApplicationNoteCommand(applicationId, input.Body),
            cancellationToken);
        var viewModel = ApplicationViewModelMapper.ToViewModel(result);
        return Created($"api/applications/{applicationId}/notes/{viewModel.Id}", viewModel);
    }

    [HttpPut("{noteId:guid}")]
    [EndpointName("UpdateApplicationNote")]
    public async Task<ActionResult<ApplicationNoteViewModel>> Update(
        Guid applicationId, Guid noteId,
        [FromBody] UpdateApplicationNoteInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateApplicationNoteCommand(applicationId, noteId, input.Body),
            cancellationToken);
        return Ok(ApplicationViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("{noteId:guid}")]
    [EndpointName("DeleteApplicationNote")]
    public async Task<IActionResult> Delete(
        Guid applicationId, Guid noteId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteApplicationNoteCommand(applicationId, noteId),
            cancellationToken);
        return NoContent();
    }
}

public record CreateApplicationNoteInputModel(string Body);
public record UpdateApplicationNoteInputModel(string Body);
