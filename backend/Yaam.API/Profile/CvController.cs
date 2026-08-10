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

// ---- Mapper ----

public static class CvDataMapper
{
    public static ParsedCvData ToData(ParsedCvDto dto) => new(
        dto.Info is null ? null : new ParsedInfoData(
            dto.Info.FirstName, dto.Info.LastName, dto.Info.Email,
            dto.Info.Phone, dto.Info.Location, dto.Info.Summary),
        dto.WorkExperiences.Select(w => new ParsedWorkExperienceData(
            w.Company, w.Title, w.StartDate, w.EndDate, w.Description)).ToList(),
        dto.Educations.Select(e => new ParsedEducationData(
            e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate)).ToList(),
        dto.Skills,
        dto.Languages.Select(l => new ParsedLanguageData(l.Name, l.Proficiency)).ToList(),
        dto.Certifications.Select(c => new ParsedCertificationData(c.Name, c.Issuer, c.Date)).ToList());

    public static ParsedCvDto ToDto(ParsedCvData data) => new(
        data.Info is null ? null : new ParsedInfoDto(
            data.Info.FirstName, data.Info.LastName, data.Info.Email,
            data.Info.Phone, data.Info.Location, data.Info.Summary),
        data.WorkExperiences.Select(w => new ParsedWorkExperienceDto(
            w.Company, w.Title, w.StartDate, w.EndDate, w.Description)).ToList(),
        data.Educations.Select(e => new ParsedEducationDto(
            e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate)).ToList(),
        data.Skills,
        data.Languages.Select(l => new ParsedLanguageDto(l.Name, l.Proficiency)).ToList(),
        data.Certifications.Select(c => new ParsedCertificationDto(c.Name, c.Issuer, c.Date)).ToList());
}
