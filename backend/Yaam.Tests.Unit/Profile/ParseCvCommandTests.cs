using FluentAssertions;
using NSubstitute;
using Yaam.Domain.Errors;
using Yaam.UseCases.Common.Cv;
using Yaam.UseCases.Profile.Commands;

namespace Yaam.Tests.Unit.Profile;

public class ParseCvCommandTests
{
    private readonly ICvParser _parser = Substitute.For<ICvParser>();

    [Fact]
    public async Task Handle_EmptyPdf_ThrowsConflictException()
    {
        var emptyPdf = CreateEmptyPdf();
        var handler = new ParseCvCommandHandler(_parser);

        var act = () => handler.Handle(new ParseCvCommand(emptyPdf), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*scanned*");
    }

    [Fact]
    public async Task Handle_ValidPdf_ReturnsParsedResult()
    {
        var samplePdf = CreateMinimalTextPdf();
        var expected = new ParsedCvDto(
            new ParsedInfoDto("Ada", "Lovelace", null, null, null, null),
            [], [], ["C#"], [], []);
        _parser.ParseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var handler = new ParseCvCommandHandler(_parser);
        var result = await handler.Handle(new ParseCvCommand(samplePdf), CancellationToken.None);

        result.Should().Be(expected);
        await _parser.Received(1).ParseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static byte[] CreateMinimalTextPdf()
    {
        // Manually crafted PDF with correct cross-reference offsets.
        // Text content is 58 characters — above the 50-character minimum.
        var text = "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj\n4 0 obj<</Length 90>>\nstream\nBT /F1 12 Tf 72 720 Td (John Doe Senior Software Engineer Resume Skills Experience) Tj ET\nendstream\nendobj\n5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\nxref\n0 6\n0000000000 65535 f\r\n0000000009 00000 n\r\n0000000052 00000 n\r\n0000000101 00000 n\r\n0000000211 00000 n\r\n0000000347 00000 n\r\ntrailer<</Size 6/Root 1 0 R>>\nstartxref\n408\n%%EOF";
        return System.Text.Encoding.ASCII.GetBytes(text);
    }

    private static byte[] CreateEmptyPdf()
    {
        var text = "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]>>endobj\nxref\n0 4\n0000000000 65535 f\r\n0000000009 00000 n\r\n0000000062 00000 n\r\n0000000119 00000 n\r\ntrailer<</Size 4/Root 1 0 R>>\nstartxref\n183\n%%EOF";
        return System.Text.Encoding.ASCII.GetBytes(text);
    }
}
