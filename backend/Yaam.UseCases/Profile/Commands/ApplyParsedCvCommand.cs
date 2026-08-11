using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Common.Cv;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands;

public enum CvApplyMode { Add, Replace }

public record ApplyParsedCvCommand(
    ParsedCvDto SelectedItems,
    CvApplyMode Mode) : IRequest<ProfileDto>;

public class ApplyParsedCvCommandHandler(IProfileRepository repository)
    : IRequestHandler<ApplyParsedCvCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(ApplyParsedCvCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var items = command.SelectedItems;

        if (command.Mode == CvApplyMode.Replace)
        {
            profile.WorkExperiences.Clear();
            profile.Educations.Clear();
            profile.Languages.Clear();
            profile.Certifications.Clear();
            profile.Skills.Clear();

            if (items.Info is not null)
                ApplyInfo(profile, items.Info);
        }
        else if (items.Info is not null)
        {
            ApplyInfo(profile, items.Info);
        }

        foreach (var we in items.WorkExperiences)
        {
            var startDate = ParseDate(we.StartDate);
            if (startDate is null) continue;
            profile.WorkExperiences.Add(new Yaam.Domain.Entities.WorkExperience
            {
                ProfileId = profile.Id,
                Company = we.Company,
                Title = we.Title,
                StartDate = startDate.Value,
                EndDate = ParseDate(we.EndDate),
                Description = we.Description,
            });
        }

        foreach (var edu in items.Educations)
            profile.Educations.Add(new Yaam.Domain.Entities.Education
            {
                ProfileId = profile.Id,
                Institution = edu.Institution,
                Degree = edu.Degree,
                FieldOfStudy = edu.FieldOfStudy,
                StartDate = ParseDate(edu.StartDate),
                EndDate = ParseDate(edu.EndDate),
            });

        foreach (var skill in items.Skills)
            profile.Skills.Add(skill);

        foreach (var lang in items.Languages)
            profile.Languages.Add(new Yaam.Domain.Entities.Language
            {
                ProfileId = profile.Id,
                Name = lang.Name,
                Proficiency = Enum.TryParse<Yaam.Domain.Enums.LanguageProficiency>(
                    lang.Proficiency, out var proficiency)
                    ? proficiency
                    : Yaam.Domain.Enums.LanguageProficiency.Basic,
            });

        foreach (var cert in items.Certifications)
        {
            var certDate = ParseDate(cert.Date);
            if (certDate is null) continue;
            profile.Certifications.Add(new Yaam.Domain.Entities.Certification
            {
                ProfileId = profile.Id,
                Name = cert.Name,
                Issuer = cert.Issuer,
                Date = certDate.Value,
            });
        }

        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(profile);
    }

    private static void ApplyInfo(Domain.Entities.Profile profile, ParsedInfoDto info)
    {
        if (info.FirstName is not null) profile.FirstName = info.FirstName;
        if (info.LastName is not null) profile.LastName = info.LastName;
        if (info.Email is not null) profile.Email = info.Email;
        if (info.Phone is not null) profile.Phone = info.Phone;
        if (info.Location is not null) profile.Location = info.Location;
        if (info.Summary is not null) profile.Summary = info.Summary;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (value is null) return null;
        // Accept "YYYY-MM-DD" or "YYYY-MM"
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", out var full)) return full;
        if (DateOnly.TryParseExact(value, "yyyy-MM", out var yearMonth)) return yearMonth;
        return null;
    }
}
