using DocuMind.Domain.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Diagnostics;
using System.Text;

namespace DocuMind.Core.Services;

public class QueryService : IQueryService
{
    private readonly IEmbeddingService  _embedder;
    private readonly IChunkRepository   _chunkRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly Kernel             _kernel;

    public QueryService(
        IEmbeddingService    embedder,
        IChunkRepository     chunkRepo,
        IDocumentRepository  documentRepo,
        Kernel               kernel)
    {
        _embedder     = embedder;
        _chunkRepo    = chunkRepo;
        _documentRepo = documentRepo;
        _kernel       = kernel;
    }

    public async Task<QueryResult> QueryAsync(
        string question, int topK = 5, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // Step 1 — embed the question using the same model used for ingestion
        var questionEmbedding = await _embedder.EmbedAsync(question, ct);

        // Step 2 — vector similarity search — cosine distance in PostgreSQL
        var relevantChunks = await _chunkRepo.SearchSimilarAsync(
            questionEmbedding, topK, ct);

        if (relevantChunks.Count == 0)
        {
            return new QueryResult(
                Answer:    "No relevant documents found for your question.",
                Citations: new List<Citation>(),
                LatencyMs: sw.Elapsed.TotalMilliseconds);
        }

        // Step 3 — build context from retrieved chunks
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("Use the following document excerpts to answer the question.");
        contextBuilder.AppendLine("Always cite the source document name for each piece of information.");
        contextBuilder.AppendLine();

        foreach (var chunk in relevantChunks)
        {
            var doc = await _documentRepo.GetByIdAsync(chunk.DocumentId, ct);
            contextBuilder.AppendLine($"[Source: {doc?.FileName ?? "Unknown"}, Page: {chunk.PageNumber}]");
            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine();
        }

        // Step 4 — inject context into prompt and call LLM
        var prompt = $"""
            {contextBuilder}
            Question: {question}

            Answer based only on the provided context. If the answer is not in the context,
            say "I could not find this information in the provided documents."
            Always end your answer with a citations section listing the sources you used.
            """;

        var response = await _kernel.InvokePromptAsync(prompt,
            cancellationToken: ct);

        sw.Stop();

        // Step 5 — build citation list from retrieved chunks
        var citations = new List<Citation>();
        foreach (var chunk in relevantChunks)
        {
            var doc = await _documentRepo.GetByIdAsync(chunk.DocumentId, ct);
            citations.Add(new Citation(
                FileName:     doc?.FileName ?? "Unknown",
                PageNumber:   chunk.PageNumber,
                ChunkPreview: chunk.Content[..Math.Min(150, chunk.Content.Length)] + "..."));
        }

        return new QueryResult(
            Answer:    response.ToString(),
            Citations: citations,
            LatencyMs: sw.Elapsed.TotalMilliseconds);
    }
}
