using System.Text.RegularExpressions;
using DocuMind.Domain.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics;
using System.Text;

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

        // Step 1 — embed the question
        var questionEmbedding = await _embedder.EmbedAsync(question, ct);

        // Step 2 — vector similarity search
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
        contextBuilder.AppendLine("Always cite the source document name.");
        contextBuilder.AppendLine();

        foreach (var chunk in relevantChunks)
        {
            var doc = await _documentRepo.GetByIdAsync(chunk.DocumentId, ct);
            contextBuilder.AppendLine($"[Source: {doc?.FileName ?? "Unknown"}, Page: {chunk.PageNumber}]");
            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine();
        }

        // Step 4 — build chat history with conversation context
        var chatHistory = new ChatHistory();

        // System prompt with document context
        chatHistory.AddSystemMessage($"""
            You are an AI assistant that answers questions based on uploaded documents.
            Always cite your sources. If the answer is not in the documents, say so clearly.

            Document context:
            {contextBuilder}
            """);

        // Add previous conversation turns
        if (history != null)
        {
            foreach (var (role, content) in history)
            {
                if (role == "user")
                    chatHistory.AddUserMessage(content);
                else if (role == "assistant")
                    chatHistory.AddAssistantMessage(content);
            }
        }

        // Add current question
        chatHistory.AddUserMessage(question);

        // Step 5 — call LLM with full conversation history
        var chat    = _kernel.GetRequiredService<IChatCompletionService>();
        var response = await chat.GetChatMessageContentAsync(chatHistory, cancellationToken: ct);

        sw.Stop();

        // Step 6 — build citations
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
            Answer:    CleanLatex(response.Content ?? ""),
            Citations: citations,
            LatencyMs: sw.Elapsed.TotalMilliseconds);
    }

    private static string CleanLatex(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove display math delimiters [ ... ] and ( ... )
        text = Regex.Replace(text, @"\\\[|\\\]|\\\(|\\\)", "");

        // Remove common LaTeX commands but keep content
        text = Regex.Replace(text, @"\mathbf\{([^}]+)\}", "$1");
        text = Regex.Replace(text, @"\mathbb\{([^}]+)\}", "$1");
        text = Regex.Replace(text, @"\text\{([^}]+)\}", "$1");
        text = Regex.Replace(text, @"\mathrm\{([^}]+)\}", "$1");
        text = Regex.Replace(text, @"\operatorname\{([^}]+)\}", "$1");
        text = Regex.Replace(text, @"\left|\right|\bigl|\bigr|\Bigl|\Bigr", "");
        text = Regex.Replace(text, @"\approx", "≈");
        text = Regex.Replace(text, @"\Delta", "Δ");
        text = Regex.Replace(text, @"\lambda", "λ");
        text = Regex.Replace(text, @"\alpha", "α");
        text = Regex.Replace(text, @"\beta", "β");
        text = Regex.Replace(text, @"\gamma", "γ");
        text = Regex.Replace(text, @"\tau", "τ");
        text = Regex.Replace(text, @"\sigma", "σ");
        text = Regex.Replace(text, @"\exp", "exp");
        text = Regex.Replace(text, @"\frac\{([^}]+)\}\{([^}]+)\}", "($1)/($2)");
        text = Regex.Replace(text, @"\sqrt\{([^}]+)\}", "sqrt($1)");
        text = Regex.Replace(text, @"\cdot", "·");
        text = Regex.Replace(text, @"\times", "×");
        text = Regex.Replace(text, @"\in", "∈");
        text = Regex.Replace(text, @"\sum", "Σ");
        text = Regex.Replace(text, @"\prod", "Π");
        text = Regex.Replace(text, @"\infty", "∞");
        text = Regex.Replace(text, @"\leq", "≤");
        text = Regex.Replace(text, @"\geq", "≥");
        text = Regex.Replace(text, @"\neq", "≠");
        text = Regex.Replace(text, @"\int", "∫");

        // Remove remaining backslash commands
        text = Regex.Replace(text, @"\[a-zA-Z]+\{([^}]*)\}", "$1");
        text = Regex.Replace(text, @"\[a-zA-Z]+", "");

        // Clean up extra spaces
        text = Regex.Replace(text, @"  +", " ");

        return text.Trim();
    }
}
