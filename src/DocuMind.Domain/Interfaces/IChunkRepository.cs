using DocuMind.Domain.Entities;

namespace DocuMind.Domain.Interfaces;

public interface IChunkRepository
{
    Task                      SaveChunksAsync(List<DocumentChunk> chunks, CancellationToken ct);
    Task<List<DocumentChunk>> SearchSimilarAsync(Pgvector.Vector queryEmbedding, int topK, CancellationToken ct);
}

