using DocuMind.Domain.Entities;

namespace DocuMind.Domain.Interfaces;

public interface IDocumentRepository
{
    Task<Document>       SaveAsync(Document document, CancellationToken ct);
    Task<Document?>      GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Document>> GetAllAsync(CancellationToken ct);
    Task                 UpdateStatusAsync(Guid id, string status, int chunkCount, CancellationToken ct);
    Task                 UpdateSummaryAsync(Guid id, string summary, CancellationToken ct);
}
