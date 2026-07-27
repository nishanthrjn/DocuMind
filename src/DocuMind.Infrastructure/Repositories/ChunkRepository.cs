using DocuMind.Domain.Entities;
using DocuMind.Domain.Interfaces;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace DocuMind.Infrastructure.Repositories;

public class ChunkRepository : IChunkRepository
{
    private readonly IDbContextFactory<DocuMindDbContext> _contextFactory;

    public ChunkRepository(IDbContextFactory<DocuMindDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task SaveChunksAsync(List<DocumentChunk> chunks, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        db.DocumentChunks.AddRange(chunks);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<DocumentChunk>> SearchSimilarAsync(
        Pgvector.Vector queryEmbedding, int topK, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var vector = queryEmbedding;

        return await db.DocumentChunks
            .FromSql($"""
                SELECT * FROM document_chunks
                ORDER BY embedding <=> {vector}::vector
                LIMIT {topK}
                """)
            .ToListAsync(ct);
    }
}


