using DocuMind.Domain.Entities;
using DocuMind.Domain.Interfaces;
using Microsoft.SemanticKernel.Embeddings;

namespace DocuMind.Core.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingGenerator;

    public EmbeddingService(ITextEmbeddingGenerationService embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var result = await _embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return result.ToArray();
    }

    public async Task<List<DocumentChunk>> EmbedChunksAsync(
        List<DocumentChunk> chunks, CancellationToken ct)
    {
        // Process in batches of 20 to avoid rate limiting
        // Each batch awaits before the next — predictable, debuggable
        const int batchSize = 20;

        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            var batch = chunks.Skip(i).Take(batchSize).ToList();

            var embeddings = await _embeddingGenerator.GenerateEmbeddingsAsync(
                batch.Select(c => c.Content).ToList(),
                cancellationToken: ct);

            for (int j = 0; j < batch.Count; j++)
                batch[j].Embedding = embeddings[j].ToArray();
        }

        return chunks;
    }
}
