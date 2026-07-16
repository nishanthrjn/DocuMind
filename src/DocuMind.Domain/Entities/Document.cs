namespace DocuMind.Domain.Entities;

public class Document
{
    public Guid     Id            { get; set; } = Guid.NewGuid();
    public string   FileName      { get; set; } = string.Empty;
    public string   ContentType   { get; set; } = string.Empty;
    public long     FileSizeBytes { get; set; }
    public DateTime UploadedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt  { get; set; }
    public string   Status        { get; set; } = "Pending";
    public int      ChunkCount    { get; set; }
    public string Metadata { get; set; } = "{}";
    public string? Summary { get; set; }
    public List<DocumentChunk> Chunks { get; set; } = new();
}

