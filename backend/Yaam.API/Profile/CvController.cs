using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.UseCases.Common.Cv;
using Yaam.UseCases.Profile.Commands;

namespace Yaam.API.Profile;

[ApiController]
[Route("api/profile/cv")]
public class CvController(IMediator mediator) : ControllerBase
{
    [HttpPost("parse")]
    [EndpointName("ParseCv")]
    public async Task<ActionResult<ParsedCvViewModel>> Parse(
        IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest("File exceeds the 2 MB limit.");

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PDF files are accepted.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);

        var result = await mediator.Send(
            new ParseCvCommand(ms.ToArray()),
            cancellationToken);

        return Ok(new ParsedCvViewModel(CvDataMapper.ToData(result)));
    }

    [HttpPost("apply")]
    [EndpointName("ApplyCv")]
    public async Task<ActionResult<ProfileViewModel>> Apply(
        [FromBody] ApplyCvInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ApplyParsedCvCommand(CvDataMapper.ToDto(input.SelectedItems), input.Mode),
            cancellationToken);

        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }
}

// ---- Input / View models ----

public record ParsedCvViewModel(ParsedCvData Data);

public record ApplyCvInputModel(ParsedCvData SelectedItems, CvApplyMode Mode);

public record ParsedCvData(
    ParsedInfoData? Info,
    List<ParsedWorkExperienceData> WorkExperiences,
    List<ParsedEducationData> Educations,
    List<string> Skills,
    List<ParsedLanguageData> Languages,
    List<ParsedCertificationData> Certifications);

public record ParsedInfoData(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Location,
    string? Summary);

public record ParsedWorkExperienceData(
    string Company,
    string Title,
    string? StartDate,
    string? EndDate,
    string? Description);

public record ParsedEducationData(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    string? StartDate,
    string? EndDate);

public record ParsedLanguageData(string Name, string Proficiency);

public record ParsedCertificationData(string Name, string? Issuer, string? Date);
