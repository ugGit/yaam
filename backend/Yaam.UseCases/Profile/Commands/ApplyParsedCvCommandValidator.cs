using FluentValidation;
using Yaam.UseCases.Common.Cv;

namespace Yaam.UseCases.Profile.Commands;

public class ApplyParsedCvCommandValidator : AbstractValidator<ApplyParsedCvCommand>
{
    public ApplyParsedCvCommandValidator()
    {
        RuleForEach(x => x.SelectedItems.WorkExperiences)
            .ChildRules(we =>
            {
                we.RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
                we.RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
                we.RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            });

        RuleForEach(x => x.SelectedItems.Educations)
            .ChildRules(edu =>
            {
                edu.RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
                edu.RuleFor(x => x.Degree).MaximumLength(200).When(x => x.Degree is not null);
                edu.RuleFor(x => x.FieldOfStudy).MaximumLength(200).When(x => x.FieldOfStudy is not null);
            });

        RuleForEach(x => x.SelectedItems.Certifications)
            .ChildRules(cert =>
            {
                cert.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
                cert.RuleFor(x => x.Issuer).MaximumLength(200).When(x => x.Issuer is not null);
            });

        RuleForEach(x => x.SelectedItems.Skills).NotEmpty().MaximumLength(100);
    }
}
