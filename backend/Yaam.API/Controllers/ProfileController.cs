using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.Domain.Enums;
using Yaam.UseCases.Profile.Commands;
using Yaam.UseCases.Profile.Commands.Education;
using Yaam.UseCases.Profile.Commands.Certifications;
using Yaam.UseCases.Profile.Commands.Languages;
using Yaam.UseCases.Profile.Commands.Links;
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

    [HttpPost("education")]
    [EndpointName("AddEducation")]
    public async Task<IActionResult> AddEducation(
        [FromBody] EducationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddEducationCommand(
                input.Institution, input.Degree, input.FieldOfStudy, input.StartDate, input.EndDate),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("education/{id:guid}")]
    [EndpointName("UpdateEducation")]
    public async Task<IActionResult> UpdateEducation(
        Guid id, [FromBody] EducationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateEducationCommand(
                id, input.Institution, input.Degree, input.FieldOfStudy, input.StartDate, input.EndDate),
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("education/{id:guid}")]
    [EndpointName("DeleteEducation")]
    public async Task<IActionResult> DeleteEducation(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteEducationCommand(id),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("languages")]
    [EndpointName("AddLanguage")]
    public async Task<IActionResult> AddLanguage(
        [FromBody] LanguageInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddLanguageCommand(input.Name, input.Proficiency),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("languages/{id:guid}")]
    [EndpointName("UpdateLanguage")]
    public async Task<IActionResult> UpdateLanguage(
        Guid id, [FromBody] LanguageInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateLanguageCommand(id, input.Name, input.Proficiency),
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("languages/{id:guid}")]
    [EndpointName("DeleteLanguage")]
    public async Task<IActionResult> DeleteLanguage(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteLanguageCommand(id),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("certifications")]
    [EndpointName("AddCertification")]
    public async Task<IActionResult> AddCertification(
        [FromBody] CertificationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddCertificationCommand(input.Name, input.Issuer, input.Date),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("certifications/{id:guid}")]
    [EndpointName("UpdateCertification")]
    public async Task<IActionResult> UpdateCertification(
        Guid id, [FromBody] CertificationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateCertificationCommand(id, input.Name, input.Issuer, input.Date),
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("certifications/{id:guid}")]
    [EndpointName("DeleteCertification")]
    public async Task<IActionResult> DeleteCertification(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteCertificationCommand(id),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("links")]
    [EndpointName("AddProfileLink")]
    public async Task<IActionResult> AddLink(
        [FromBody] ProfileLinkInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddProfileLinkCommand(input.Label, input.Url),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("links/{id:guid}")]
    [EndpointName("UpdateProfileLink")]
    public async Task<IActionResult> UpdateLink(
        Guid id, [FromBody] ProfileLinkInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateProfileLinkCommand(id, input.Label, input.Url),
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("links/{id:guid}")]
    [EndpointName("DeleteProfileLink")]
    public async Task<IActionResult> DeleteLink(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteProfileLinkCommand(id),
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

public record EducationInputModel(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate);

public record LanguageInputModel(string Name, LanguageProficiency Proficiency);

public record CertificationInputModel(string Name, string? Issuer, DateOnly Date);

public record ProfileLinkInputModel(string Label, string Url);
