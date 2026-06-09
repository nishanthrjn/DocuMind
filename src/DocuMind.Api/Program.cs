using DocuMind.Core.Parsers;
using DocuMind.Core.Services;
using DocuMind.Domain.Interfaces;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Pgvector.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString  = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=documind;Username=documind;Password=documind_dev";
var ollamaEndpoint    = builder.Configuration["Ollama:Endpoint"]       ?? "http://localhost:11434";

// Set 15-minute timeout for all HttpClients — Ollama needs long timeout on CPU
builder.Services.ConfigureHttpClientDefaults(b =>
    b.ConfigureHttpClient(c => c.Timeout = TimeSpan.FromMinutes(15)));
var ollamaEmbedModel  = builder.Configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";

// Extend timeout for embedding — large PDFs take longer to embed on CPU
builder.Services.ConfigureHttpClientDefaults(b =>
    b.ConfigureHttpClient(c => c.Timeout = TimeSpan.FromMinutes(10)));
var ollamaChatModel   = builder.Configuration["Ollama:ChatModel"]      ?? "llama3.2";
var groqApiKey        = builder.Configuration["Groq:ApiKey"]             ?? "";
var groqChatModel     = builder.Configuration["Groq:ChatModel"]          ?? "llama-3.3-70b-versatile";

builder.Services.AddDbContextFactory<DocuMindDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));

#pragma warning disable SKEXP0070
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddOllamaTextEmbeddingGeneration(ollamaEmbedModel, new Uri(ollamaEndpoint));
// Use Groq for chat — fast cloud inference (< 2 seconds vs 60+ seconds on CPU)
if (!string.IsNullOrEmpty(groqApiKey))
{
    kernelBuilder.AddOpenAIChatCompletion(
        modelId:  groqChatModel,
        apiKey:   groqApiKey,
        endpoint: new Uri("https://api.groq.com/openai/v1"));
}
else
{
    kernelBuilder.AddOllamaChatCompletion(ollamaChatModel, new Uri(ollamaEndpoint));
}
var kernel = kernelBuilder.Build();
#pragma warning restore SKEXP0070

builder.Services.AddSingleton(kernel);
#pragma warning disable SKEXP0001
builder.Services.AddSingleton(
    kernel.GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>());
#pragma warning restore SKEXP0001

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChunkRepository,    ChunkRepository>();
builder.Services.AddScoped<IChunkingService,    ChunkingService>();
builder.Services.AddScoped<IEmbeddingService,   EmbeddingService>();
builder.Services.AddScoped<IQueryService,       QueryService>();
builder.Services.AddScoped<IngestionService>();
builder.Services.AddSingleton<IDocumentParser, DocuMind.Core.Parsers.PdfDocumentParser>();
builder.Services.AddSingleton<IDocumentParser, DocuMind.Core.Parsers.PlainTextParser>();
builder.Services.AddSingleton<DocumentParserDispatcher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<DocuMindDbContext>>();
    await using var db = factory.CreateDbContext();
    await db.Database.MigrateAsync();
}

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "DocuMind API";
    options.Theme = ScalarTheme.DeepSpace;
});

app.MapGet("/health", () => new
{
    Status  = "DocuMind Online",
    Time    = DateTime.UtcNow,
    Version = "1.0.0"
}).WithName("GetHealth").WithTags("System");

app.MapGet("/api/documents", async (
    IDocumentRepository repo, CancellationToken ct) =>
{
    var docs = await repo.GetAllAsync(ct);
    return Results.Ok(docs.Select(d => new
    {
        d.Id, d.FileName, d.ContentType,
        d.Status, d.ChunkCount,
        d.UploadedAt, d.ProcessedAt
    }));
}).WithName("GetDocuments").WithTags("Documents");

app.MapGet("/api/documents/{id:guid}", async (
    Guid id, IDocumentRepository repo, CancellationToken ct) =>
{
    var doc = await repo.GetByIdAsync(id, ct);
    return doc is null
        ? Results.NotFound(new { Error = $"Document {id} not found" })
        : Results.Ok(doc);
}).WithName("GetDocument").WithTags("Documents");

app.MapPost("/api/documents/ingest", async (
    HttpRequest request, IngestionService ingestion, CancellationToken ct) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { Error = "Request must be multipart/form-data" });

    var form = await request.ReadFormAsync(ct);
    var file = form.Files.GetFile("file");

    if (file is null)
        return Results.BadRequest(new { Error = "No file uploaded. Use field name 'file'" });

    await using var stream = file.OpenReadStream();
    var document = await ingestion.IngestAsync(
        stream, file.FileName, file.ContentType ?? "application/octet-stream", ct);

    return Results.Accepted($"/api/documents/{document.Id}", new
    {
        document.Id,
        document.FileName,
        document.Status,
        document.ChunkCount,
        Message = "Document ingested successfully"
    });
}).WithName("IngestDocument").WithTags("Documents")
  .DisableAntiforgery();

app.MapDelete("/api/documents", async (
    IDocumentRepository docRepo, IChunkRepository chunkRepo,
    DocuMindDbContext db, CancellationToken ct) =>
{
    await db.DocumentChunks.ExecuteDeleteAsync(ct);
    await db.Documents.ExecuteDeleteAsync(ct);
    return Results.Ok(new { Message = "All documents deleted" });
}).WithName("DeleteAllDocuments").WithTags("Documents");

app.MapPost("/api/query", async (
    QueryRequest request, IQueryService queryService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
        return Results.BadRequest(new { Error = "Question cannot be empty" });

    var history = request.History?
        .Select(h => (h.Role, h.Content))
        .ToList();
    var result = await queryService.QueryAsync(request.Question, request.TopK, history, ct);
    return Results.Ok(new
    {
        result.Answer,
        result.Citations,
        result.LatencyMs,
        request.Question
    });
}).WithName("Query").WithTags("Query");

app.Run();

public record QueryRequest(string Question, int TopK = 5, List<ConversationMessage>? History = null);
public record ConversationMessage(string Role, string Content);
