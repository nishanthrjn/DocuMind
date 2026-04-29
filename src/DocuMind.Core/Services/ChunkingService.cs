using DocuMind.Domain.Entities;
using DocuMind.Domain.Interfaces;

namespace DocuMind.Core.Services;

public class ChunkingService : IChunkingService
{
    public List<DocumentChunk> Chunk(
        Guid   documentId,
        string fullText,
        int    chunkSize = 512,
        int    overlap   = 50)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            return new List<DocumentChunk>();

        var words      = fullText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks     = new List<DocumentChunk>();
        var index      = 0;
        var chunkIndex = 0;

        while (index < words.Length)
        {
            var end   = Math.Min(index + chunkSize, words.Length);
            var slice = words[index..end];

            chunks.Add(new DocumentChunk
            {
                Id         = Guid.NewGuid(),
                DocumentId = documentId,
                ChunkIndex = chunkIndex++,
                Content    = string.Join(' ', slice),
                TokenCount = slice.Length,
                CreatedAt  = DateTime.UtcNow
            });

            index += chunkSize - overlap;
        }

        return chunks;
    }
}
