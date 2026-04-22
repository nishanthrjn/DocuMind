namespace DocuMind.Domain.Entities;

public class QueryRecord
{
    public Guid     Id         { get; set; } = Guid.NewGuid();
    public string   Question   { get; set; } = string.Empty;
    public string   Answer     { get; set; } = string.Empty;
    public string   Citations  { get; set; } = string.Empty;
    public int      ChunksUsed { get; set; }
    public double   LatencyMs  { get; set; }
    public DateTime AskedAt    { get; set; } = DateTime.UtcNow;
}
