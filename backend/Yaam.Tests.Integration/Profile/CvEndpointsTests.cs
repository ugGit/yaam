using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.API.Profile;

namespace Yaam.Tests.Integration.Profile;

[Collection("Integration")]
public class CvEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_CvParse_ValidPdf_ReturnsParsedData()
    {
        var pdfBytes = CreateMinimalTextPdf();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "cv.pdf");

        var response = await _client.PostAsync("/api/profile/cv/parse", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParsedCvViewModel>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.Info!.FirstName.Should().Be("Ada");
        result.Data.Skills.Should().Contain("C#");
    }

    [Fact]
    public async Task POST_CvParse_EmptyPdf_Returns409()
    {
        var pdfBytes = CreateEmptyPdf();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "cv.pdf");

        var response = await _client.PostAsync("/api/profile/cv/parse", content);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_CvApply_AddMode_AddsItemsToProfile()
    {
        var payload = new
        {
            selectedItems = new
            {
                info = new { firstName = "Ada", lastName = "Lovelace", email = (string?)null, phone = (string?)null, location = (string?)null, summary = (string?)null },
                workExperiences = new[] { new { company = "Acme", title = "Engineer", startDate = "2020-01", endDate = (string?)null, description = (string?)null } },
                educations = Array.Empty<object>(),
                skills = new[] { "C#" },
                languages = Array.Empty<object>(),
                certifications = Array.Empty<object>(),
            },
            mode = "Add",
        };

        var response = await _client.PostAsJsonAsync("/api/profile/cv/apply", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileViewModel>();
        profile!.FirstName.Should().Be("Ada");
        profile.WorkExperiences.Should().ContainSingle(w => w.Company == "Acme");
        profile.Skills.Should().Contain("C#");
    }

    [Fact]
    public async Task POST_CvApply_ReplaceMode_ClearsAndReplacesProfile()
    {
        // Seed existing data
        await _client.PostAsJsonAsync("/api/profile/cv/apply", new
        {
            selectedItems = new
            {
                info = (object?)null,
                workExperiences = new[] { new { company = "OldCo", title = "Dev", startDate = "2018-01", endDate = (string?)null, description = (string?)null } },
                educations = Array.Empty<object>(),
                skills = new[] { "Pascal" },
                languages = Array.Empty<object>(),
                certifications = Array.Empty<object>(),
            },
            mode = "Add",
        });

        // Now replace with new data
        var response = await _client.PostAsJsonAsync("/api/profile/cv/apply", new
        {
            selectedItems = new
            {
                info = (object?)null,
                workExperiences = new[] { new { company = "NewCo", title = "Senior Dev", startDate = "2023-01", endDate = (string?)null, description = (string?)null } },
                educations = Array.Empty<object>(),
                skills = new[] { "C#" },
                languages = Array.Empty<object>(),
                certifications = Array.Empty<object>(),
            },
            mode = "Replace",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileViewModel>();
        profile!.WorkExperiences.Should().ContainSingle(w => w.Company == "NewCo");
        profile.WorkExperiences.Should().NotContain(w => w.Company == "OldCo");
        profile.Skills.Should().Contain("C#");
        profile.Skills.Should().NotContain("Pascal");
    }

    private static byte[] CreateMinimalTextPdf()
    {
        // Manually crafted PDF with correct cross-reference offsets.
        // Text content is 58 characters — above the 50-character minimum.
        var pdfText = "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj\n4 0 obj<</Length 90>>\nstream\nBT /F1 12 Tf 72 720 Td (John Doe Senior Software Engineer Resume Skills Experience) Tj ET\nendstream\nendobj\n5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\nxref\n0 6\n0000000000 65535 f\r\n0000000009 00000 n\r\n0000000052 00000 n\r\n0000000101 00000 n\r\n0000000211 00000 n\r\n0000000347 00000 n\r\ntrailer<</Size 6/Root 1 0 R>>\nstartxref\n408\n%%EOF";
        return System.Text.Encoding.ASCII.GetBytes(pdfText);
    }

    private static byte[] CreateEmptyPdf()
    {
        var pdfText = "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]>>endobj\nxref\n0 4\n0000000000 65535 f\r\n0000000009 00000 n\r\n0000000062 00000 n\r\n0000000119 00000 n\r\ntrailer<</Size 4/Root 1 0 R>>\nstartxref\n183\n%%EOF";
        return System.Text.Encoding.ASCII.GetBytes(pdfText);
    }
}
