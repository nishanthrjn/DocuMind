using DocuMind.Domain.Interfaces;
using System.Text;

namespace DocuMind.Core.Parsers;

public class PlainTextParser : IDocumentParser
{
    public bool CanParse(string contentType) =>
        contentType is "text/plain" or "text/markdown" or "text/html";

    public async Task<string> ParseAsync(Stream fileStream, CancellationToken ct)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        return await reader.ReadToEndAsync(ct);
    }
}
