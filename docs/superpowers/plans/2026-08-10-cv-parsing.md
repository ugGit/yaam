# CV Parsing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Users can upload a PDF CV that is parsed by AI into structured profile data, reviewed with per-item checkboxes and inline editing, and applied to their profile in either merge or replace mode.

**Architecture:** Backend pipeline — PdfPig extracts text from the PDF, `ICvParser` (Ollama locally) returns `ParsedCvDto`, two endpoints handle parse and apply separately. Frontend — `cv-upload` triggers parsing, `cv-review` shows section checklists with inline editing, profile page orchestrates the flow and calls apply.

**Tech Stack:** .NET 10, PdfPig (`UglyToad.PdfPig`), Ollama HTTP API, Angular 19 signals, daisyUI

## Global Constraints

- Follow CLAUDE.md naming rules: handler parameter is `command`, never `request`; no abbreviations; no default parameters; `mediator.Send` always spans three lines
- Backend controllers never return Dto types directly — map to ViewModel via mapper
- Every controller endpoint uses a dedicated `*InputModel` for request bodies
- Frontend forms use `form()` / `[formField]` from `@angular/forms` — never `[value]` / `(input)` manual binding
- daisyUI is mandatory for all UI — never raw Tailwind for components it covers
- Conventional Commits for all commit messages

---

## Ollama Setup (do this before Task 2)

Run these commands in your terminal before starting Task 2:

```bash
# Install Ollama (macOS)
brew install ollama

# Start the Ollama server (leave this running in a terminal)
ollama serve

# Pull the model (in a separate terminal — this takes a few minutes)
ollama pull llama3.1

# Verify it works
curl http://localhost:11434/api/chat \
  -H "Content-Type: application/json" \
  -d '{"model":"llama3.1","messages":[{"role":"user","content":"Reply with {\"ok\":true}"}],"format":"json","stream":false}'
# Expected: a JSON response containing {"ok":true}
```

If `llama3.1` is slow on your machine, `mistral` is smaller: `ollama pull mistral` and update `OllamaModel` in appsettings accordingly.

---

## Test Fixtures (do this before Task 3)

Create two PDF fixtures for the integration and unit tests:

```
backend/Yaam.Tests.Integration/Fixtures/sample-cv.pdf   ← a real text-based PDF
backend/Yaam.Tests.Integration/Fixtures/empty-cv.pdf    ← a PDF with no text content
```

For `sample-cv.pdf`: export any text document as PDF (e.g. from LibreOffice, Word, or any online tool). It just needs to contain some text — content doesn't matter.

For `empty-cv.pdf`: create a PDF with no text (a blank page). LibreOffice → New Document → Export as PDF → blank content.

Register both as test project resources so they copy to the output directory. Add to `Yaam.Tests.Integration.csproj`:

```xml
<ItemGroup>
  <None Update="Fixtures\sample-cv.pdf">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
  <None Update="Fixtures\empty-cv.pdf">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## File Map

**New files — backend:**
| File | Responsibility |
|------|---------------|
| `Yaam.UseCases/Common/Cv/ICvParser.cs` | Interface: text in → `ParsedCvDto` out |
| `Yaam.UseCases/Common/Cv/ParsedCvDto.cs` | Records representing AI-extracted profile data |
| `Yaam.UseCases/Common/Cv/CvSettings.cs` | Settings POCO for CV provider config |
| `Yaam.Infrastructure/Cv/OllamaCvParser.cs` | Sends text to Ollama, deserialises JSON response |
| `Yaam.UseCases/Profile/Commands/ParseCvCommand.cs` | Extracts PDF text with PdfPig, calls `ICvParser` |
| `Yaam.UseCases/Profile/Commands/ApplyParsedCvCommand.cs` | Writes selected items to profile (add or replace) |
| `Yaam.API/Profile/CvController.cs` | `POST /api/profile/cv/parse` and `POST /api/profile/cv/apply` plus all input/view model types |
| `Yaam.Tests.Unit/Profile/ParseCvCommandTests.cs` | Unit tests for `ParseCvCommand` |
| `Yaam.Tests.Integration/Profile/CvEndpointsTests.cs` | Integration tests for both endpoints |

**Modified files — backend:**
| File | Change |
|------|--------|
| `Yaam.Infrastructure/DependencyInjection.cs` | Register `ICvParser → OllamaCvParser`, bind `CvSettings` |
| `Yaam.API/appsettings.Development.json` | Add `Cv` section |
| `Yaam.Infrastructure/Yaam.Infrastructure.csproj` | Add `UglyToad.PdfPig` |
| `Yaam.UseCases/Yaam.UseCases.csproj` | Add `UglyToad.PdfPig` |
| `Yaam.Tests.Integration/ApiFactory.cs` | Register `NoOpCvParser` |
| `Yaam.Tests.Integration/Yaam.Tests.Integration.csproj` | Add fixture `None` entries |

**New files — frontend** (after API regeneration):
| File | Responsibility |
|------|---------------|
| `features/profile/components/cv-upload/cv-upload.component.ts` | File picker, validates PDF/2MB, calls parse, emits result |
| `features/profile/components/cv-upload/cv-upload.component.html` | Upload UI |
| `features/profile/components/cv-review/cv-review.component.ts` | Section checklists with inline editing, emits confirmed selection |
| `features/profile/components/cv-review/cv-review.component.html` | Review UI |

**Modified files — frontend:**
| File | Change |
|------|--------|
| `features/profile/pages/profile-page/profile-page.component.ts` | Add cv-upload/cv-review modal flow |
| `features/profile/pages/profile-page/profile-page.component.html` | Add "Upload CV" button + modal |

---

## Task 1: ICvParser interface and ParsedCvDto

**Files:**
- Create: `backend/Yaam.UseCases/Common/Cv/ICvParser.cs`
- Create: `backend/Yaam.UseCases/Common/Cv/ParsedCvDto.cs`
- Create: `backend/Yaam.UseCases/Common/Cv/CvSettings.cs`

**Interfaces:**
- Produces: `ICvParser`, `ParsedCvDto` and all nested Dto types, `CvSettings` — all used by Tasks 2, 3, 4

- [ ] **Step 1: Create `ParsedCvDto.cs`**

```csharp
// backend/Yaam.UseCases/Common/Cv/ParsedCvDto.cs
namespace Yaam.UseCases.Common.Cv;

public record ParsedCvDto(
    ParsedInfoDto? Info,
    List<ParsedWorkExperienceDto> WorkExperiences,
    List<ParsedEducationDto> Educations,
    List<string> Skills,
    List<ParsedLanguageDto> Languages,
    List<ParsedCertificationDto> Certifications);

public record ParsedInfoDto(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Location,
    string? Summary);

// Dates are ISO strings ("2023-05" or "2023-05-01") — the AI returns them as text.
// DateOnly parsing happens in ApplyParsedCvCommand.
public record ParsedWorkExperienceDto(
    string Company,
    string Title,
    string? StartDate,
    string? EndDate,
    string? Description);

public record ParsedEducationDto(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    string? StartDate,
    string? EndDate);

public record ParsedLanguageDto(
    string Name,
    string Proficiency);

public record ParsedCertificationDto(
    string Name,
    string? Issuer,
    string? Date);
```

- [ ] **Step 2: Create `ICvParser.cs`**

```csharp
// backend/Yaam.UseCases/Common/Cv/ICvParser.cs
namespace Yaam.UseCases.Common.Cv;

public interface ICvParser
{
    Task<ParsedCvDto> ParseAsync(string text, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Create `CvSettings.cs`**

```csharp
// backend/Yaam.UseCases/Common/Cv/CvSettings.cs
namespace Yaam.UseCases.Common.Cv;

public class CvSettings
{
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "llama3.1";
}
```

- [ ] **Step 4: Build to verify no errors**

```bash
cd backend && dotnet build Yaam.UseCases
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add backend/Yaam.UseCases/Common/Cv/
git commit -m "feat(cv-parsing): add ICvParser interface and ParsedCvDto"
```

---

## Task 2: OllamaCvParser infrastructure implementation

**Files:**
- Create: `backend/Yaam.Infrastructure/Cv/OllamaCvParser.cs`
- Modify: `backend/Yaam.Infrastructure/DependencyInjection.cs`
- Modify: `backend/Yaam.Infrastructure/Yaam.Infrastructure.csproj` — add `UglyToad.PdfPig`
- Modify: `backend/Yaam.API/appsettings.Development.json` — add `Cv` section

**Interfaces:**
- Consumes: `ICvParser`, `ParsedCvDto`, `CvSettings` from Task 1
- Produces: `OllamaCvParser` registered as `ICvParser` in DI

- [ ] **Step 1: Add PdfPig to Infrastructure (needed later in Task 3, add now since we're touching the csproj)**

```bash
cd backend && dotnet add Yaam.Infrastructure package UglyToad.PdfPig
```

- [ ] **Step 2: Create `OllamaCvParser.cs`**

```csharp
// backend/Yaam.Infrastructure/Cv/OllamaCvParser.cs
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

    private static string BuildPrompt(string text) => $"""
        Extract structured data from the following CV text and return it as JSON matching this exact schema.
        Return ONLY the JSON object, no other text or markdown.

        Schema:
        {{
          "Info": {{ "FirstName": string|null, "LastName": string|null, "Email": string|null, "Phone": string|null, "Location": string|null, "Summary": string|null }},
          "WorkExperiences": [{{ "Company": string, "Title": string, "StartDate": "YYYY-MM"|null, "EndDate": "YYYY-MM"|null, "Description": string|null }}],
          "Educations": [{{ "Institution": string, "Degree": string|null, "FieldOfStudy": string|null, "StartDate": "YYYY-MM"|null, "EndDate": "YYYY-MM"|null }}],
          "Skills": [string],
          "Languages": [{{ "Name": string, "Proficiency": "Basic"|"BusinessProficiency"|"Fluent"|"Native" }}],
          "Certifications": [{{ "Name": string, "Issuer": string|null, "Date": "YYYY-MM"|null }}]
        }}

        Use null for missing fields. For Proficiency, pick the closest match:
        Basic=elementary, BusinessProficiency=working/professional, Fluent=full professional, Native=mother tongue.

        CV text:
        {text}
        """;
}
```

- [ ] **Step 3: Register in DI and bind settings**

In `backend/Yaam.Infrastructure/DependencyInjection.cs`, add after the existing registrations:

```csharp
// existing using directives + add:
using Yaam.Infrastructure.Cv;
using Yaam.UseCases.Common.Cv;

// inside AddInfrastructure, after the email block:
services.Configure<CvSettings>(configuration.GetSection("Cv"));
services.AddHttpClient<ICvParser, OllamaCvParser>();
```

- [ ] **Step 4: Add `Cv` section to `appsettings.Development.json`**

Add after the existing `"Email"` block:

```json
"Cv": {
  "OllamaBaseUrl": "http://localhost:11434",
  "OllamaModel": "llama3.1"
}
```

- [ ] **Step 5: Build**

```bash
cd backend && dotnet build Yaam.Infrastructure
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add backend/Yaam.Infrastructure/ backend/Yaam.API/appsettings.Development.json
git commit -m "feat(cv-parsing): add OllamaCvParser and wire into DI"
```

---

## Task 3: ParseCvCommand

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/ParseCvCommand.cs`
- Modify: `backend/Yaam.UseCases/Yaam.UseCases.csproj` — add `UglyToad.PdfPig`
- Create: `backend/Yaam.Tests.Unit/Profile/ParseCvCommandTests.cs`

**Interfaces:**
- Consumes: `ICvParser`, `ParsedCvDto` (Task 1)
- Produces: `ParseCvCommand`, `ParseCvCommandHandler` — consumed by Task 5 (controller)

- [ ] **Step 1: Add PdfPig to UseCases**

```bash
cd backend && dotnet add Yaam.UseCases package UglyToad.PdfPig
```

- [ ] **Step 2: Write the failing unit tests**

```csharp
// backend/Yaam.Tests.Unit/Profile/ParseCvCommandTests.cs
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
        var emptyPdf = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", 
                "Yaam.Tests.Integration", "Fixtures", "empty-cv.pdf"));
        var handler = new ParseCvCommandHandler(_parser);

        var act = () => handler.Handle(new ParseCvCommand(emptyPdf), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*scanned*");
    }

    [Fact]
    public async Task Handle_ValidPdf_ReturnsParsedResult()
    {
        var samplePdf = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "Yaam.Tests.Integration", "Fixtures", "sample-cv.pdf"));
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
}
```

- [ ] **Step 3: Run tests to confirm they fail**

```bash
cd backend && dotnet test Yaam.Tests.Unit --filter "ParseCvCommandTests" -v minimal
```
Expected: FAIL — `ParseCvCommand` and `ParseCvCommandHandler` do not exist yet.

- [ ] **Step 4: Create `ParseCvCommand.cs`**

```csharp
// backend/Yaam.UseCases/Profile/Commands/ParseCvCommand.cs
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
```

- [ ] **Step 5: Run tests to confirm they pass**

```bash
cd backend && dotnet test Yaam.Tests.Unit --filter "ParseCvCommandTests" -v minimal
```
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add backend/Yaam.UseCases/ backend/Yaam.Tests.Unit/Profile/ParseCvCommandTests.cs
git commit -m "feat(cv-parsing): add ParseCvCommand with PdfPig text extraction"
```

---

## Task 4: ApplyParsedCvCommand

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/ApplyParsedCvCommand.cs`

**Interfaces:**
- Consumes: `ParsedCvDto` (Task 1), `IProfileRepository`, `ProfileMapper`, `ProfileDto`
- Produces: `ApplyParsedCvCommand`, `CvApplyMode`, `ApplyParsedCvCommandHandler` — consumed by Task 5

Note: The handler writes directly via `IProfileRepository` — it does not dispatch individual `Add*` commands through MediatR, to keep the operation atomic and avoid N mediator calls in a loop.

- [ ] **Step 1: Create `ApplyParsedCvCommand.cs`**

```csharp
// backend/Yaam.UseCases/Profile/Commands/ApplyParsedCvCommand.cs
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
            profile.WorkExperiences.Add(new WorkExperience
            {
                ProfileId = profile.Id,
                Company = we.Company,
                Title = we.Title,
                StartDate = ParseDate(we.StartDate) ?? DateOnly.MinValue,
                EndDate = ParseDate(we.EndDate),
                Description = we.Description,
            });

        foreach (var edu in items.Educations)
            profile.Educations.Add(new Education
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
            profile.Languages.Add(new Language
            {
                ProfileId = profile.Id,
                Name = lang.Name,
                Proficiency = Enum.TryParse<Yaam.Domain.Enums.LanguageProficiency>(
                    lang.Proficiency, out var proficiency)
                    ? proficiency
                    : Yaam.Domain.Enums.LanguageProficiency.Basic,
            });

        foreach (var cert in items.Certifications)
            profile.Certifications.Add(new Certification
            {
                ProfileId = profile.Id,
                Name = cert.Name,
                Issuer = cert.Issuer,
                Date = ParseDate(cert.Date) ?? DateOnly.MinValue,
            });

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
```

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build Yaam.UseCases
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add backend/Yaam.UseCases/Profile/Commands/ApplyParsedCvCommand.cs
git commit -m "feat(cv-parsing): add ApplyParsedCvCommand with add and replace modes"
```

---

## Task 5: CvController and integration tests

**Files:**
- Create: `backend/Yaam.API/Profile/CvController.cs`
- Modify: `backend/Yaam.Tests.Integration/ApiFactory.cs` — register `NoOpCvParser`
- Create: `backend/Yaam.Tests.Integration/Profile/CvEndpointsTests.cs`

**Interfaces:**
- Consumes: `ParseCvCommand`, `ApplyParsedCvCommand`, `CvApplyMode` (Tasks 3–4), `ProfileViewModelMapper`
- Produces: `POST /api/profile/cv/parse`, `POST /api/profile/cv/apply`

- [ ] **Step 1: Create `CvController.cs`**

```csharp
// backend/Yaam.API/Profile/CvController.cs
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
```

- [ ] **Step 2: Register `NoOpCvParser` in `ApiFactory.cs`**

Add to the `ConfigureServices` block in `ApiFactory.cs`, after the existing `RemoveAll<IEmailSender>` lines:

```csharp
// existing using directives + add:
using Yaam.UseCases.Common.Cv;

// inside ConfigureServices:
services.RemoveAll<ICvParser>();
services.AddSingleton<ICvParser, NoOpCvParser>();
```

Add the `NoOpCvParser` class at the bottom of `ApiFactory.cs`:

```csharp
public class NoOpCvParser : ICvParser
{
    public Task<ParsedCvDto> ParseAsync(string text, CancellationToken cancellationToken) =>
        Task.FromResult(new ParsedCvDto(
            new ParsedInfoDto("Ada", "Lovelace", "ada@example.com", "+41 79 000 00 00", null, null),
            [new ParsedWorkExperienceDto("Acme", "Engineer", "2020-01", null, "Built things.")],
            [],
            ["C#", "Angular"],
            [new ParsedLanguageDto("English", "Native")],
            []));
}
```

- [ ] **Step 3: Write integration tests**

```csharp
// backend/Yaam.Tests.Integration/Profile/CvEndpointsTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.API.Profile;
using Yaam.UseCases.Profile.Commands;

namespace Yaam.Tests.Integration.Profile;

[Collection("Integration")]
public class CvEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_CvParse_ValidPdf_ReturnsParsedData()
    {
        var pdfBytes = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-cv.pdf"));
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(pdfBytes), "file", "cv.pdf");

        var response = await _client.PostAsync("/api/profile/cv/parse", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParsedCvViewModel>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        // NoOpCvParser returns known data
        result.Data.Info!.FirstName.Should().Be("Ada");
        result.Data.Skills.Should().Contain("C#");
    }

    [Fact]
    public async Task POST_CvParse_EmptyPdf_Returns409()
    {
        var pdfBytes = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "empty-cv.pdf"));
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(pdfBytes), "file", "cv.pdf");

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
}
```

- [ ] **Step 4: Run tests to confirm they fail (endpoint not yet wired)**

```bash
cd backend && dotnet test Yaam.Tests.Integration --filter "CvEndpointsTests" -v minimal
```
Expected: FAIL

- [ ] **Step 5: Build the full solution**

```bash
cd backend && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 6: Run integration tests again**

```bash
cd backend && dotnet test Yaam.Tests.Integration --filter "CvEndpointsTests" -v minimal
```
Expected: all 4 tests PASS.

- [ ] **Step 7: Run the full test suite**

```bash
cd backend && dotnet test
```
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.API/Profile/CvController.cs backend/Yaam.Tests.Integration/
git commit -m "feat(cv-parsing): add CvController with parse and apply endpoints"
```

---

## Task 6: Regenerate Angular API client

**Files:**
- Modify: `frontend/src/app/generated/api/` (owned by generator — do not edit manually)

- [ ] **Step 1: Start the backend**

```bash
cd backend && dotnet run --project Yaam.API
```
Leave this running. The generator pulls the OpenAPI spec from the live server.

- [ ] **Step 2: Regenerate in a second terminal**

```bash
cd frontend && npm run generate:api
```
Expected: files updated under `src/app/generated/api/`.

- [ ] **Step 3: Stop the backend** (`Ctrl+C`)

- [ ] **Step 4: Build the frontend to confirm no type errors**

```bash
cd frontend && npx ng build --configuration development
```
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/generated/
git commit -m "chore: regenerate API client after CV parsing endpoints"
```

---

## Task 7: cv-upload component

**Files:**
- Create: `frontend/src/app/features/profile/components/cv-upload/cv-upload.component.ts`
- Create: `frontend/src/app/features/profile/components/cv-upload/cv-upload.component.html`

**Interfaces:**
- Produces: `CvUploadComponent` with output `parsed: OutputEmitterRef<ParsedCvViewModel>` and `error: OutputEmitterRef<string>` — consumed by Task 9

- [ ] **Step 1: Create `cv-upload.component.ts`**

```typescript
// frontend/src/app/features/profile/components/cv-upload/cv-upload.component.ts
import { Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ParsedCvViewModel, ProfileService } from '../../../../generated/api';

@Component({
  selector: 'app-cv-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cv-upload.component.html',
})
export class CvUploadComponent {
  readonly parsed = output<ParsedCvViewModel>();
  readonly error = output<string>();

  private readonly profileService = inject(ProfileService);

  protected readonly selectedFile = signal<File | null>(null);
  protected readonly isParsing = signal(false);
  protected readonly validationError = signal<string | null>(null);

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.validationError.set(null);

    if (!file) return;
    if (file.type !== 'application/pdf') {
      this.validationError.set('Only PDF files are accepted.');
      return;
    }
    if (file.size > 2 * 1024 * 1024) {
      this.validationError.set('File exceeds the 2 MB limit.');
      return;
    }
    this.selectedFile.set(file);
  }

  protected async onUpload(): Promise<void> {
    const file = this.selectedFile();
    if (!file) return;

    this.isParsing.set(true);
    this.validationError.set(null);
    try {
      const result = await firstValueFrom(
        this.profileService.parseCv({ file })
      );
      this.parsed.emit(result);
    } catch {
      this.error.emit('Parsing failed. Please check that the PDF contains text and try again.');
    } finally {
      this.isParsing.set(false);
    }
  }
}
```

- [ ] **Step 2: Invoke the daisyUI skill**

```
/daisyui
```

Then create `cv-upload.component.html` using daisyUI classes per the skill's guidance. The template needs:
- A `<label>` styled as a daisyUI `btn` that wraps a hidden `<input type="file" accept=".pdf">` with `(change)="onFileSelected($event)"`
- Display the selected file name when `selectedFile()` is set
- An upload `<button class="btn btn-primary">` that calls `onUpload()`, disabled when `isParsing()` or `!selectedFile()`
- A `<span class="loading loading-spinner">` inside the button when `isParsing()`
- A daisyUI `alert alert-error` showing `validationError()` when non-null

- [ ] **Step 3: Build to verify no type errors**

```bash
cd frontend && npx ng build --configuration development
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/profile/components/cv-upload/
git commit -m "feat(cv-parsing): add cv-upload component"
```

---

## Task 8: cv-review component

**Files:**
- Create: `frontend/src/app/features/profile/components/cv-review/cv-review.component.ts`
- Create: `frontend/src/app/features/profile/components/cv-review/cv-review.component.html`

**Interfaces:**
- Consumes: `ParsedCvViewModel` and `ParsedCvData` types from generated API (Task 6)
- Produces: `CvReviewComponent` with inputs `parsedData`, `hasExistingData` and outputs `confirmed`, `cancelled` — consumed by Task 9

- [ ] **Step 1: Create `cv-review.component.ts`**

```typescript
// frontend/src/app/features/profile/components/cv-review/cv-review.component.ts
import { Component, computed, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ParsedCvData, ParsedWorkExperienceData, ParsedEducationData, ParsedLanguageData, ParsedCertificationData } from '../../../../generated/api';

export interface CvReviewConfirmation {
  selectedItems: ParsedCvData;
  mode: 'Add' | 'Replace';
}

type CheckedItem<T> = { checked: boolean; data: T };

@Component({
  selector: 'app-cv-review',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cv-review.component.html',
})
export class CvReviewComponent {
  readonly parsedData = input.required<ParsedCvData>();
  readonly hasExistingData = input.required<boolean>();
  readonly confirmed = output<CvReviewConfirmation>();
  readonly cancelled = output<void>();

  protected readonly mode = signal<'Add' | 'Replace'>('Add');
  protected readonly showReplaceConfirm = signal(false);

  protected readonly infoChecked = signal(true);

  protected readonly workExperiences = computed<CheckedItem<ParsedWorkExperienceData>[]>(() =>
    this.parsedData().workExperiences.map((d) => ({ checked: true, data: d }))
  );
  private readonly _workExperiences = signal<CheckedItem<ParsedWorkExperienceData>[]>([]);

  protected readonly educations = signal<CheckedItem<ParsedEducationData>[]>([]);
  protected readonly languages = signal<CheckedItem<ParsedLanguageData>[]>([]);
  protected readonly certifications = signal<CheckedItem<ParsedCertificationData>[]>([]);
  protected readonly skills = signal<CheckedItem<string>[]>([]);

  ngOnInit(): void {
    const d = this.parsedData();
    this._workExperiences.set(d.workExperiences.map((data) => ({ checked: true, data })));
    this.educations.set(d.educations.map((data) => ({ checked: true, data })));
    this.languages.set(d.languages.map((data) => ({ checked: true, data })));
    this.certifications.set(d.certifications.map((data) => ({ checked: true, data })));
    this.skills.set(d.skills.map((data) => ({ checked: true, data })));
  }

  protected toggleWorkExperience(index: number): void {
    const items = [...this._workExperiences()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this._workExperiences.set(items);
  }

  protected toggleEducation(index: number): void {
    const items = [...this.educations()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this.educations.set(items);
  }

  protected toggleLanguage(index: number): void {
    const items = [...this.languages()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this.languages.set(items);
  }

  protected toggleCertification(index: number): void {
    const items = [...this.certifications()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this.certifications.set(items);
  }

  protected toggleSkill(index: number): void {
    const items = [...this.skills()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this.skills.set(items);
  }

  protected onConfirm(): void {
    if (this.mode() === 'Replace' && !this.showReplaceConfirm()) {
      this.showReplaceConfirm.set(true);
      return;
    }
    this.emit();
  }

  protected onReplaceConfirmed(): void {
    this.showReplaceConfirm.set(false);
    this.emit();
  }

  protected onReplaceCancelled(): void {
    this.showReplaceConfirm.set(false);
  }

  private emit(): void {
    const d = this.parsedData();
    const selectedItems: ParsedCvData = {
      info: this.infoChecked() ? d.info : null,
      workExperiences: this._workExperiences().filter((i) => i.checked).map((i) => i.data),
      educations: this.educations().filter((i) => i.checked).map((i) => i.data),
      skills: this.skills().filter((i) => i.checked).map((i) => i.data),
      languages: this.languages().filter((i) => i.checked).map((i) => i.data),
      certifications: this.certifications().filter((i) => i.checked).map((i) => i.data),
    };
    this.confirmed.emit({ selectedItems, mode: this.mode() });
  }
}
```

- [ ] **Step 2: Create `cv-review.component.html`** using daisyUI

The template must cover (using daisyUI classes):

- **Merge mode selector** (shown only when `hasExistingData()`): two radio buttons — "Add to profile" / "Replace entire profile". On change, call `mode.set(...)`.
- **Info section**: checkbox to include info fields. When `parsedData().info` is null, show `<p class="text-sm opacity-60">No personal info found in your CV.</p>`.
- **Work Experience section**: loop `_workExperiences()`, each row with a checkbox (`(change)="toggleWorkExperience(i)"`) and displays `company`, `title`, `startDate`–`endDate`. When empty, show `<p class="text-sm opacity-60">No work experience found in your CV.</p>`.
- Repeat the same pattern for Education, Skills, Languages, Certifications — each with its empty-state notice.
- **Replace confirm dialog**: a daisyUI `modal` shown when `showReplaceConfirm()`, with a warning message and "Confirm replace" / "Cancel" buttons.
- **Footer**: a "Save to profile" `btn btn-primary` that calls `onConfirm()`, and a "Cancel" `btn btn-ghost` that emits `cancelled`.

- [ ] **Step 3: Build to verify no type errors**

```bash
cd frontend && npx ng build --configuration development
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/profile/components/cv-review/
git commit -m "feat(cv-parsing): add cv-review component with section checklists"
```

---

## Task 9: Wire CV flow into profile page

**Files:**
- Modify: `frontend/src/app/features/profile/pages/profile-page/profile-page.component.ts`
- Modify: `frontend/src/app/features/profile/pages/profile-page/profile-page.component.html`

**Interfaces:**
- Consumes: `CvUploadComponent` (Task 7), `CvReviewComponent` + `CvReviewConfirmation` (Task 8), generated `ProfileService.applyCv`

- [ ] **Step 1: Update `profile-page.component.ts`**

Add the cv upload/review state and apply logic:

```typescript
// Add to existing imports:
import { CvUploadComponent } from '../../components/cv-upload/cv-upload.component';
import { CvReviewComponent, CvReviewConfirmation } from '../../components/cv-review/cv-review.component';
import { ParsedCvViewModel } from '../../../../generated/api';

// In @Component imports array, add: CvUploadComponent, CvReviewComponent

// In class body, add:
protected readonly showCvModal = signal(false);
protected readonly parsedCv = signal<ParsedCvViewModel | null>(null);
protected readonly cvError = signal<string | null>(null);
protected readonly isApplying = signal(false);

protected get hasExistingProfileData(): boolean {
  const p = this.profileResource.value();
  return !!(
    p && (
      (p.workExperiences?.length ?? 0) > 0 ||
      (p.educations?.length ?? 0) > 0 ||
      (p.skills?.length ?? 0) > 0
    )
  );
}

protected onOpenCvModal(): void {
  this.parsedCv.set(null);
  this.cvError.set(null);
  this.showCvModal.set(true);
}

protected onCvParsed(result: ParsedCvViewModel): void {
  this.parsedCv.set(result);
}

protected onCvParseError(message: string): void {
  this.cvError.set(message);
}

protected onCvReviewCancelled(): void {
  this.showCvModal.set(false);
}

protected async onCvReviewConfirmed(confirmation: CvReviewConfirmation): Promise<void> {
  this.isApplying.set(true);
  try {
    const profile = await firstValueFrom(
      this.profileService.applyCv({
        selectedItems: confirmation.selectedItems,
        mode: confirmation.mode,
      })
    );
    this.profileResource.set(profile);
    this.showCvModal.set(false);
  } finally {
    this.isApplying.set(false);
  }
}
```

- [ ] **Step 2: Update `profile-page.component.html`**

Add the following to the profile page template (using daisyUI):

1. An "Upload CV" `<button class="btn btn-outline btn-sm">` that calls `onOpenCvModal()` — place it near the page heading.

2. A daisyUI modal (`<dialog>` with the `modal` class) that is shown when `showCvModal()`:
   - When `!parsedCv()`: show `<app-cv-upload (parsed)="onCvParsed($event)" (error)="onCvParseError($event)" />`; show `cvError()` in a `alert alert-error` if set.
   - When `parsedCv()`: show `<app-cv-review [parsedData]="parsedCv()!.data" [hasExistingData]="hasExistingProfileData" (confirmed)="onCvReviewConfirmed($event)" (cancelled)="onCvReviewCancelled()" />`
   - Show a `loading loading-spinner` overlay when `isApplying()`.

- [ ] **Step 3: Build**

```bash
cd frontend && npx ng build --configuration development
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Invoke the verify skill to confirm the feature works end-to-end**

```
/verify
```

Test the golden path:
1. Open the profile page — confirm "Upload CV" button is visible
2. Click "Upload CV" — confirm modal opens with file picker
3. Select a text-based PDF — confirm the upload button activates
4. Click upload — confirm loading spinner appears, then the review UI shows
5. Uncheck one item in the review — confirm it is excluded on save
6. Click "Save to profile" — confirm the profile page refreshes with the new data
7. Reopen the modal and upload again — confirm the merge mode selector appears (since data now exists)
8. Select "Replace entire profile" — confirm the confirmation dialog appears before saving

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/profile/
git commit -m "feat(cv-parsing): wire cv upload and review flow into profile page"
```

---

## Self-Review

**Spec coverage:**
- ✅ Story 1: Upload → loading → review checklist → editable → save (Tasks 7–9)
- ✅ Story 1: Scanned PDF error (Task 3 — `ConflictException` → 409)
- ✅ Story 1: Empty section notice (Task 8 template)
- ✅ Story 2: Reupload shows merge mode selector (Task 9 — `hasExistingProfileData`)
- ✅ Story 2: Add mode preserves existing data (Task 4, integration test)
- ✅ Story 2: Replace mode clears and replaces (Task 4, integration test)
- ✅ Story 2: Replace confirmation dialog (Task 8)
- ✅ ICvParser abstraction — production provider swap is infrastructure-only (Task 1–2)
- ✅ Ollama setup guide — top of plan
- ✅ 2 MB file size limit — enforced in controller (Task 5) and frontend (Task 7)

**Placeholder scan:** No TBD or TODO.

**Type consistency:**
- `ParsedCvData` defined in Task 5, referenced in Tasks 7–9 — consistent
- `CvReviewConfirmation.mode` is `'Add' | 'Replace'` (frontend) matching `CvApplyMode` enum (backend) — consistent via generated API
- `_workExperiences` private signal used in `toggleWorkExperience` and `emit()` — consistent
