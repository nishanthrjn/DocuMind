using DocuMind.Domain.Interfaces;
using UglyToad.PdfPig;
using System.Text;

namespace DocuMind.Core.Parsers;

public class PdfDocumentParser : IDocumentParser
{
    public bool CanParse(string contentType) =>
        contentType is "application/pdf" or "pdf";

    public Task<string> ParseAsync(Stream fileStream, CancellationToken ct)
    {
        var sb = new StringBuilder();

        using var pdf = PdfDocument.Open(fileStream);

        foreach (var page in pdf.GetPages())
        {
            var words = page.GetWords();
            var line  = string.Join(" ", words.Select(w => w.Text));

            if (!string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine(line);
                sb.AppendLine();
            }
        }

        return Task.FromResult(sb.ToString().Trim());
    }
}
