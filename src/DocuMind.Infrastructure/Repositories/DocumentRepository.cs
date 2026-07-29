using DocuMind.Domain.Entities;
using DocuMind.Domain.Interfaces;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly IDbContextFactory<DocuMindDbContext> _contextFactory;

    public DocumentRepository(IDbContextFactory<DocuMindDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<Document> SaveAsync(Document document, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);
        return document;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<List<Document>> GetAllAsync(CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Documents
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, string status, int chunkCount, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var doc = await db.Documents.FindAsync(new object[] { id }, ct);
        if (doc is null) return;
        doc.Status      = status;
        doc.ChunkCount  = chunkCount;
        doc.ProcessedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
