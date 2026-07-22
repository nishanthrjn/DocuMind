using DocuMind.Domain.Entities;

namespace DocuMind.Domain.Interfaces;

public interface IEmbeddingService
{
    Task<Pgvector.Vector>             EmbedAsync(string text, CancellationToken ct);
    Task<List<DocumentChunk>> EmbedChunksAsync(List<DocumentChunk> chunks, CancellationToken ct);
}

