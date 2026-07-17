using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.UseCases.Profile.Commands;
using Yaam.UseCases.Profile.Commands.WorkExperiences;
using Yaam.UseCases.Profile.Dtos;
using Yaam.UseCases.Profile.Queries;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("GetProfile")]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetProfileQuery(),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("info")]
    [EndpointName("UpdateProfileInfo")]
    public async Task<IActionResult> UpdateInfo(
        [FromBody] UpdateProfileInfoInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateProfileInfoCommand(
                input.FirstName,
                input.LastName,
                input.Email,
                input.Phone,
                input.Location,
                input.Summary),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("skills")]
    [EndpointName("UpdateProfileSkills")]
    public async Task<IActionResult> UpdateSkills(
        [FromBody] UpdateProfileSkillsInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateProfileSkillsCommand(input.Skills),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("work-experiences")]
    [EndpointName("AddWorkExperience")]
    public async Task<IActionResult> AddWorkExperience(
        [FromBody] WorkExperienceInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddWorkExperienceCommand(
                input.Company, input.Title, input.StartDate, input.EndDate, input.Description),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("work-experiences/{id:guid}")]
    [EndpointName("UpdateWorkExperience")]
    public async Task<IActionResult> UpdateWorkExperience(
        Guid id, [FromBody] WorkExperienceInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateWorkExperienceCommand(
                id, input.Company, input.Title, input.StartDate, input.EndDate, input.Description),
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("work-experiences/{id:guid}")]
    [EndpointName("DeleteWorkExperience")]
    public async Task<IActionResult> DeleteWorkExperience(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteWorkExperienceCommand(id),
            cancellationToken);
        return NoContent();
    }
}

public record UpdateProfileInfoInputModel(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Location,
    string? Summary);

public record UpdateProfileSkillsInputModel(List<string> Skills);

public record WorkExperienceInputModel(
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description);
