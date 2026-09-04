using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Interfaces;

namespace DocuMind.Core.Services;

public class CohereReranker : IReranker
{
    private readonly HttpClient _http;
    private readonly string _model;

    public CohereReranker(HttpClient http, string model = "rerank-v3.5")
    {
        _http  = http;
        _model = model;
    }

    public async Task<List<DocumentChunk>> RerankAsync(
        string query, List<DocumentChunk> candidates, int topN, CancellationToken ct)
    {
        if (candidates.Count == 0) return candidates;

        var requestBody = new CohereRerankRequest(
            Model:     _model,
            Query:     query,
            Documents: candidates.Select(c => c.Content).ToList(),
            TopN:      Math.Min(topN, candidates.Count));

        var response = await _http.PostAsJsonAsync("v2/rerank", requestBody, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CohereRerankResponse>(cancellationToken: ct);
        if (result?.Results is null) return candidates.Take(topN).ToList();

        return result.Results
            .OrderByDescending(r => r.RelevanceScore)
            .Select(r => candidates[r.Index])
            .ToList();
    }
}

// Used automatically when no Cohere key is configured — same graceful-degradation
// pattern as the Groq -> Ollama chat fallback in Program.cs.
public class NoOpReranker : IReranker
{
    public Task<List<DocumentChunk>> RerankAsync(
        string query, List<DocumentChunk> candidates, int topN, CancellationToken ct)
        => Task.FromResult(candidates.Take(topN).ToList());
}

file record CohereRerankRequest(
    [property: JsonPropertyName("model")]     string Model,
    [property: JsonPropertyName("query")]     string Query,
    [property: JsonPropertyName("documents")] List<string> Documents,
    [property: JsonPropertyName("top_n")]     int TopN);

file record CohereRerankResponse(
    [property: JsonPropertyName("results")] List<CohereRerankResult> Results);

file record CohereRerankResult(
    [property: JsonPropertyName("index")]            int Index,
    [property: JsonPropertyName("relevance_score")]  double RelevanceScore);
