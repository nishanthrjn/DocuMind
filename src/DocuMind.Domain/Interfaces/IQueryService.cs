using DocuMind.Domain.Entities;

namespace DocuMind.Domain.Interfaces;

public record QueryResult(string Answer, List<Citation> Citations, double LatencyMs);
public record Citation(string FileName, int? PageNumber, string ChunkPreview);

public interface IQueryService
{
    Task<QueryResult> QueryAsync(string question, int topK = 5, CancellationToken ct = default);
}
