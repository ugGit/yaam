using Yaam.UseCases.Common.Cv;

namespace Yaam.API.Profile;

internal static class CvDataMapper
{
    internal static ParsedCvData ToData(ParsedCvDto dto) => new(
        dto.Info is null ? null : new ParsedInfoData(
            dto.Info.FirstName, dto.Info.LastName, dto.Info.Email,
            dto.Info.Phone, dto.Info.Location, dto.Info.Summary),
        dto.WorkExperiences.Select(w => new ParsedWorkExperienceData(
            w.Company, w.Title, w.StartDate, w.EndDate, w.Description)).ToList(),
        dto.Educations.Select(e => new ParsedEducationData(
            e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate)).ToList(),
        dto.Skills,
        dto.Languages.Select(l => new ParsedLanguageData(l.Name, l.Proficiency)).ToList(),
        dto.Certifications.Select(c => new ParsedCertificationData(c.Name, c.Issuer, c.Date)).ToList(),
        dto.CustomFields.Select(f => new ParsedCustomFieldData(f.Label, f.Value)).ToList());

    internal static ParsedCvDto ToDto(ParsedCvData data) => new(
        data.Info is null ? null : new ParsedInfoDto(
            data.Info.FirstName, data.Info.LastName, data.Info.Email,
            data.Info.Phone, data.Info.Location, data.Info.Summary),
        data.WorkExperiences.Select(w => new ParsedWorkExperienceDto(
            w.Company, w.Title, w.StartDate, w.EndDate, w.Description)).ToList(),
        data.Educations.Select(e => new ParsedEducationDto(
            e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate)).ToList(),
        data.Skills,
        data.Languages.Select(l => new ParsedLanguageDto(l.Name, l.Proficiency)).ToList(),
        data.Certifications.Select(c => new ParsedCertificationDto(c.Name, c.Issuer, c.Date)).ToList(),
        data.CustomFields.Select(f => new ParsedCustomFieldDto(f.Label, f.Value)).ToList());
}
