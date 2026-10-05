using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Common;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;

namespace Yaam.Infrastructure.Repositories;

public class ApplicationRepository(AppDbContext db) : IApplicationRepository
{
    public async Task<List<Application>> GetAllAsync(
        ApplicationStatus? status, ApplicationSortField sort, string order, CancellationToken cancellationToken)
    {
        var query = db.Applications.AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        query = (sort, order.ToLower()) switch
        {
            (ApplicationSortField.CompanyName, "asc") => query.OrderBy(a => a.CompanyName),
            (ApplicationSortField.CompanyName, _) => query.OrderByDescending(a => a.CompanyName),
            (ApplicationSortField.DateApplied, "asc") => query.OrderBy(a => a.DateApplied),
            (ApplicationSortField.DateApplied, _) => query.OrderByDescending(a => a.DateApplied),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unsupported sort field.")
        };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Application?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.Applications
            .Include(a => a.Notes.OrderByDescending(n => n.CreatedAt))
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Application> AddAsync(Application application, CancellationToken cancellationToken)
    {
        db.Applications.Add(application);
        await db.SaveChangesAsync(cancellationToken);
        return application;
    }

    public async Task UpdateAsync(Application application, CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await db.Applications.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(Application), id);
        db.Applications.Remove(application);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApplicationNote?> GetNoteByIdAsync(Guid applicationId, Guid noteId, CancellationToken cancellationToken)
        => await db.ApplicationNotes
            .FirstOrDefaultAsync(n => n.ApplicationId == applicationId && n.Id == noteId, cancellationToken);

    public async Task<ApplicationNote> AddNoteAsync(ApplicationNote note, CancellationToken cancellationToken)
    {
        db.ApplicationNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken);
        return note;
    }

    public async Task UpdateNoteAsync(ApplicationNote note, CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteNoteAsync(Guid applicationId, Guid noteId, CancellationToken cancellationToken)
    {
        var note = await db.ApplicationNotes
            .FirstOrDefaultAsync(n => n.ApplicationId == applicationId && n.Id == noteId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationNote), noteId);
        db.ApplicationNotes.Remove(note);
        await db.SaveChangesAsync(cancellationToken);
    }
}
