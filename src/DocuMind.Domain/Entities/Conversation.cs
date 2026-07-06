namespace DocuMind.Domain.Entities;

public class Conversation
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public string   Title     { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ConversationMessage> Messages { get; set; } = new();
}

public class ConversationMessage
{
    public Guid     Id             { get; set; } = Guid.NewGuid();
    public Guid     ConversationId { get; set; }
    public string   Role           { get; set; } = string.Empty;
    public string   Content        { get; set; } = string.Empty;
    public string   Citations      { get; set; } = "[]";
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
}
