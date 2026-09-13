using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yaam.Domain.Common;
using Yaam.Domain.Enums;
using Yaam.UseCases.Applications.Commands;
using Yaam.UseCases.Applications.Queries;

namespace Yaam.API.Applications;

[Authorize]
[ApiController]
[Route("api/applications")]
public class ApplicationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListApplications")]
    public async Task<ActionResult<List<ApplicationSummaryViewModel>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] ApplicationStatus? status,
        [FromQuery] string sort = ApplicationSortFields.DateApplied,
        [FromQuery] string order = "desc")
    {
        var sortField = sort switch
        {
            ApplicationSortFields.CompanyName => ApplicationSortField.CompanyName,
            _ => ApplicationSortField.DateApplied,
        };
        var result = await mediator.Send(new GetApplicationsQuery(status, sortField, order), cancellationToken);
        return Ok(result.Select(ApplicationViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet("{id:guid}")]
    [EndpointName("GetApplication")]
    public async Task<ActionResult<ApplicationViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetApplicationByIdQuery(id),
            cancellationToken);
        return Ok(ApplicationViewModelMapper.ToViewModel(result));
    }

    [HttpPost]
    [EndpointName("CreateApplication")]
    public async Task<ActionResult<ApplicationViewModel>> Create(
        [FromBody] CreateApplicationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateApplicationCommand(
                input.CompanyName,
                input.Role,
                input.DateApplied,
                input.Status,
                input.ContactName,
                input.ContactEmail,
                input.ContactPhone,
                input.JobPosting),
            cancellationToken);
        var viewModel = ApplicationViewModelMapper.ToViewModel(result);
        return CreatedAtAction(nameof(GetById), new { id = viewModel.Id }, viewModel);
    }

    [HttpPut("{id:guid}")]
    [EndpointName("UpdateApplication")]
    public async Task<ActionResult<ApplicationViewModel>> Update(
        Guid id, [FromBody] UpdateApplicationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateApplicationCommand(
                id,
                input.CompanyName,
                input.Role,
                input.DateApplied,
                input.Status,
                input.ContactName,
                input.ContactEmail,
                input.ContactPhone,
                input.JobPosting),
            cancellationToken);
        return Ok(ApplicationViewModelMapper.ToViewModel(result));
    }

    [HttpPatch("{id:guid}/status")]
    [EndpointName("PatchApplicationStatus")]
    public async Task<ActionResult<ApplicationViewModel>> UpdateStatus(
        Guid id, [FromBody] UpdateApplicationStatusInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateApplicationStatusCommand(id, input.Status),
            cancellationToken);
        return Ok(ApplicationViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("{id:guid}")]
    [EndpointName("DeleteApplication")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteApplicationCommand(id),
            cancellationToken);
        return NoContent();
    }
}

public record CreateApplicationInputModel(
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting);

public record UpdateApplicationInputModel(
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting);

public record UpdateApplicationStatusInputModel(ApplicationStatus Status);
