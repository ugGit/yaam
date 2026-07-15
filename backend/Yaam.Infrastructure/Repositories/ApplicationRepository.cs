using Microsoft.EntityFrameworkCore;
using Yaam.Application.Applications.Queries;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;
using ApplicationNote = Yaam.Domain.Entities.ApplicationNote;

namespace Yaam.Infrastructure.Repositories;

public class ApplicationRepository(AppDbContext db) : IApplicationRepository
{
    public async Task<List<global::Yaam.Domain.Entities.Application>> GetAllAsync(
        ApplicationStatus? status, string sort, string order, CancellationToken ct)
    {
        var query = db.Applications.AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        query = (sort, order.ToLower()) switch
        {
            (ApplicationSortFields.CompanyName, "asc") => query.OrderBy(a => a.CompanyName),
            (ApplicationSortFields.CompanyName, _) => query.OrderByDescending(a => a.CompanyName),
            (ApplicationSortFields.DateApplied, "asc") => query.OrderBy(a => a.DateApplied),
            (ApplicationSortFields.DateApplied, _) => query.OrderByDescending(a => a.DateApplied),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unsupported sort field.")
        };

        return await query.ToListAsync(ct);
    }

    public async Task<global::Yaam.Domain.Entities.Application?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Applications
            .Include(a => a.Notes.OrderByDescending(n => n.CreatedAt))
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<global::Yaam.Domain.Entities.Application> AddAsync(global::Yaam.Domain.Entities.Application application, CancellationToken ct)
    {
        db.Applications.Add(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    public async Task UpdateAsync(global::Yaam.Domain.Entities.Application application, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var application = await db.Applications.FindAsync([id], ct);
        if (application is not null)
        {
            db.Applications.Remove(application);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<ApplicationNote?> GetNoteByIdAsync(Guid applicationId, Guid noteId, CancellationToken ct)
        => await db.ApplicationNotes
            .FirstOrDefaultAsync(n => n.ApplicationId == applicationId && n.Id == noteId, ct);

    public async Task<ApplicationNote> AddNoteAsync(ApplicationNote note, CancellationToken ct)
    {
        db.ApplicationNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note;
    }

    public async Task UpdateNoteAsync(ApplicationNote note, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteNoteAsync(Guid applicationId, Guid noteId, CancellationToken ct)
    {
        var note = await db.ApplicationNotes
            .FirstOrDefaultAsync(n => n.ApplicationId == applicationId && n.Id == noteId, ct);
        if (note is not null)
        {
            db.ApplicationNotes.Remove(note);
            await db.SaveChangesAsync(ct);
        }
    }
}
