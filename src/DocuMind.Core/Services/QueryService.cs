using DocuMind.Domain.Entities;
using DocuMind.Domain.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DocuMind.Core.Services;

public class QueryService : IQueryService
{
    private readonly IEmbeddingService   _embedder;
    private readonly IChunkRepository    _chunkRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly Kernel              _kernel;

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
        string question,
        int topK = 5,
        List<(string Role, string Content)>? history = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var questionEmbedding = await _embedder.EmbedAsync(question, ct);
        var relevantChunks    = await _chunkRepo.SearchSimilarAsync(questionEmbedding, topK, ct);

        if (relevantChunks.Count == 0)
        {
            return new QueryResult(
                Answer:    "No relevant documents found for your question.",
                Citations: new List<Citation>(),
                LatencyMs: sw.Elapsed.TotalMilliseconds);
        }

        // Build numbered context
        var contextBuilder = new StringBuilder();
        var chunkDocs      = new List<(DocumentChunk Chunk, Document? Doc)>();

        for (int i = 0; i < relevantChunks.Count; i++)
        {
            var chunk = relevantChunks[i];
            var doc   = await _documentRepo.GetByIdAsync(chunk.DocumentId, ct);
            chunkDocs.Add((chunk, doc));
            contextBuilder.AppendLine($"[{i + 1}] Source: {doc?.FileName ?? "Unknown"}");
            contextBuilder.AppendLine(chunk.Content[..Math.Min(800, chunk.Content.Length)]);
            contextBuilder.AppendLine();
        }

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage($"""
            You are DocuMind, an expert AI research assistant. Answer questions based strictly on the provided document excerpts.

            FORMATTING RULES:
            - Write all mathematical formulas in plain text only. Never use LaTeX, backslash commands, or dollar signs.
            - Use **bold** for key terms and concepts
            - Use bullet points for lists of findings
            - Keep answers concise and well-structured
            - IMPORTANT: Cite sources inline using [1], [2], [3] etc. matching the source numbers below.
            - Example: "The Transformer uses attention mechanisms [1] which allow parallel processing [2]."
            - If the answer is not in the documents, say so clearly

            Document context:
            {contextBuilder}
            """);

        if (history != null)
        {
            foreach (var (role, content) in history)
            {
                if (role == "user") chatHistory.AddUserMessage(content);
                else if (role == "assistant") chatHistory.AddAssistantMessage(content);
            }
        }

        chatHistory.AddUserMessage(question);

        var chat     = _kernel.GetRequiredService<IChatCompletionService>();
        var response = await chat.GetChatMessageContentAsync(chatHistory, cancellationToken: ct);

        sw.Stop();

        var answer = CleanLatex(response.Content ?? "");

        // Build citations from chunks
        var citations = chunkDocs.Select((cd, i) => new Citation(
            FileName:     cd.Doc?.FileName ?? "Unknown",
            PageNumber:   cd.Chunk.PageNumber,
            ChunkPreview: cd.Chunk.Content[..Math.Min(600, cd.Chunk.Content.Length)] + "..."
        )).ToList();

        return new QueryResult(
            Answer:    answer,
            Citations: citations,
            LatencyMs: sw.Elapsed.TotalMilliseconds);
    }

    private static string CleanLatex(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var replacements = new (string From, string To)[]
        {
            (@"\approx",  "≈"),  (@"\Delta",  "Δ"),  (@"\delta",  "δ"),
            (@"\lambda",  "λ"),  (@"\Lambda", "Λ"),  (@"\alpha",  "α"),
            (@"\beta",    "β"),  (@"\gamma",  "γ"),  (@"\Gamma",  "Γ"),
            (@"\tau",     "τ"),  (@"\sigma",  "σ"),  (@"\Sigma",  "Σ"),
            (@"\mu",      "μ"),  (@"\pi",     "π"),  (@"\theta",  "θ"),
            (@"\phi",     "φ"),  (@"\omega",  "ω"),  (@"\cdot",   "·"),
            (@"\times",   "×"),  (@"\leq",    "≤"),  (@"\geq",    "≥"),
            (@"\neq",     "≠"),  (@"\infty",  "∞"),  (@"\int",    "∫"),
            (@"\sum",     "Σ"),  (@"\prod",   "Π"),  (@"\in",     "∈"),
            (@"\exp",     "exp"),(@"\log",    "log"),(@"\mathbf", ""),
            (@"\mathbb",  ""),   (@"\mathrm", ""),   (@"\text",   ""),
            (@"\left",    ""),   (@"\right",  ""),   (@"\bigl",   ""),
            (@"\bigr",    ""),   (@"\frac",   ""),   (@"\sqrt",   "sqrt"),
            (@"\[",       ""),   (@"\]",      ""),   (@"\(",      ""),
            (@"\)",       ""),
        };

        foreach (var (from, to) in replacements)
            text = text.Replace(from, to);

        return text.Trim();
    }
}

