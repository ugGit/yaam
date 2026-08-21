namespace Yaam.UseCases.Common.Cv;

public interface ICvParser
{
    Task<ParsedCvDto> ParseAsync(string text, CancellationToken cancellationToken);
}
