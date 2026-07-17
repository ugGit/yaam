using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.UseCases.Profile.Commands;
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
}

public record UpdateProfileInfoInputModel(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Location,
    string? Summary);

public record UpdateProfileSkillsInputModel(List<string> Skills);
