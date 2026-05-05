using DocuMind.Domain.Interfaces;

namespace DocuMind.Core.Parsers;

public class DocumentParserDispatcher
{
    private readonly IEnumerable<IDocumentParser> _parsers;

    public DocumentParserDispatcher(IEnumerable<IDocumentParser> parsers)
    {
        _parsers = parsers;
    }

    public IDocumentParser GetParser(string contentType)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanParse(contentType));

        if (parser is null)
            throw new NotSupportedException(
                $"No parser registered for content type: {contentType}. " +
                $"Supported parsers: {string.Join(", ", _parsers.Select(p => p.GetType().Name))}");

        return parser;
    }
}
