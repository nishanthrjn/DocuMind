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
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var result = await _embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken: cts.Token);
        return result.ToArray();
    }

    public async Task<List<DocumentChunk>> EmbedChunksAsync(
        List<DocumentChunk> chunks, CancellationToken ct)
    {
        const int batchSize = 5;
        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            var batch = chunks.Skip(i).Take(batchSize).ToList();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var embeddings = await _embeddingGenerator.GenerateEmbeddingsAsync(
                batch.Select(c => c.Content).ToList(),
                cancellationToken: cts.Token);
            for (int j = 0; j < batch.Count; j++)
                batch[j].Embedding = embeddings[j].ToArray();
        }
        return chunks;
    }
}
