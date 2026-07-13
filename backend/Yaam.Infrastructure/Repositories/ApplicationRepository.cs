using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;
using DomainApplication = Yaam.Domain.Entities.Application;
using DomainApplicationNote = Yaam.Domain.Entities.ApplicationNote;

namespace Yaam.Infrastructure.Repositories;

public class ApplicationRepository(AppDbContext db) : IApplicationRepository
{
    public async Task<List<DomainApplication>> GetAllAsync(
        ApplicationStatus? status, string sort, string order, CancellationToken ct)
    {
        var query = db.Applications.AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        query = (sort.ToLower(), order.ToLower()) switch
        {
            ("companyname", "asc") => query.OrderBy(a => a.CompanyName),
            ("companyname", _) => query.OrderByDescending(a => a.CompanyName),
            ("status", "asc") => query.OrderBy(a => a.Status),
            ("status", _) => query.OrderByDescending(a => a.Status),
            (_, "asc") => query.OrderBy(a => a.DateApplied),
            _ => query.OrderByDescending(a => a.DateApplied),
        };

        return await query.ToListAsync(ct);
    }

    public async Task<DomainApplication?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Applications
            .Include(a => a.Notes.OrderByDescending(n => n.CreatedAt))
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<DomainApplication> AddAsync(DomainApplication application, CancellationToken ct)
    {
        db.Applications.Add(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    public async Task UpdateAsync(DomainApplication application, CancellationToken ct)
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

    public async Task<DomainApplicationNote?> GetNoteByIdAsync(Guid applicationId, Guid noteId, CancellationToken ct)
        => await db.ApplicationNotes
            .FirstOrDefaultAsync(n => n.ApplicationId == applicationId && n.Id == noteId, ct);

    public async Task<DomainApplicationNote> AddNoteAsync(DomainApplicationNote note, CancellationToken ct)
    {
        db.ApplicationNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note;
    }

    public async Task UpdateNoteAsync(DomainApplicationNote note, CancellationToken ct)
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
