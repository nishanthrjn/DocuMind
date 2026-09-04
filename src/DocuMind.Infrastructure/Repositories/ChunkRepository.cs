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
        float[] queryEmbedding, int topK, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var vector = new Vector(queryEmbedding);
        return await db.DocumentChunks
            .FromSql($"""
                SELECT * FROM document_chunks
                ORDER BY embedding <=> {vector}::vector
                LIMIT {topK}
                """)
            .ToListAsync(ct);
    }

    public async Task<List<DocumentChunk>> HybridSearchAsync(
        float[] queryEmbedding, string queryText, int topK, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var vector = new Vector(queryEmbedding);

        return await db.DocumentChunks
            .FromSql($"""
                WITH vector_search AS (
                    SELECT id, RANK() OVER (ORDER BY embedding <=> {vector}::vector) AS rank
                    FROM document_chunks
                    ORDER BY embedding <=> {vector}::vector
                    LIMIT 30
                ),
                text_search AS (
                    SELECT id, RANK() OVER (
                        ORDER BY ts_rank_cd(content_tsv, websearch_to_tsquery('english', {queryText})) DESC
                    ) AS rank
                    FROM document_chunks
                    WHERE content_tsv @@ websearch_to_tsquery('english', {queryText})
                    LIMIT 30
                )
                SELECT dc.* FROM document_chunks dc
                JOIN (
                    SELECT COALESCE(v.id, t.id) AS id,
                           COALESCE(1.0/(60+v.rank), 0) + COALESCE(1.0/(60+t.rank), 0) AS rrf_score
                    FROM vector_search v
                    FULL OUTER JOIN text_search t ON v.id = t.id
                    ORDER BY rrf_score DESC
                    LIMIT {topK}
                ) ranked ON dc.id = ranked.id
                ORDER BY ranked.rrf_score DESC
                """)
            .ToListAsync(ct);
    }
}
