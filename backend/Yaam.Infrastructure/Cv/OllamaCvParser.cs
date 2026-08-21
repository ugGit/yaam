using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yaam.UseCases.Common.Cv;

namespace Yaam.Infrastructure.Cv;

public class OllamaCvParser(
    HttpClient httpClient,
    IOptions<CvSettings> cvOptions,
    ILogger<OllamaCvParser> logger) : ICvParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private record OllamaChatResponse(OllamaChatMessage Message);
    private record OllamaChatMessage(string Role, string Content);

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

        try
        {
            var response = await httpClient.PostAsync(
                new Uri(new Uri(settings.OllamaBaseUrl), "/api/chat"),
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var ollamaResponse = await JsonSerializer.DeserializeAsync<OllamaChatResponse>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                JsonOptions,
                cancellationToken);
            var content = ollamaResponse?.Message?.Content ?? "{}";

            return JsonSerializer.Deserialize<ParsedCvDto>(content, JsonOptions)
                   ?? new ParsedCvDto(null, [], [], [], [], [], []);
        }
        catch (HttpRequestException exception)
        {
            var message = exception.Message;
            logger.LogWarning("CV parsing HTTP error: {Message}", message.Length > 500 ? message[..500] : message);
            throw new Exception($"CV parsing failed: {exception.Message}", exception);
        }
        catch (JsonException exception)
        {
            var message = exception.Message;
            logger.LogWarning("CV parsing JSON error: {Message}", message.Length > 500 ? message[..500] : message);
            throw new Exception($"CV parsing failed: {exception.Message}", exception);
        }
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
          "Certifications": [{ "Name": string, "Issuer": string|null, "Date": "YYYY-MM"|null }],
          "CustomFields": [{ "Label": string, "Value": string }]
        }

        Use null for missing fields. For Proficiency, pick the closest match:
        Basic=elementary, BusinessProficiency=working/professional, Fluent=full professional, Native=mother tongue.

        CustomFields should capture data that does not fit any of the fields above — for example:
        portfolio URL, GitHub profile, LinkedIn URL, visa status, driving licence, publications, awards,
        volunteer work, hobbies, or any other notable information present in the CV.
        Only add a custom field when you are confident the data belongs there and cannot be placed elsewhere.
        Leave CustomFields empty when all CV data is covered by the structured fields above.

        CV text:
        {{text}}
        """;
}
