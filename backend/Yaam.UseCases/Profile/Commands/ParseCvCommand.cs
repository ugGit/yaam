using MediatR;
using UglyToad.PdfPig;
using Yaam.Domain.Errors;
using Yaam.UseCases.Common.Cv;

namespace Yaam.UseCases.Profile.Commands;

public record ParseCvCommand(byte[] PdfBytes) : IRequest<ParsedCvDto>;

public class ParseCvCommandHandler(ICvParser cvParser)
    : IRequestHandler<ParseCvCommand, ParsedCvDto>
{
    private const int MinimumTextLength = 50;

    public async Task<ParsedCvDto> Handle(ParseCvCommand command, CancellationToken cancellationToken)
    {
        string text;
        using (var document = PdfDocument.Open(command.PdfBytes))
        {
            text = string.Join("\n", document.GetPages().Select(page => page.Text));
        }

        if (text.Trim().Length < MinimumTextLength)
            throw new ConflictException(
                "The uploaded PDF appears to be scanned or contains no text. Please upload a text-based PDF.");

        return await cvParser.ParseAsync(text, cancellationToken);
    }
}
