namespace DocuMind.Domain.Interfaces;

public interface IDocumentParser
{
    bool CanParse(string contentType);
    Task<string> ParseAsync(Stream fileStream, CancellationToken ct);
}
