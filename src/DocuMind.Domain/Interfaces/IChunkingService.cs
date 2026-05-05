using DocuMind.Domain.Entities;

namespace DocuMind.Domain.Interfaces;

public interface IChunkingService
{
    List<DocumentChunk> Chunk(Guid documentId, string fullText,
                               int chunkSize = 512, int overlap = 50);
}
