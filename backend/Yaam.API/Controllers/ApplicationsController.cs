using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.Application.Applications.Commands;
using Yaam.Application.Applications.Queries;
using Yaam.Domain.Enums;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListApplications")]
    public async Task<IActionResult> GetAll(
        [FromQuery] ApplicationStatus? status,
        [FromQuery] string sort = "dateApplied",
        [FromQuery] string order = "desc",
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetApplicationsQuery(status, sort, order), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [EndpointName("GetApplication")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetApplicationByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [EndpointName("CreateApplication")]
    public async Task<IActionResult> Create(
        [FromBody] CreateApplicationCommand command, CancellationToken ct = default)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [EndpointName("UpdateApplication")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateApplicationRequest request, CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateApplicationCommand(id, request.CompanyName, request.Role,
                request.DateApplied, request.Status, request.ContactName,
                request.ContactEmail, request.ContactPhone, request.JobPosting), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [EndpointName("PatchApplicationStatus")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateApplicationStatusRequest request, CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateApplicationStatusCommand(id, request.Status), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [EndpointName("DeleteApplication")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new DeleteApplicationCommand(id), ct);
        return NoContent();
    }
}

public record UpdateApplicationRequest(
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting);

public record UpdateApplicationStatusRequest(ApplicationStatus Status);
