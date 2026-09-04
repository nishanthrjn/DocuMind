using DocuMind.Domain.Entities;

namespace DocuMind.Domain.Interfaces;

public interface IReranker
{
    Task<List<DocumentChunk>> RerankAsync(
        string query, List<DocumentChunk> candidates, int topN, CancellationToken ct);
}
