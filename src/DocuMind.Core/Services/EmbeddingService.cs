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

    public async Task<Pgvector.Vector> EmbedAsync(string text, CancellationToken ct)
    {
        // Use independent token — not linked to request ct — so HttpClient timeout
        // does not cancel the embedding call
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var result = await _embeddingGenerator.GenerateEmbeddingAsync(
            text, cancellationToken: cts.Token);
        return new Pgvector.Vector(result.ToArray());
    }

    public async Task<List<DocumentChunk>> EmbedChunksAsync(
        List<DocumentChunk> chunks, CancellationToken ct)
    {
        const int batchSize = 5;
        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            var batch = chunks.Skip(i).Take(batchSize).ToList();

            // Independent timeout per batch — not linked to HTTP request
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            var embeddings = await _embeddingGenerator.GenerateEmbeddingsAsync(
                batch.Select(c => c.Content).ToList(),
                cancellationToken: cts.Token);

            for (int j = 0; j < batch.Count; j++)
                batch[j].Embedding = new Pgvector.Vector(embeddings[j].ToArray());
        }
        return chunks;
    }
}



