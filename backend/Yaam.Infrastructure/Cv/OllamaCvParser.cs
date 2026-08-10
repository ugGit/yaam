using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Yaam.UseCases.Common.Cv;

namespace Yaam.Infrastructure.Cv;

public class OllamaCvParser(
    HttpClient httpClient,
    IOptions<CvSettings> cvOptions) : ICvParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<ParsedCvDto> ParseAsync(string text, CancellationToken cancellationToken)
    {
        var settings = cvOptions.Value;
        var prompt = BuildPrompt(text);

        var requestBody = new
        {
            model = settings.OllamaModel,
            messages = new[] { new { role = "user", content = prompt } },
            format = "json",
            stream = false,
        };

        var response = await httpClient.PostAsync(
            new Uri(new Uri(settings.OllamaBaseUrl), "/api/chat"),
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(responseJson);
        var content = document.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        return JsonSerializer.Deserialize<ParsedCvDto>(content, JsonOptions)
               ?? new ParsedCvDto(null, [], [], [], [], []);
    }

    private static string BuildPrompt(string text) => $$"""
        Extract structured data from the following CV text and return it as JSON matching this exact schema.
        Return ONLY the JSON object, no other text or markdown.

        Schema:
        {
          "Info": { "FirstName": string|null, "LastName": string|null, "Email": string|null, "Phone": string|null, "Location": string|null, "Summary": string|null },
          "WorkExperiences": [{ "Company": string, "Title": string, "StartDate": "YYYY-MM"|null, "EndDate": "YYYY-MM"|null, "Description": string|null }],
          "Educations": [{ "Institution": string, "Degree": string|null, "FieldOfStudy": string|null, "StartDate": "YYYY-MM"|null, "EndDate": "YYYY-MM"|null }],
          "Skills": [string],
          "Languages": [{ "Name": string, "Proficiency": "Basic"|"BusinessProficiency"|"Fluent"|"Native" }],
          "Certifications": [{ "Name": string, "Issuer": string|null, "Date": "YYYY-MM"|null }]
        }

        Use null for missing fields. For Proficiency, pick the closest match:
        Basic=elementary, BusinessProficiency=working/professional, Fluent=full professional, Native=mother tongue.

        CV text:
        {{text}}
        """;
}
