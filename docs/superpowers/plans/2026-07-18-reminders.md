# Reminders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full-stack reminders (Stories 1–3) on `feat/reminders`: .NET backend with CQRS, a daily email notification BackgroundService, MailKit SMTP sender, and Angular frontend with a reminder section on the application detail and a dedicated reminders list page.

**Architecture:** Clean Architecture backend (Domain → UseCases → Infrastructure → API), CQRS with MediatR. `Reminder` is a 1:1 child of `Application` (one active reminder per application). All reminder display states (Upcoming/Due today/Overdue/Done) are computed from `DueDate` vs today — only `NotifiedAt` and `CompletedAt` are stored. `IEmailSender` in UseCases layer; `SmtpEmailSender` (MailKit) in Infrastructure. Background job is a .NET `BackgroundService` that wakes at 08:00 UTC daily and dispatches `SendDueRemindersCommand` via MediatR.

**Tech Stack:** .NET 10, MediatR 14, FluentValidation 12, EF Core 10 + Npgsql, MailKit, xUnit, FluentAssertions, NSubstitute, Angular 22, daisyUI, `@angular/forms/signals`.

## Global Constraints

- Branch: `feat/reminders` — never commit to `main`
- .NET: `net10.0`; Nullable enabled; ImplicitUsings enabled
- No AutoMapper — map explicitly in static `*Mapper` classes with `ToDto` / `ToViewModel` methods
- Handler parameter: `command` for commands, `query` for queries. Never `request`
- `mediator.Send` always: command/query on its own line, `cancellationToken` on third line
- `CancellationToken cancellationToken` — never `ct`, never `= default`
- No type aliases unless genuinely needed to resolve ambiguity
- Angular: `form()` / `[formField]` from `@angular/forms/signals` — never `ReactiveFormsModule`, `FormBuilder`, `FormGroup`
- Angular: `resource()` + `firstValueFrom()` — never `rxResource`
- daisyUI for all UI components — no raw Tailwind for things daisyUI covers
- No comments unless the WHY is non-obvious

---

## File Map

**New backend files:**
```
backend/Yaam.Domain/Entities/Reminder.cs
backend/Yaam.Domain/Repositories/IReminderRepository.cs
backend/Yaam.Domain/Errors/ConflictException.cs
backend/Yaam.Infrastructure/Persistence/Configurations/ReminderConfiguration.cs
backend/Yaam.Infrastructure/Repositories/ReminderRepository.cs
backend/Yaam.Infrastructure/Email/EmailSettings.cs
backend/Yaam.Infrastructure/Email/SmtpEmailSender.cs
backend/Yaam.UseCases/Common/Email/IEmailSender.cs
backend/Yaam.UseCases/Reminders/ReminderMapper.cs
backend/Yaam.UseCases/Reminders/Dtos/ApplicationReminderDto.cs
backend/Yaam.UseCases/Reminders/Dtos/ReminderSummaryDto.cs
backend/Yaam.UseCases/Reminders/Queries/GetActiveRemindersQuery.cs
backend/Yaam.UseCases/Reminders/Commands/SetReminderCommand.cs
backend/Yaam.UseCases/Reminders/Commands/CompleteReminderCommand.cs
backend/Yaam.UseCases/Reminders/Commands/RescheduleReminderCommand.cs
backend/Yaam.UseCases/Reminders/Commands/DeleteReminderCommand.cs
backend/Yaam.UseCases/Reminders/Commands/SendDueRemindersCommand.cs
backend/Yaam.API/Reminders/ReminderViewModel.cs
backend/Yaam.API/Reminders/ReminderViewModelMapper.cs
backend/Yaam.API/Reminders/ReminderNotificationService.cs
backend/Yaam.API/Controllers/ApplicationReminderController.cs
backend/Yaam.API/Controllers/RemindersController.cs
backend/Yaam.Tests.Unit/Reminders/SetReminderCommandValidatorTests.cs
backend/Yaam.Tests.Unit/Reminders/RescheduleReminderCommandValidatorTests.cs
backend/Yaam.Tests.Unit/Reminders/SendDueRemindersCommandHandlerTests.cs
backend/Yaam.Tests.Integration/Reminders/RemindersEndpointsTests.cs
```

**Modified backend files:**
```
backend/Yaam.Domain/Entities/Application.cs              (add Reminder? nav property)
backend/Yaam.Infrastructure/Persistence/AppDbContext.cs   (add DbSet<Reminder>)
backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs  (Include Reminder)
backend/Yaam.Infrastructure/DependencyInjection.cs        (register repo + email sender)
backend/Yaam.UseCases/Applications/Dtos/ApplicationDto.cs (add ApplicationReminderDto? Reminder)
backend/Yaam.UseCases/Applications/ApplicationMapper.cs   (map Reminder)
backend/Yaam.API/Applications/ApplicationViewModel.cs     (add ApplicationReminderViewModel?)
backend/Yaam.API/Applications/ApplicationViewModelMapper.cs (map Reminder)
backend/Yaam.API/GlobalExceptionHandler.cs                (add ConflictException → 409)
backend/Yaam.API/Program.cs                               (register BackgroundService)
backend/Yaam.API/appsettings.json                         (add Email section)
backend/Yaam.API/appsettings.Development.json             (Mailpit config)
backend/Yaam.Tests.Integration/ApiFactory.cs              (register NoOpEmailSender)
```

**New frontend files:**
```
frontend/src/app/features/applications/components/reminder-section/reminder-section.component.ts
frontend/src/app/features/applications/components/reminder-section/reminder-section.component.html
frontend/src/app/features/reminders/pages/reminders-list/reminders-list.component.ts
frontend/src/app/features/reminders/pages/reminders-list/reminders-list.component.html
```

**Modified frontend files:**
```
frontend/src/app/features/applications/pages/application-detail/application-detail.component.html
frontend/src/app/features/reminders/reminders.routes.ts
```

---

## Task 1: Domain layer

**Files:**
- Create: `backend/Yaam.Domain/Entities/Reminder.cs`
- Create: `backend/Yaam.Domain/Repositories/IReminderRepository.cs`
- Create: `backend/Yaam.Domain/Errors/ConflictException.cs`
- Modify: `backend/Yaam.Domain/Entities/Application.cs`

**Interfaces:**
- Produces: `Reminder`, `IReminderRepository`, `ConflictException` — consumed by Tasks 2–6

- [ ] **Step 1: Create the Reminder entity**

Create `backend/Yaam.Domain/Entities/Reminder.cs`:

```csharp
namespace Yaam.Domain.Entities;

public class Reminder : Entity
{
    public Guid ApplicationId { get; set; }
    public int DelayDays { get; set; }
    public DateOnly DueDate { get; set; }
    public string? Note { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Application Application { get; set; } = null!;
}
```

- [ ] **Step 2: Add the Reminder navigation property to Application**

Modify `backend/Yaam.Domain/Entities/Application.cs` — add one property at the end:

```csharp
using Yaam.Domain.Enums;

namespace Yaam.Domain.Entities;

public class Application : Entity
{
    public required string CompanyName { get; set; }
    public required string Role { get; set; }
    public DateOnly? DateApplied { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? JobPosting { get; set; }
    public ICollection<ApplicationNote> Notes { get; set; } = new List<ApplicationNote>();
    public Reminder? Reminder { get; set; }
}
```

- [ ] **Step 3: Create IReminderRepository**

Create `backend/Yaam.Domain/Repositories/IReminderRepository.cs`:

```csharp
using Yaam.Domain.Entities;

namespace Yaam.Domain.Repositories;

public interface IReminderRepository
{
    Task<Reminder?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken);
    Task<List<Reminder>> GetAllActiveAsync(CancellationToken cancellationToken);
    Task<List<Reminder>> GetDueAsync(DateOnly today, CancellationToken cancellationToken);
    Task<Reminder> AddAsync(Reminder reminder, CancellationToken cancellationToken);
    Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken);
    Task DeleteByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Create ConflictException**

Create `backend/Yaam.Domain/Errors/ConflictException.cs`:

```csharp
namespace Yaam.Domain.Errors;

public class ConflictException(string message) : Exception(message);
```

- [ ] **Step 5: Build domain**

```bash
cd /Users/uchendu/yaam/backend
dotnet build Yaam.Domain
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add backend/Yaam.Domain/
git commit -m "feat: add Reminder entity, IReminderRepository, and ConflictException"
```

---

## Task 2: Infrastructure layer

**Files:**
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/ReminderConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Repositories/ReminderRepository.cs`
- Modify: `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs`
- Modify: `backend/Yaam.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `Reminder`, `IReminderRepository` from Task 1
- Produces: `IReminderRepository` registered in DI; EF migration ready to run

- [ ] **Step 1: Create ReminderConfiguration**

Create `backend/Yaam.Infrastructure/Persistence/Configurations/ReminderConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.DelayDays).IsRequired();
        builder.Property(r => r.DueDate).IsRequired();
        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasOne(r => r.Application)
            .WithOne(a => a.Reminder)
            .HasForeignKey<Reminder>(r => r.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Add DbSet to AppDbContext**

Modify `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs` — add one line after `CustomFields`:

```csharp
using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationNote> ApplicationNotes => Set<ApplicationNote>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<ProfileLink> ProfileLinks => Set<ProfileLink>();
    public DbSet<CustomField> CustomFields => Set<CustomField>();
    public DbSet<Reminder> Reminders => Set<Reminder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 3: Update ApplicationRepository to include Reminder**

Modify `backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs` — update `GetByIdAsync` to add the `.Include(a => a.Reminder)` line:

```csharp
public async Task<Application?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    => await db.Applications
        .Include(a => a.Notes.OrderByDescending(n => n.CreatedAt))
        .Include(a => a.Reminder)
        .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
```

- [ ] **Step 4: Create ReminderRepository**

Create `backend/Yaam.Infrastructure/Repositories/ReminderRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;

namespace Yaam.Infrastructure.Repositories;

public class ReminderRepository(AppDbContext db) : IReminderRepository
{
    public async Task<Reminder?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken)
        => await db.Reminders
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId && r.CompletedAt == null, cancellationToken);

    public async Task<List<Reminder>> GetAllActiveAsync(CancellationToken cancellationToken)
        => await db.Reminders
            .Include(r => r.Application)
            .Where(r => r.CompletedAt == null)
            .OrderBy(r => r.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<List<Reminder>> GetDueAsync(DateOnly today, CancellationToken cancellationToken)
        => await db.Reminders
            .Include(r => r.Application)
            .Where(r => r.CompletedAt == null && r.NotifiedAt == null && r.DueDate <= today)
            .ToListAsync(cancellationToken);

    public async Task<Reminder> AddAsync(Reminder reminder, CancellationToken cancellationToken)
    {
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(cancellationToken);
        return reminder;
    }

    public async Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken)
        => await db.SaveChangesAsync(cancellationToken);

    public async Task DeleteByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var reminder = await db.Reminders
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId, cancellationToken);
        if (reminder is not null)
        {
            db.Reminders.Remove(reminder);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 5: Register IReminderRepository in DI**

Modify `backend/Yaam.Infrastructure/DependencyInjection.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;
using Yaam.Infrastructure.Repositories;

namespace Yaam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();

        return services;
    }
}
```

- [ ] **Step 6: Build full solution**

```bash
cd /Users/uchendu/yaam/backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Create and apply EF migration**

Ensure PostgreSQL is running (`docker compose up -d postgres`), then:

```bash
cd /Users/uchendu/yaam/backend
dotnet ef migrations add AddReminders \
  --project Yaam.Infrastructure \
  --startup-project Yaam.API
dotnet ef database update \
  --project Yaam.Infrastructure \
  --startup-project Yaam.API
```

Expected: migration files created under `Yaam.Infrastructure/Migrations/`, database updated.

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.Infrastructure/ backend/Yaam.Domain/
git commit -m "feat: add Reminder EF config, repository, and migration"
```

---

## Task 3: Email infrastructure

**Files:**
- Create: `backend/Yaam.UseCases/Common/Email/IEmailSender.cs`
- Create: `backend/Yaam.Infrastructure/Email/EmailSettings.cs`
- Create: `backend/Yaam.Infrastructure/Email/SmtpEmailSender.cs`
- Modify: `backend/Yaam.Infrastructure/DependencyInjection.cs`
- Modify: `backend/Yaam.API/appsettings.json`
- Modify: `backend/Yaam.API/appsettings.Development.json`

**Interfaces:**
- Produces: `IEmailSender` registered in DI; local dev routes mail to Mailpit on port 1025

- [ ] **Step 1: Add MailKit package**

```bash
cd /Users/uchendu/yaam/backend
dotnet add Yaam.Infrastructure/Yaam.Infrastructure.csproj package MailKit
```

Expected: MailKit added to `Yaam.Infrastructure.csproj`.

- [ ] **Step 2: Create IEmailSender**

Create `backend/Yaam.UseCases/Common/Email/IEmailSender.cs`:

```csharp
namespace Yaam.UseCases.Common.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Create EmailSettings**

Create `backend/Yaam.Infrastructure/Email/EmailSettings.cs`:

```csharp
namespace Yaam.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "YAAM";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string NotificationEmail { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Create SmtpEmailSender**

Create `backend/Yaam.Infrastructure/Email/SmtpEmailSender.cs`:

```csharp
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Yaam.UseCases.Common.Email;

namespace Yaam.Infrastructure.Email;

public class SmtpEmailSender(IOptions<EmailSettings> options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = textBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.None, cancellationToken);
        if (!string.IsNullOrEmpty(settings.Username))
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
```

- [ ] **Step 5: Register IEmailSender and bind EmailSettings in DI**

Modify `backend/Yaam.Infrastructure/DependencyInjection.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Email;
using Yaam.Infrastructure.Persistence;
using Yaam.Infrastructure.Repositories;
using Yaam.UseCases.Common.Email;

namespace Yaam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();

        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
```

- [ ] **Step 6: Update Program.cs to pass IConfiguration to AddInfrastructure**

Modify `backend/Yaam.API/Program.cs` — update the `AddInfrastructure` call:

```csharp
using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Yaam.API;
using Yaam.Infrastructure;
using Yaam.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required."),
    builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("Angular");
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
```

- [ ] **Step 7: Add Email section to appsettings.json**

Modify `backend/Yaam.API/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Email": {
    "SmtpHost": "",
    "SmtpPort": 587,
    "FromAddress": "noreply@yaam.app",
    "FromName": "YAAM",
    "Username": "",
    "Password": "",
    "NotificationEmail": ""
  }
}
```

- [ ] **Step 8: Add Mailpit config to appsettings.Development.json**

Modify `backend/Yaam.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=yaam;Username=yaam;Password=yaam"
  },
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "FromAddress": "noreply@yaam.local",
    "FromName": "YAAM",
    "NotificationEmail": "dev@yaam.local"
  }
}
```

- [ ] **Step 9: Build solution**

```bash
cd /Users/uchendu/yaam/backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 10: Smoke-test email delivery to Mailpit**

Start Mailpit and the API:

```bash
docker compose up -d mailpit
dotnet run --project Yaam.API
```

Open `http://localhost:8025` — Mailpit inbox should be empty and reachable.

- [ ] **Step 11: Commit**

```bash
git add backend/Yaam.Infrastructure/ backend/Yaam.UseCases/Common/ backend/Yaam.API/
git commit -m "feat: add IEmailSender, SmtpEmailSender with MailKit, and Mailpit dev config"
```

---

## Task 4: UseCases — DTOs, CRUD commands, and queries

**Files:**
- Create: `backend/Yaam.UseCases/Reminders/Dtos/ApplicationReminderDto.cs`
- Create: `backend/Yaam.UseCases/Reminders/Dtos/ReminderSummaryDto.cs`
- Create: `backend/Yaam.UseCases/Reminders/ReminderMapper.cs`
- Create: `backend/Yaam.UseCases/Reminders/Queries/GetActiveRemindersQuery.cs`
- Create: `backend/Yaam.UseCases/Reminders/Commands/SetReminderCommand.cs`
- Create: `backend/Yaam.UseCases/Reminders/Commands/CompleteReminderCommand.cs`
- Create: `backend/Yaam.UseCases/Reminders/Commands/RescheduleReminderCommand.cs`
- Create: `backend/Yaam.UseCases/Reminders/Commands/DeleteReminderCommand.cs`
- Create: `backend/Yaam.Tests.Unit/Reminders/SetReminderCommandValidatorTests.cs`
- Create: `backend/Yaam.Tests.Unit/Reminders/RescheduleReminderCommandValidatorTests.cs`
- Modify: `backend/Yaam.UseCases/Applications/Dtos/ApplicationDto.cs`
- Modify: `backend/Yaam.UseCases/Applications/ApplicationMapper.cs`

**Interfaces:**
- Consumes: `Reminder`, `IReminderRepository`, `ConflictException` from Tasks 1–2
- Produces: all CRUD handlers registered via MediatR assembly scanning; `ApplicationDto` includes embedded `ApplicationReminderDto?`

- [ ] **Step 1: Create DTOs**

Create `backend/Yaam.UseCases/Reminders/Dtos/ApplicationReminderDto.cs`:

```csharp
namespace Yaam.UseCases.Reminders.Dtos;

public record ApplicationReminderDto(
    Guid Id,
    int DelayDays,
    DateOnly DueDate,
    string? Note,
    DateTime? NotifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
```

Create `backend/Yaam.UseCases/Reminders/Dtos/ReminderSummaryDto.cs`:

```csharp
namespace Yaam.UseCases.Reminders.Dtos;

public record ReminderSummaryDto(
    Guid Id,
    Guid ApplicationId,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    string? ContactName,
    int DelayDays,
    DateOnly DueDate,
    string? Note,
    DateTime? NotifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
```

- [ ] **Step 2: Create ReminderMapper**

Create `backend/Yaam.UseCases/Reminders/ReminderMapper.cs`:

```csharp
using Yaam.Domain.Entities;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Reminders;

internal static class ReminderMapper
{
    internal static ApplicationReminderDto ToApplicationReminderDto(Reminder r) => new(
        r.Id, r.DelayDays, r.DueDate, r.Note, r.NotifiedAt, r.CompletedAt, r.CreatedAt);

    internal static ReminderSummaryDto ToSummaryDto(Reminder r) => new(
        r.Id, r.ApplicationId,
        r.Application.CompanyName, r.Application.Role, r.Application.DateApplied, r.Application.ContactName,
        r.DelayDays, r.DueDate, r.Note, r.NotifiedAt, r.CompletedAt, r.CreatedAt);
}
```

- [ ] **Step 3: Update ApplicationDto to include reminder**

Modify `backend/Yaam.UseCases/Applications/Dtos/ApplicationDto.cs`:

```csharp
using Yaam.Domain.Enums;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Applications.Dtos;

public record ApplicationDto(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ApplicationNoteDto> Notes,
    ApplicationReminderDto? Reminder);
```

- [ ] **Step 4: Update ApplicationMapper to map reminder**

Modify `backend/Yaam.UseCases/Applications/ApplicationMapper.cs`:

```csharp
using Yaam.Domain.Entities;
using Yaam.UseCases.Applications.Dtos;
using Yaam.UseCases.Reminders;

namespace Yaam.UseCases.Applications;

internal static class ApplicationMapper
{
    internal static ApplicationDto ToDto(Application a) => new(
        a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status,
        a.ContactName, a.ContactEmail, a.ContactPhone, a.JobPosting,
        a.CreatedAt, a.UpdatedAt,
        a.Notes.Select(NoteToDto).ToList(),
        a.Reminder is null ? null : ReminderMapper.ToApplicationReminderDto(a.Reminder));

    internal static ApplicationSummaryDto ToSummaryDto(Application a) =>
        new(a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status);

    internal static ApplicationNoteDto NoteToDto(ApplicationNote n) =>
        new(n.Id, n.Body, n.CreatedAt, n.UpdatedAt);
}
```

- [ ] **Step 5: Write failing validator tests**

Create `backend/Yaam.Tests.Unit/Reminders/SetReminderCommandValidatorTests.cs`:

```csharp
using FluentValidation.TestHelper;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.Tests.Unit.Reminders;

public class SetReminderCommandValidatorTests
{
    private readonly SetReminderCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_DelayDays_Zero()
    {
        var result = _validator.TestValidate(new SetReminderCommand(Guid.NewGuid(), 0, null));
        result.ShouldHaveValidationErrorFor(x => x.DelayDays);
    }

    [Fact]
    public void Should_Fail_When_DelayDays_Negative()
    {
        var result = _validator.TestValidate(new SetReminderCommand(Guid.NewGuid(), -1, null));
        result.ShouldHaveValidationErrorFor(x => x.DelayDays);
    }

    [Fact]
    public void Should_Pass_With_Valid_Delay()
    {
        var result = _validator.TestValidate(new SetReminderCommand(Guid.NewGuid(), 7, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Note_Exceeds_500_Chars()
    {
        var result = _validator.TestValidate(new SetReminderCommand(Guid.NewGuid(), 7, new string('x', 501)));
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }
}
```

Create `backend/Yaam.Tests.Unit/Reminders/RescheduleReminderCommandValidatorTests.cs`:

```csharp
using FluentValidation.TestHelper;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.Tests.Unit.Reminders;

public class RescheduleReminderCommandValidatorTests
{
    private readonly RescheduleReminderCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_NewDueDate_Is_Today()
    {
        var result = _validator.TestValidate(
            new RescheduleReminderCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow)));
        result.ShouldHaveValidationErrorFor(x => x.NewDueDate);
    }

    [Fact]
    public void Should_Fail_When_NewDueDate_Is_In_The_Past()
    {
        var result = _validator.TestValidate(
            new RescheduleReminderCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
        result.ShouldHaveValidationErrorFor(x => x.NewDueDate);
    }

    [Fact]
    public void Should_Pass_When_NewDueDate_Is_Tomorrow()
    {
        var result = _validator.TestValidate(
            new RescheduleReminderCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

- [ ] **Step 6: Run tests to confirm they fail**

```bash
cd /Users/uchendu/yaam/backend
dotnet test Yaam.Tests.Unit --filter "Reminders"
```

Expected: FAIL — command types do not exist yet.

- [ ] **Step 7: Create GetActiveRemindersQuery**

Create `backend/Yaam.UseCases/Reminders/Queries/GetActiveRemindersQuery.cs`:

```csharp
using MediatR;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Reminders.Queries;

public record GetActiveRemindersQuery : IRequest<List<ReminderSummaryDto>>;

public class GetActiveRemindersQueryHandler(IReminderRepository repository)
    : IRequestHandler<GetActiveRemindersQuery, List<ReminderSummaryDto>>
{
    public async Task<List<ReminderSummaryDto>> Handle(
        GetActiveRemindersQuery query, CancellationToken cancellationToken)
    {
        var reminders = await repository.GetAllActiveAsync(cancellationToken);
        return reminders.Select(ReminderMapper.ToSummaryDto).ToList();
    }
}
```

- [ ] **Step 8: Create SetReminderCommand**

Create `backend/Yaam.UseCases/Reminders/Commands/SetReminderCommand.cs`:

```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Reminders.Commands;

public record SetReminderCommand(
    Guid ApplicationId,
    int DelayDays,
    string? Note) : IRequest<ApplicationReminderDto>;

public class SetReminderCommandValidator : AbstractValidator<SetReminderCommand>
{
    public SetReminderCommandValidator()
    {
        RuleFor(x => x.DelayDays).GreaterThan(0).WithMessage("Delay must be at least 1 day.");
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}

public class SetReminderCommandHandler(IReminderRepository reminderRepository, IApplicationRepository applicationRepository)
    : IRequestHandler<SetReminderCommand, ApplicationReminderDto>
{
    public async Task<ApplicationReminderDto> Handle(
        SetReminderCommand command, CancellationToken cancellationToken)
    {
        var application = await applicationRepository.GetByIdAsync(command.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Application), command.ApplicationId);

        var existing = await reminderRepository.GetByApplicationIdAsync(command.ApplicationId, cancellationToken);
        if (existing is not null)
            throw new ConflictException("An active reminder already exists. Complete or delete it before adding a new one.");

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(command.DelayDays);
        var reminder = new Reminder
        {
            ApplicationId = command.ApplicationId,
            DelayDays = command.DelayDays,
            DueDate = dueDate,
            Note = command.Note,
        };

        await reminderRepository.AddAsync(reminder, cancellationToken);
        return ReminderMapper.ToApplicationReminderDto(reminder);
    }
}
```

- [ ] **Step 9: Create CompleteReminderCommand**

Create `backend/Yaam.UseCases/Reminders/Commands/CompleteReminderCommand.cs`:

```csharp
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Reminders.Commands;

public record CompleteReminderCommand(Guid ApplicationId) : IRequest;

public class CompleteReminderCommandHandler(IReminderRepository repository)
    : IRequestHandler<CompleteReminderCommand>
{
    public async Task Handle(CompleteReminderCommand command, CancellationToken cancellationToken)
    {
        var reminder = await repository.GetByApplicationIdAsync(command.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), command.ApplicationId);

        reminder.CompletedAt = DateTime.UtcNow;
        await repository.UpdateAsync(reminder, cancellationToken);
    }
}
```

- [ ] **Step 10: Create RescheduleReminderCommand**

Create `backend/Yaam.UseCases/Reminders/Commands/RescheduleReminderCommand.cs`:

```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Reminders.Commands;

public record RescheduleReminderCommand(
    Guid ApplicationId,
    DateOnly NewDueDate) : IRequest<ApplicationReminderDto>;

public class RescheduleReminderCommandValidator : AbstractValidator<RescheduleReminderCommand>
{
    public RescheduleReminderCommandValidator()
    {
        RuleFor(x => x.NewDueDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("New due date must be in the future.");
    }
}

public class RescheduleReminderCommandHandler(IReminderRepository repository)
    : IRequestHandler<RescheduleReminderCommand, ApplicationReminderDto>
{
    public async Task<ApplicationReminderDto> Handle(
        RescheduleReminderCommand command, CancellationToken cancellationToken)
    {
        var reminder = await repository.GetByApplicationIdAsync(command.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), command.ApplicationId);

        reminder.DueDate = command.NewDueDate;
        reminder.NotifiedAt = null;
        await repository.UpdateAsync(reminder, cancellationToken);
        return ReminderMapper.ToApplicationReminderDto(reminder);
    }
}
```

- [ ] **Step 11: Create DeleteReminderCommand**

Create `backend/Yaam.UseCases/Reminders/Commands/DeleteReminderCommand.cs`:

```csharp
using MediatR;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Reminders.Commands;

public record DeleteReminderCommand(Guid ApplicationId) : IRequest;

public class DeleteReminderCommandHandler(IReminderRepository repository)
    : IRequestHandler<DeleteReminderCommand>
{
    public async Task Handle(DeleteReminderCommand command, CancellationToken cancellationToken)
        => await repository.DeleteByApplicationIdAsync(command.ApplicationId, cancellationToken);
}
```

- [ ] **Step 12: Run validator tests**

```bash
cd /Users/uchendu/yaam/backend
dotnet test Yaam.Tests.Unit --filter "Reminders"
```

Expected: PASS — 7 tests passing.

- [ ] **Step 13: Run full unit test suite**

```bash
dotnet test Yaam.Tests.Unit
```

Expected: all tests pass.

- [ ] **Step 14: Commit**

```bash
git add backend/Yaam.UseCases/ backend/Yaam.Tests.Unit/
git commit -m "feat: add reminder DTOs, queries, CRUD command handlers, and validators"
```

---

## Task 5: SendDueRemindersCommand

**Files:**
- Create: `backend/Yaam.UseCases/Reminders/Commands/SendDueRemindersCommand.cs`
- Create: `backend/Yaam.Tests.Unit/Reminders/SendDueRemindersCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IReminderRepository`, `IEmailSender`, `EmailSettings` from Tasks 1–3
- Produces: `SendDueRemindersCommand` handler dispatched by the background service in Task 6

- [ ] **Step 1: Write failing handler tests**

Create `backend/Yaam.Tests.Unit/Reminders/SendDueRemindersCommandHandlerTests.cs`:

```csharp
using Microsoft.Extensions.Options;
using NSubstitute;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Email;
using Yaam.UseCases.Common.Email;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.Tests.Unit.Reminders;

public class SendDueRemindersCommandHandlerTests
{
    private readonly IReminderRepository _repo = Substitute.For<IReminderRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly EmailSettings _settings = new()
    {
        NotificationEmail = "user@example.com",
        FromAddress = "noreply@yaam.local",
        FromName = "YAAM"
    };

    private SendDueRemindersCommandHandler CreateHandler() =>
        new(_repo, _emailSender, Options.Create(_settings));

    [Fact]
    public async Task Handle_SendsOneEmailPerDueReminder()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminder = new Reminder
        {
            ApplicationId = Guid.NewGuid(),
            DelayDays = 7,
            DueDate = today,
            Application = new Application { CompanyName = "Acme Corp", Role = "Dev" },
        };
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([reminder]);

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            _settings.NotificationEmail,
            Arg.Is<string>(s => s.Contains("Acme Corp")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsNotifiedAt_AfterSending()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminder = new Reminder
        {
            ApplicationId = Guid.NewGuid(),
            DelayDays = 7,
            DueDate = today,
            Application = new Application { CompanyName = "Acme Corp", Role = "Dev" },
        };
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([reminder]);

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        await _repo.Received(1).UpdateAsync(
            Arg.Is<Reminder>(r => r.NotifiedAt != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNothing_WhenNoDueReminders()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([]);

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailBody_ContainsDraftWithContactName()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminder = new Reminder
        {
            ApplicationId = Guid.NewGuid(),
            DelayDays = 14,
            DueDate = today,
            Application = new Application
            {
                CompanyName = "Beta Ltd",
                Role = "QA Engineer",
                ContactName = "Alice Smith",
                DateApplied = today.AddDays(-14),
            },
        };
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([reminder]);

        string? capturedBody = null;
        await _emailSender.SendAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Do<string>(b => capturedBody = b),
            Arg.Any<CancellationToken>());

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        Assert.NotNull(capturedBody);
        Assert.Contains("Dear Alice Smith,", capturedBody);
        Assert.Contains("Beta Ltd", capturedBody);
        Assert.Contains("QA Engineer", capturedBody);
    }

    [Fact]
    public async Task Handle_EmailBody_FallsBackToGenericSalutation_WhenNoContactName()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminder = new Reminder
        {
            ApplicationId = Guid.NewGuid(),
            DelayDays = 7,
            DueDate = today,
            Application = new Application { CompanyName = "Gamma Inc", Role = "PM" },
        };
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([reminder]);

        string? capturedBody = null;
        await _emailSender.SendAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Do<string>(b => capturedBody = b),
            Arg.Any<CancellationToken>());

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        Assert.NotNull(capturedBody);
        Assert.Contains("Dear Hiring Team,", capturedBody);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd /Users/uchendu/yaam/backend
dotnet test Yaam.Tests.Unit --filter "SendDueRemindersCommandHandlerTests"
```

Expected: FAIL — `SendDueRemindersCommandHandler` does not exist yet.

- [ ] **Step 3: Add NSubstitute to unit tests project**

The unit tests project already has NSubstitute, but the handler needs `IOptions<EmailSettings>` from Infrastructure. Add a project reference:

```bash
dotnet add Yaam.Tests.Unit/Yaam.Tests.Unit.csproj reference Yaam.Infrastructure/Yaam.Infrastructure.csproj
```

- [ ] **Step 4: Create SendDueRemindersCommand**

Create `backend/Yaam.UseCases/Reminders/Commands/SendDueRemindersCommand.cs`:

```csharp
using System.Text;
using MediatR;
using Microsoft.Extensions.Options;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Email;
using Yaam.UseCases.Common.Email;

namespace Yaam.UseCases.Reminders.Commands;

public record SendDueRemindersCommand : IRequest;

public class SendDueRemindersCommandHandler(
    IReminderRepository repository,
    IEmailSender emailSender,
    IOptions<EmailSettings> emailOptions)
    : IRequestHandler<SendDueRemindersCommand>
{
    public async Task Handle(SendDueRemindersCommand command, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueReminders = await repository.GetDueAsync(today, cancellationToken);

        foreach (var reminder in dueReminders)
        {
            var subject = $"Follow-up reminder: {reminder.Application.CompanyName} — {reminder.Application.Role}";
            var body = BuildEmailBody(reminder);

            await emailSender.SendAsync(
                emailOptions.Value.NotificationEmail,
                subject,
                body,
                cancellationToken);

            reminder.NotifiedAt = DateTime.UtcNow;
            await repository.UpdateAsync(reminder, cancellationToken);
        }
    }

    private static string BuildEmailBody(Reminder reminder)
    {
        var app = reminder.Application;
        var salutation = string.IsNullOrEmpty(app.ContactName)
            ? "Dear Hiring Team,"
            : $"Dear {app.ContactName},";
        var dateApplied = app.DateApplied.HasValue
            ? app.DateApplied.Value.ToString("d MMMM yyyy")
            : null;
        var appliedLine = dateApplied is not null ? $", which I submitted on {dateApplied}" : "";

        var sb = new StringBuilder();
        sb.AppendLine($"Your follow-up reminder for {app.CompanyName} ({app.Role}) is due today.");
        if (dateApplied is not null)
            sb.AppendLine($"Applied: {dateApplied}");
        if (!string.IsNullOrEmpty(reminder.Note))
            sb.AppendLine($"Your note: {reminder.Note}");
        sb.AppendLine();
        sb.AppendLine("--- Follow-up email draft ---");
        sb.AppendLine();
        sb.AppendLine(salutation);
        sb.AppendLine();
        sb.AppendLine($"I am writing to follow up on my application for the {app.Role} position at {app.CompanyName}{appliedLine}.");
        sb.AppendLine();
        sb.AppendLine("I remain very interested in this opportunity and would welcome the chance to discuss my application further. Please let me know if you need any additional information.");
        sb.AppendLine();
        sb.AppendLine("Kind regards");

        return sb.ToString();
    }
}
```

Note: `SendDueRemindersCommand` lives in `Yaam.UseCases` but depends on `EmailSettings` from `Yaam.Infrastructure`. Add a project reference:

```bash
dotnet add Yaam.UseCases/Yaam.UseCases.csproj reference Yaam.Infrastructure/Yaam.Infrastructure.csproj
```

- [ ] **Step 5: Run handler tests**

```bash
cd /Users/uchendu/yaam/backend
dotnet test Yaam.Tests.Unit --filter "SendDueRemindersCommandHandlerTests"
```

Expected: PASS — 5 tests passing.

- [ ] **Step 6: Run full unit test suite**

```bash
dotnet test Yaam.Tests.Unit
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
git add backend/Yaam.UseCases/ backend/Yaam.Tests.Unit/
git commit -m "feat: add SendDueRemindersCommand with email template and handler tests"
```

---

## Task 6: API layer — ViewModels, controllers, background service, integration tests

**Files:**
- Create: `backend/Yaam.API/Reminders/ReminderViewModel.cs`
- Create: `backend/Yaam.API/Reminders/ReminderViewModelMapper.cs`
- Create: `backend/Yaam.API/Reminders/ReminderNotificationService.cs`
- Create: `backend/Yaam.API/Controllers/ApplicationReminderController.cs`
- Create: `backend/Yaam.API/Controllers/RemindersController.cs`
- Create: `backend/Yaam.Tests.Integration/Reminders/RemindersEndpointsTests.cs`
- Modify: `backend/Yaam.API/Applications/ApplicationViewModel.cs`
- Modify: `backend/Yaam.API/Applications/ApplicationViewModelMapper.cs`
- Modify: `backend/Yaam.API/GlobalExceptionHandler.cs`
- Modify: `backend/Yaam.API/Program.cs`
- Modify: `backend/Yaam.Tests.Integration/ApiFactory.cs`

**Interfaces:**
- Consumes: all UseCases from Tasks 4–5
- Produces: REST endpoints, background service, 9 integration tests

- [ ] **Step 1: Create ReminderViewModel records**

Create `backend/Yaam.API/Reminders/ReminderViewModel.cs`:

```csharp
namespace Yaam.API.Reminders;

public record ApplicationReminderViewModel(
    Guid Id,
    int DelayDays,
    DateOnly DueDate,
    string? Note,
    DateTime? NotifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);

public record ReminderViewModel(
    Guid Id,
    Guid ApplicationId,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    string? ContactName,
    int DelayDays,
    DateOnly DueDate,
    string? Note,
    DateTime? NotifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
```

- [ ] **Step 2: Create ReminderViewModelMapper**

Create `backend/Yaam.API/Reminders/ReminderViewModelMapper.cs`:

```csharp
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.API.Reminders;

internal static class ReminderViewModelMapper
{
    internal static ApplicationReminderViewModel ToViewModel(ApplicationReminderDto dto) => new(
        dto.Id, dto.DelayDays, dto.DueDate, dto.Note, dto.NotifiedAt, dto.CompletedAt, dto.CreatedAt);

    internal static ReminderViewModel ToViewModel(ReminderSummaryDto dto) => new(
        dto.Id, dto.ApplicationId, dto.CompanyName, dto.Role, dto.DateApplied, dto.ContactName,
        dto.DelayDays, dto.DueDate, dto.Note, dto.NotifiedAt, dto.CompletedAt, dto.CreatedAt);
}
```

- [ ] **Step 3: Update ApplicationViewModel to embed reminder**

Modify `backend/Yaam.API/Applications/ApplicationViewModel.cs`:

```csharp
using Yaam.API.Reminders;
using Yaam.Domain.Enums;

namespace Yaam.API.Applications;

public record ApplicationViewModel(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ApplicationNoteViewModel> Notes,
    ApplicationReminderViewModel? Reminder);

public record ApplicationSummaryViewModel(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status);

public record ApplicationNoteViewModel(
    Guid Id,
    string Body,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

- [ ] **Step 4: Update ApplicationViewModelMapper to map reminder**

Modify `backend/Yaam.API/Applications/ApplicationViewModelMapper.cs`:

```csharp
using Yaam.API.Reminders;
using Yaam.UseCases.Applications.Dtos;

namespace Yaam.API.Applications;

internal static class ApplicationViewModelMapper
{
    internal static ApplicationViewModel ToViewModel(ApplicationDto dto) => new(
        dto.Id,
        dto.CompanyName,
        dto.Role,
        dto.DateApplied,
        dto.Status,
        dto.ContactName,
        dto.ContactEmail,
        dto.ContactPhone,
        dto.JobPosting,
        dto.CreatedAt,
        dto.UpdatedAt,
        dto.Notes.Select(ToViewModel).ToList(),
        dto.Reminder is null ? null : ReminderViewModelMapper.ToViewModel(dto.Reminder));

    internal static ApplicationSummaryViewModel ToViewModel(ApplicationSummaryDto dto) =>
        new(dto.Id, dto.CompanyName, dto.Role, dto.DateApplied, dto.Status);

    internal static ApplicationNoteViewModel ToViewModel(ApplicationNoteDto dto) =>
        new(dto.Id, dto.Body, dto.CreatedAt, dto.UpdatedAt);
}
```

- [ ] **Step 5: Add ConflictException handler to GlobalExceptionHandler**

Modify `backend/Yaam.API/GlobalExceptionHandler.cs`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Yaam.Domain.Errors;

namespace Yaam.API;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found.", (Dictionary<string, string[]>?)null),
            ConflictException ce => (StatusCodes.Status409Conflict, ce.Message, (Dictionary<string, string[]>?)null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", (Dictionary<string, string[]>?)null)
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = "https://tools.ietf.org/html/rfc9457"
        };

        if (errors is not null)
            problemDetails.Extensions["errors"] = errors;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
```

- [ ] **Step 6: Create ApplicationReminderController**

Create `backend/Yaam.API/Controllers/ApplicationReminderController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.API.Reminders;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/applications/{applicationId:guid}/reminder")]
public class ApplicationReminderController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [EndpointName("SetReminder")]
    public async Task<ActionResult<ApplicationReminderViewModel>> Set(
        Guid applicationId, [FromBody] SetReminderInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SetReminderCommand(applicationId, input.DelayDays, input.Note),
            cancellationToken);
        return CreatedAtAction(nameof(Set), new { applicationId }, ReminderViewModelMapper.ToViewModel(result));
    }

    [HttpPatch("complete")]
    [EndpointName("CompleteReminder")]
    public async Task<IActionResult> Complete(Guid applicationId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new CompleteReminderCommand(applicationId),
            cancellationToken);
        return NoContent();
    }

    [HttpPatch("reschedule")]
    [EndpointName("RescheduleReminder")]
    public async Task<ActionResult<ApplicationReminderViewModel>> Reschedule(
        Guid applicationId, [FromBody] RescheduleReminderInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RescheduleReminderCommand(applicationId, input.NewDueDate),
            cancellationToken);
        return Ok(ReminderViewModelMapper.ToViewModel(result));
    }

    [HttpDelete]
    [EndpointName("DeleteReminder")]
    public async Task<IActionResult> Delete(Guid applicationId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteReminderCommand(applicationId),
            cancellationToken);
        return NoContent();
    }
}

public record SetReminderInputModel(int DelayDays, string? Note);
public record RescheduleReminderInputModel(DateOnly NewDueDate);
```

- [ ] **Step 7: Create RemindersController**

Create `backend/Yaam.API/Controllers/RemindersController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.API.Reminders;
using Yaam.UseCases.Reminders.Queries;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/reminders")]
public class RemindersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListReminders")]
    public async Task<ActionResult<List<ReminderViewModel>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetActiveRemindersQuery(),
            cancellationToken);
        return Ok(result.Select(ReminderViewModelMapper.ToViewModel).ToList());
    }
}
```

- [ ] **Step 8: Create ReminderNotificationService**

Create `backend/Yaam.API/Reminders/ReminderNotificationService.cs`:

```csharp
using MediatR;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.API.Reminders;

public class ReminderNotificationService(IServiceScopeFactory scopeFactory, ILogger<ReminderNotificationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun();
            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            logger.LogInformation("Running reminder notification job at {Time}", DateTime.UtcNow);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(
                    new SendDueRemindersCommand(),
                    stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Reminder notification job failed");
            }
        }
    }

    private static TimeSpan TimeUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(8);
        if (now >= nextRun) nextRun = nextRun.AddDays(1);
        return nextRun - now;
    }
}
```

- [ ] **Step 9: Register background service in Program.cs**

Modify `backend/Yaam.API/Program.cs` — add one line after `AddExceptionHandler`:

```csharp
using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Yaam.API;
using Yaam.API.Reminders;
using Yaam.Infrastructure;
using Yaam.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required."),
    builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHostedService<ReminderNotificationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("Angular");
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
```

- [ ] **Step 10: Add NoOpEmailSender to integration test project**

Modify `backend/Yaam.Tests.Integration/ApiFactory.cs` — add a `NoOpEmailSender` class and replace the real sender in the test host:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yaam.Infrastructure.Persistence;
using Yaam.UseCases.Common.Email;

namespace Yaam.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json"),
                optional: true);
            config.AddEnvironmentVariables();
        });

        builder.ConfigureServices((context, services) =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    context.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException(
                        "Test connection string 'DefaultConnection' not found in appsettings.Test.json.")));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}

public class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

- [ ] **Step 11: Create integration tests**

Create `backend/Yaam.Tests.Integration/Reminders/RemindersEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.API.Applications;
using Yaam.API.Reminders;

namespace Yaam.Tests.Integration.Reminders;

[Collection("Integration")]
public class RemindersEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_Reminders_ReturnsEmptyList_WhenNoRemindersExist()
    {
        var response = await _client.GetAsync("/api/reminders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ReminderViewModel>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task ReminderCrudFlow_SetCompleteRescheduleDelete()
    {
        var app = await CreateApplicationAsync();

        // Set reminder
        var setPayload = new { delayDays = 7, note = "Check in on status" };
        var setResponse = await _client.PostAsJsonAsync($"/api/applications/{app.Id}/reminder", setPayload);
        setResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reminder = await setResponse.Content.ReadFromJsonAsync<ApplicationReminderViewModel>();
        reminder!.DelayDays.Should().Be(7);
        reminder.Note.Should().Be("Check in on status");
        reminder.CompletedAt.Should().BeNull();
        reminder.NotifiedAt.Should().BeNull();

        // Application GET now includes reminder
        var appResponse = await _client.GetAsync($"/api/applications/{app.Id}");
        var appWithReminder = await appResponse.Content.ReadFromJsonAsync<ApplicationViewModel>();
        appWithReminder!.Reminder.Should().NotBeNull();
        appWithReminder.Reminder!.DelayDays.Should().Be(7);

        // Second set is rejected
        var conflictResponse = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/reminder", setPayload);
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Reschedule
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd");
        var rescheduleResponse = await _client.PatchAsJsonAsync(
            $"/api/applications/{app.Id}/reminder/reschedule", new { newDueDate = tomorrow });
        rescheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rescheduled = await rescheduleResponse.Content.ReadFromJsonAsync<ApplicationReminderViewModel>();
        rescheduled!.NotifiedAt.Should().BeNull();

        // Complete
        var completeResponse = await _client.PatchAsJsonAsync(
            $"/api/applications/{app.Id}/reminder/complete", new { });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // After complete, app GET shows no active reminder
        var appAfterComplete = await _client.GetAsync($"/api/applications/{app.Id}");
        var appBody = await appAfterComplete.Content.ReadFromJsonAsync<ApplicationViewModel>();
        appBody!.Reminder.Should().BeNull();

        // Set a new reminder after completing old one
        var setAgainResponse = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/reminder", new { delayDays = 14 });
        setAgainResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/applications/{app.Id}/reminder");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Reminders list is empty
        var listResponse = await _client.GetAsync("/api/reminders");
        var list = await listResponse.Content.ReadFromJsonAsync<List<ReminderViewModel>>();
        list.Should().NotContain(r => r.ApplicationId == app.Id);
    }

    [Fact]
    public async Task POST_Reminder_Returns400_WhenDelayDaysIsZero()
    {
        var app = await CreateApplicationAsync();
        var response = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/reminder", new { delayDays = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_Reschedule_Returns400_WhenNewDueDateIsToday()
    {
        var app = await CreateApplicationAsync();
        await _client.PostAsJsonAsync($"/api/applications/{app.Id}/reminder", new { delayDays = 7 });

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PatchAsJsonAsync(
            $"/api/applications/{app.Id}/reminder/reschedule", new { newDueDate = today });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<ApplicationViewModel> CreateApplicationAsync()
    {
        var payload = new { companyName = "Test Co", role = "Dev", status = "Draft" };
        var response = await _client.PostAsJsonAsync("/api/applications", payload);
        return (await response.Content.ReadFromJsonAsync<ApplicationViewModel>())!;
    }
}
```

- [ ] **Step 12: Build full solution**

```bash
cd /Users/uchendu/yaam/backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 13: Run integration tests**

Ensure PostgreSQL is running (`docker compose up -d postgres`), then:

```bash
dotnet test Yaam.Tests.Integration
```

Expected: all tests pass (including the new reminder tests).

- [ ] **Step 14: Run full test suite**

```bash
dotnet test
```

Expected: all unit + integration tests pass.

- [ ] **Step 15: Commit**

```bash
git add backend/Yaam.API/ backend/Yaam.Tests.Integration/
git commit -m "feat: add reminder controllers, ViewModels, background notification service, and integration tests"
```

---

## Task 7: Regenerate API client

**Files:**
- Modify: `frontend/src/app/generated/api/` (generated, not committed)

**Interfaces:**
- Produces: TypeScript types `ApplicationReminderViewModel`, `ReminderViewModel`, updated `ApplicationViewModel` with `reminder?` field — consumed by Tasks 8–9

- [ ] **Step 1: Start the backend**

```bash
cd /Users/uchendu/yaam/backend
dotnet run --project Yaam.API
```

Wait until the API is running (look for "Now listening on: https://localhost:5001").

- [ ] **Step 2: Regenerate the API client**

In a second terminal:

```bash
cd /Users/uchendu/yaam/frontend
npm run generate:api
```

Expected: `frontend/src/app/generated/api/` regenerated. Verify the following types exist:
- `ApplicationReminderViewModel` in `model/applicationReminderViewModel.ts`
- `ReminderViewModel` in `model/reminderViewModel.ts`
- `ApplicationViewModel` has a `reminder?: ApplicationReminderViewModel` field

- [ ] **Step 3: Build the frontend**

```bash
npx ng build
```

Expected: build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/generated/
```

The generated directory is gitignored — no commit needed. Confirm `.gitignore` excludes it:

```bash
git status frontend/src/app/generated/
```

Expected: no output (directory is ignored).

---

## Task 8: Frontend — ReminderSection on application detail

**Files:**
- Create: `frontend/src/app/features/applications/components/reminder-section/reminder-section.component.ts`
- Create: `frontend/src/app/features/applications/components/reminder-section/reminder-section.component.html`
- Modify: `frontend/src/app/features/applications/pages/application-detail/application-detail.component.html`

**Interfaces:**
- Consumes: `ApplicationReminderViewModel` from generated API, `ApplicationReminderService` (generated)
- Produces: reminder section visible on the application detail page

- [ ] **Step 1: Create ReminderSectionComponent**

Create `frontend/src/app/features/applications/components/reminder-section/reminder-section.component.ts`:

```typescript
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, required } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationReminderService,
  ApplicationReminderViewModel,
} from '../../../../generated/api/index';

@Component({
  selector: 'app-reminder-section',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './reminder-section.component.html',
})
export class ReminderSectionComponent {
  readonly applicationId = input.required<string>();
  readonly reminder = input<ApplicationReminderViewModel | null | undefined>();

  readonly reminderChanged = output<void>();

  private readonly api = inject(ApplicationReminderService);

  protected readonly showAddForm = signal(false);
  protected readonly showRescheduleForm = signal(false);
  protected readonly deleteConfirm = signal(false);
  protected readonly saving = signal(false);

  protected readonly addModel = signal({ delayDays: 7, customDays: '', note: '' });
  protected readonly addFields = form(this.addModel, () => {});

  protected readonly rescheduleModel = signal({ newDueDate: '' });
  protected readonly rescheduleFields = form(this.rescheduleModel, (fields) => {
    required(fields.newDueDate, { message: 'New due date is required.' });
  });

  protected readonly presetDays = [7, 14, 30] as const;

  protected readonly today = computed(() => new Date().toISOString().split('T')[0]);

  protected readonly dueDateDisplay = computed(() => {
    const r = this.reminder();
    if (!r) return null;
    const due = new Date(r.dueDate as unknown as string);
    const todayDate = new Date();
    todayDate.setHours(0, 0, 0, 0);
    due.setHours(0, 0, 0, 0);
    const diff = Math.round((due.getTime() - todayDate.getTime()) / 86400000);
    if (diff < 0) return { label: `Overdue by ${-diff} day${-diff === 1 ? '' : 's'}`, cls: 'badge-error' };
    if (diff === 0) return { label: 'Due today', cls: 'badge-warning' };
    return { label: `Due in ${diff} day${diff === 1 ? '' : 's'}`, cls: 'badge-info' };
  });

  protected selectPreset(days: number): void {
    this.addModel.update((m) => ({ ...m, delayDays: days, customDays: '' }));
  }

  protected onCustomDaysInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    const parsed = parseInt(value, 10);
    if (!isNaN(parsed) && parsed > 0) {
      this.addModel.update((m) => ({ ...m, delayDays: parsed, customDays: value }));
    } else {
      this.addModel.update((m) => ({ ...m, customDays: value }));
    }
  }

  protected previewDate(): string {
    const days = this.addModel().delayDays;
    if (!days || days < 1) return '';
    const d = new Date();
    d.setDate(d.getDate() + days);
    return d.toLocaleDateString('en-CH', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  protected async setReminder(): Promise<void> {
    const { delayDays, note } = this.addModel();
    if (!delayDays || delayDays < 1) return;
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.setReminder(this.applicationId(), { delayDays, note: note || null }),
      );
      this.showAddForm.set(false);
      this.addModel.set({ delayDays: 7, customDays: '', note: '' });
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected async completeReminder(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.completeReminder(this.applicationId()));
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected async rescheduleReminder(): Promise<void> {
    const { newDueDate } = this.rescheduleModel();
    if (!newDueDate) return;
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.rescheduleReminder(this.applicationId(), { newDueDate: newDueDate as unknown as Date }),
      );
      this.showRescheduleForm.set(false);
      this.rescheduleModel.set({ newDueDate: '' });
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected async deleteReminder(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.deleteReminder(this.applicationId()));
      this.deleteConfirm.set(false);
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }
}
```

- [ ] **Step 2: Create ReminderSectionComponent template**

Create `frontend/src/app/features/applications/components/reminder-section/reminder-section.component.html`:

```html
<div>
  <h2 class="text-lg font-semibold mb-3">Reminder</h2>

  @if (reminder()) {
    @let r = reminder()!;
    <div class="card bg-base-200 p-4 space-y-3">
      <div class="flex items-center justify-between flex-wrap gap-2">
        <div class="flex items-center gap-2">
          @if (dueDateDisplay(); as display) {
            <span class="badge {{ display.cls }}">{{ display.label }}</span>
          }
          <span class="text-sm text-base-content/70">{{ r.dueDate }}</span>
        </div>
        @if (r.notifiedAt) {
          <span class="text-xs text-base-content/50">
            Follow-up sent {{ r.notifiedAt | date: 'mediumDate' }}
          </span>
        }
      </div>

      @if (r.note) {
        <p class="text-sm">{{ r.note }}</p>
      }

      @if (!showRescheduleForm()) {
        <div class="flex gap-2 flex-wrap">
          <button class="btn btn-sm btn-outline" (click)="showRescheduleForm.set(true)">
            Reschedule
          </button>
          <button
            class="btn btn-sm btn-success btn-outline"
            [disabled]="saving()"
            (click)="completeReminder()"
          >
            Mark as done
          </button>
          @if (deleteConfirm()) {
            <button class="btn btn-sm btn-error" [disabled]="saving()" (click)="deleteReminder()">
              Confirm delete
            </button>
            <button class="btn btn-sm btn-ghost" (click)="deleteConfirm.set(false)">Cancel</button>
          } @else {
            <button class="btn btn-sm btn-ghost text-error" (click)="deleteConfirm.set(true)">
              Delete
            </button>
          }
        </div>
      } @else {
        <div class="flex items-end gap-2 flex-wrap">
          <div class="form-control">
            <label class="label label-text text-xs">New due date</label>
            <input
              type="date"
              class="input input-bordered input-sm"
              [min]="today()"
              [value]="rescheduleModel().newDueDate"
              (input)="rescheduleModel.update(m => ({ ...m, newDueDate: $any($event.target).value }))"
            />
          </div>
          <button
            class="btn btn-sm btn-primary"
            [disabled]="saving() || !rescheduleModel().newDueDate"
            (click)="rescheduleReminder()"
          >
            Save
          </button>
          <button class="btn btn-sm btn-ghost" (click)="showRescheduleForm.set(false)">
            Cancel
          </button>
        </div>
      }
    </div>
  } @else if (!showAddForm()) {
    <div class="flex items-center gap-3">
      <p class="text-sm text-base-content/50">No reminder set.</p>
      <button class="btn btn-sm btn-outline" (click)="showAddForm.set(true)">Add reminder</button>
    </div>
  } @else {
    <div class="card bg-base-200 p-4 space-y-3">
      <div class="flex gap-2 flex-wrap">
        @for (days of presetDays; track days) {
          <button
            class="btn btn-sm"
            [class.btn-primary]="addModel().delayDays === days && !addModel().customDays"
            [class.btn-outline]="addModel().delayDays !== days || !!addModel().customDays"
            (click)="selectPreset(days)"
          >
            {{ days }} days
          </button>
        }
        <input
          type="number"
          class="input input-bordered input-sm w-24"
          placeholder="Custom"
          min="1"
          [value]="addModel().customDays"
          (input)="onCustomDaysInput($event)"
        />
      </div>

      @if (previewDate()) {
        <p class="text-xs text-base-content/60">Reminder due: {{ previewDate() }}</p>
      }

      <textarea
        class="textarea textarea-bordered w-full text-sm"
        rows="2"
        placeholder="Optional note (e.g. 'Call recruiter by EOD')"
        [value]="addModel().note"
        (input)="addModel.update(m => ({ ...m, note: $any($event.target).value }))"
      ></textarea>

      <div class="flex gap-2 justify-end">
        <button class="btn btn-sm btn-ghost" (click)="showAddForm.set(false)">Cancel</button>
        <button
          class="btn btn-sm btn-primary"
          [disabled]="saving() || addModel().delayDays < 1"
          (click)="setReminder()"
        >
          Save reminder
        </button>
      </div>
    </div>
  }
</div>
```

- [ ] **Step 3: Add ReminderSection to the application detail template**

Modify `frontend/src/app/features/applications/pages/application-detail/application-detail.component.html` — add `app-reminder-section` after `app-application-notes` inside the card. Replace the `<app-application-notes ... />` line with:

```html
      <app-application-notes [applicationId]="app.id" [initialNotes]="app.notes" />

      <div class="divider"></div>

      <app-reminder-section
        [applicationId]="app.id"
        [reminder]="app.reminder"
        (reminderChanged)="applicationResource.reload()"
      />
```

- [ ] **Step 4: Add ReminderSectionComponent import to ApplicationDetailComponent**

Modify `frontend/src/app/features/applications/pages/application-detail/application-detail.component.ts` — add `ReminderSectionComponent` to imports:

```typescript
import { Component, computed, inject, resource, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { firstValueFrom } from 'rxjs';
import { ApplicationStatus, ApplicationsService } from '../../../../generated/api/index';
import { Application, STATUS_LABELS, ALL_STATUSES } from '../../models/application.model';
import {
  ApplicationFormComponent,
  ApplicationFormData,
} from '../../components/application-form/application-form.component';
import { ApplicationNotesComponent } from '../../components/application-notes/application-notes.component';
import { ReminderSectionComponent } from '../../components/reminder-section/reminder-section.component';

@Component({
  selector: 'app-application-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ApplicationFormComponent, ApplicationNotesComponent, ReminderSectionComponent],
  templateUrl: './application-detail.component.html',
})
export class ApplicationDetailComponent {
  private readonly api = inject(ApplicationsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly STATUS_LABELS = STATUS_LABELS;
  protected readonly ALL_STATUSES = ALL_STATUSES;

  protected readonly editMode = signal(false);
  protected readonly statusSaving = signal(false);
  protected readonly deleteModalOpen = signal(false);
  protected readonly serverErrors = signal<Record<string, string[]>>({});

  protected readonly applicationId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? undefined)),
  );
  protected readonly isNew = computed(() => !this.applicationId());

  protected readonly applicationResource = resource<Application | undefined, string | undefined>({
    params: () => this.applicationId(),
    loader: ({ params }) =>
      params ? firstValueFrom(this.api.getApplication(params)) : Promise.resolve(undefined),
  });

  protected async onSave(formData: ApplicationFormData): Promise<void> {
    const payload = {
      ...formData,
      status: formData.status as ApplicationStatus,
      dateApplied: formData.dateApplied || null,
      contactName: formData.contactName || null,
      contactEmail: formData.contactEmail || null,
      contactPhone: formData.contactPhone || null,
      jobPosting: formData.jobPosting || null,
    };
    try {
      if (this.isNew()) {
        const created = await firstValueFrom(this.api.createApplication(payload));
        await this.router.navigate(['/applications', created.id]);
      } else {
        await firstValueFrom(this.api.updateApplication(this.applicationId()!, payload));
        this.applicationResource.reload();
        this.editMode.set(false);
      }
    } catch (err: unknown) {
      const apiErr = err as { errors?: Record<string, string[]> };
      this.serverErrors.set(apiErr?.errors ?? {});
    }
  }

  protected onCancelEdit(): void {
    if (this.isNew()) {
      this.router.navigate(['/applications']);
    } else {
      this.editMode.set(false);
    }
  }

  protected async onStatusChange(event: Event): Promise<void> {
    const status = (event.target as HTMLSelectElement).value;
    this.statusSaving.set(true);
    try {
      await firstValueFrom(
        this.api.patchApplicationStatus(this.applicationId()!, {
          status: status as ApplicationStatus,
        }),
      );
      this.applicationResource.reload();
    } finally {
      this.statusSaving.set(false);
    }
  }

  protected async onDelete(): Promise<void> {
    await firstValueFrom(this.api.deleteApplication(this.applicationId()!));
    await this.router.navigate(['/applications']);
  }
}
```

- [ ] **Step 5: Build the frontend**

```bash
cd /Users/uchendu/yaam/frontend
npx ng build
```

Expected: build succeeds with no type errors.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/app/features/applications/
git commit -m "feat: add reminder section to application detail"
```

---

## Task 9: Frontend — Reminders list page

**Files:**
- Create: `frontend/src/app/features/reminders/pages/reminders-list/reminders-list.component.ts`
- Create: `frontend/src/app/features/reminders/pages/reminders-list/reminders-list.component.html`
- Modify: `frontend/src/app/features/reminders/reminders.routes.ts`

**Interfaces:**
- Consumes: `RemindersService` (generated), `ApplicationReminderService` (generated), `ReminderViewModel`
- Produces: `/reminders` route with grouped Overdue / Due today / Upcoming / Done list

- [ ] **Step 1: Create RemindersListComponent**

Create `frontend/src/app/features/reminders/pages/reminders-list/reminders-list.component.ts`:

```typescript
import { Component, computed, inject, resource, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationReminderService,
  RemindersService,
  ReminderViewModel,
} from '../../../../generated/api/index';

@Component({
  selector: 'app-reminders-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './reminders-list.component.html',
})
export class RemindersListComponent {
  private readonly remindersApi = inject(RemindersService);
  private readonly applicationReminderApi = inject(ApplicationReminderService);

  protected readonly saving = signal<string | null>(null);

  protected readonly remindersResource = resource<ReminderViewModel[], unknown>({
    loader: () => firstValueFrom(this.remindersApi.listReminders()),
  });

  protected readonly grouped = computed(() => {
    const all = this.remindersResource.value() ?? [];
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const overdue: ReminderViewModel[] = [];
    const dueToday: ReminderViewModel[] = [];
    const upcoming: ReminderViewModel[] = [];

    for (const r of all) {
      const due = new Date(r.dueDate as unknown as string);
      due.setHours(0, 0, 0, 0);
      const diff = Math.round((due.getTime() - today.getTime()) / 86400000);
      if (diff < 0) overdue.push(r);
      else if (diff === 0) dueToday.push(r);
      else upcoming.push(r);
    }

    return { overdue, dueToday, upcoming };
  });

  protected async complete(applicationId: string): Promise<void> {
    this.saving.set(applicationId);
    try {
      await firstValueFrom(this.applicationReminderApi.completeReminder(applicationId));
      this.remindersResource.reload();
    } finally {
      this.saving.set(null);
    }
  }

  protected async deleteReminder(applicationId: string): Promise<void> {
    this.saving.set(applicationId);
    try {
      await firstValueFrom(this.applicationReminderApi.deleteReminder(applicationId));
      this.remindersResource.reload();
    } finally {
      this.saving.set(null);
    }
  }

  protected dueDaysLabel(reminder: ReminderViewModel): string {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const due = new Date(reminder.dueDate as unknown as string);
    due.setHours(0, 0, 0, 0);
    const diff = Math.round((due.getTime() - today.getTime()) / 86400000);
    if (diff < 0) return `${-diff}d overdue`;
    if (diff === 0) return 'Today';
    return `In ${diff}d`;
  }
}
```

- [ ] **Step 2: Create RemindersListComponent template**

Create `frontend/src/app/features/reminders/pages/reminders-list/reminders-list.component.html`:

```html
<div class="container mx-auto p-6 max-w-3xl">
  <h1 class="text-2xl font-bold mb-6">Reminders</h1>

  @if (remindersResource.isLoading()) {
    <div class="flex justify-center py-12">
      <span class="loading loading-spinner loading-lg"></span>
    </div>
  } @else if (remindersResource.status() === 'error') {
    <div class="alert alert-error">Failed to load reminders.</div>
  } @else if (!remindersResource.value()?.length) {
    <div class="flex flex-col items-center py-16 text-base-content/50">
      <p class="text-lg">No active reminders.</p>
      <a routerLink="/applications" class="btn btn-outline mt-4">Go to applications</a>
    </div>
  } @else {
    @let g = grouped();

    @if (g.overdue.length) {
      <section class="mb-6">
        <h2 class="text-sm font-semibold text-error uppercase tracking-wide mb-2">
          Overdue ({{ g.overdue.length }})
        </h2>
        @for (r of g.overdue; track r.id) {
          <ng-container *ngTemplateOutlet="reminderCard; context: { r }" />
        }
      </section>
    }

    @if (g.dueToday.length) {
      <section class="mb-6">
        <h2 class="text-sm font-semibold text-warning uppercase tracking-wide mb-2">
          Due today ({{ g.dueToday.length }})
        </h2>
        @for (r of g.dueToday; track r.id) {
          <ng-container *ngTemplateOutlet="reminderCard; context: { r }" />
        }
      </section>
    }

    @if (g.upcoming.length) {
      <section class="mb-6">
        <h2 class="text-sm font-semibold uppercase tracking-wide mb-2">
          Upcoming ({{ g.upcoming.length }})
        </h2>
        @for (r of g.upcoming; track r.id) {
          <ng-container *ngTemplateOutlet="reminderCard; context: { r }" />
        }
      </section>
    }
  }
</div>

<ng-template #reminderCard let-r="r">
  <div class="card bg-base-100 shadow-sm mb-3 p-4">
    <div class="flex items-start justify-between gap-4">
      <div class="flex-1 min-w-0">
        <a
          class="font-semibold hover:underline"
          [routerLink]="['/applications', r.applicationId]"
        >
          {{ r.companyName }}
        </a>
        <p class="text-sm text-base-content/70">{{ r.role }}</p>
        @if (r.note) {
          <p class="text-xs text-base-content/60 mt-1">{{ r.note }}</p>
        }
        @if (r.notifiedAt) {
          <p class="text-xs text-base-content/50 mt-1">
            Notification sent {{ r.notifiedAt | date: 'mediumDate' }}
          </p>
        }
      </div>
      <div class="flex flex-col items-end gap-2 shrink-0">
        <span class="text-xs font-medium text-base-content/60">{{ dueDaysLabel(r) }}</span>
        <div class="flex gap-1">
          <button
            class="btn btn-xs btn-success btn-outline"
            [disabled]="saving() === r.applicationId"
            (click)="complete(r.applicationId)"
          >
            Done
          </button>
          <button
            class="btn btn-xs btn-ghost text-error"
            [disabled]="saving() === r.applicationId"
            (click)="deleteReminder(r.applicationId)"
          >
            Delete
          </button>
        </div>
      </div>
    </div>
  </div>
</ng-template>
```

- [ ] **Step 3: Update reminders.routes.ts**

Modify `frontend/src/app/features/reminders/reminders.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { RemindersListComponent } from './pages/reminders-list/reminders-list.component';

export const REMINDERS_ROUTES: Routes = [
  { path: '', component: RemindersListComponent },
];
```

- [ ] **Step 4: Build the frontend**

```bash
cd /Users/uchendu/yaam/frontend
npx ng build
```

Expected: build succeeds with no type errors.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/reminders/
git commit -m "feat: add reminders list page with grouping and done/delete actions"
```

---

## Self-Review

**Spec coverage check:**

- Story 1 (Set reminder): Task 4 `SetReminderCommand` + Task 6 `ApplicationReminderController POST` + Task 8 `ReminderSectionComponent` add form with presets + Task 8 due date preview. ✓
- Story 1 (one active per application): `SetReminderCommand` throws `ConflictException` → 409. ✓
- Story 2 (email notification): Task 5 `SendDueRemindersCommand` + Task 6 `ReminderNotificationService`. ✓
- Story 2 (follow-up draft in email): `BuildEmailBody` in Task 5. ✓
- Story 2 (NotifiedAt indicator on application): `ApplicationReminderViewModel.notifiedAt` shown in Task 8 template. ✓
- Story 3 (reminders page with grouping): Task 9. ✓
- Story 3 (mark done): `CompleteReminderCommand` + Task 8 + Task 9 buttons. ✓
- Story 3 (reschedule resets NotifiedAt): `RescheduleReminderCommand` sets `reminder.NotifiedAt = null`. ✓
- Story 3 (delete): `DeleteReminderCommand` + Task 8 + Task 9 buttons. ✓
- Story 3 (empty state on application and reminders page): both templates handle null/empty. ✓

**Type consistency check:**

- `SetReminderCommand(Guid ApplicationId, int DelayDays, string? Note)` — used consistently in Task 4 handler and Task 6 controller. ✓
- `CompleteReminderCommand(Guid ApplicationId)` — consistent across Tasks 4 and 6. ✓
- `RescheduleReminderCommand(Guid ApplicationId, DateOnly NewDueDate)` — consistent. ✓
- `DeleteReminderCommand(Guid ApplicationId)` — consistent. ✓
- `ReminderMapper.ToApplicationReminderDto` called in Task 4 commands and `ApplicationMapper`. ✓
- `ApplicationReminderViewModel?` added to `ApplicationViewModel` in Task 6, mapped in `ApplicationViewModelMapper`. ✓
- Frontend `applicationResource.reload()` triggered from `ReminderSectionComponent` via `reminderChanged` output. ✓

**Placeholder scan:** No TBDs, TODOs, or "similar to Task N" references found. All code blocks are complete.
