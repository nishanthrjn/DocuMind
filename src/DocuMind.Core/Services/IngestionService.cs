using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Domain.Interfaces;
using DocuMind.Core.Parsers;
using Microsoft.Extensions.Logging;

namespace DocuMind.Core.Services;

public class IngestionService
{
    private readonly DocumentParserDispatcher  _dispatcher;
    private readonly IChunkingService          _chunker;
    private readonly IEmbeddingService         _embedder;
    private readonly IDocumentRepository       _documentRepo;
    private readonly IChunkRepository          _chunkRepo;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        DocumentParserDispatcher  dispatcher,
        IChunkingService          chunker,
        IEmbeddingService         embedder,
        IDocumentRepository       documentRepo,
        IChunkRepository          chunkRepo,
        ILogger<IngestionService> logger)
    {
        _dispatcher   = dispatcher;
        _chunker      = chunker;
        _embedder     = embedder;
        _documentRepo = documentRepo;
        _chunkRepo    = chunkRepo;
        _logger       = logger;
    }

    public async Task<Document> IngestAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct)
    {
        _logger.LogInformation("Starting ingestion for {FileName}", fileName);

        var document = new Document
        {
            FileName      = fileName,
            ContentType   = contentType,
            FileSizeBytes = fileStream.Length,
            Status        = DocumentStatus.Processing,
            UploadedAt    = DateTime.UtcNow
        };
        await _documentRepo.SaveAsync(document, ct);

        try
        {
            // 1. Parse
            var parser  = _dispatcher.GetParser(contentType);
            var rawText = await parser.ParseAsync(fileStream, ct);
            rawText = rawText.Replace("\0", " ").Trim();
            rawText = new string(rawText.Where(c => c != '\0').ToArray());
            _logger.LogInformation("Parsed {Chars} characters from {FileName}", rawText.Length, fileName);

            // 2. Chunk
            var chunks = _chunker.Chunk(document.Id, rawText);
            _logger.LogInformation("Created {Count} chunks from {FileName}", chunks.Count, fileName);

            // 3. Embed
            var embeddedChunks = await _embedder.EmbedChunksAsync(chunks, ct);

            // 4. Persist
            await _chunkRepo.SaveChunksAsync(embeddedChunks, ct);

            // 5. Mark processed
            await _documentRepo.UpdateStatusAsync(document.Id, DocumentStatus.Processed, chunks.Count, ct);
            document.Status      = DocumentStatus.Processed;
            document.ProcessedAt = DateTime.UtcNow;
            document.ChunkCount  = chunks.Count;
            _logger.LogInformation("Ingestion complete for {FileName} — {Count} chunks stored", fileName, chunks.Count);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for {FileName}", fileName);
            await _documentRepo.UpdateStatusAsync(document.Id, DocumentStatus.Failed, 0, ct);
            throw;
        }
    }
}
