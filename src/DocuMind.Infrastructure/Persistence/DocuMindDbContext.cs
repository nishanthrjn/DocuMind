using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class DocuMindDbContext : DbContext
{
    public DocuMindDbContext(DbContextOptions<DocuMindDbContext> options)
        : base(options) { }

    public DbSet<Document>            Documents            { get; set; }
    public DbSet<DocumentChunk>       DocumentChunks       { get; set; }
    public DbSet<QueryRecord>         QueryRecords         { get; set; }
    public DbSet<Conversation>        Conversations        { get; set; }
    public DbSet<ConversationMessage> ConversationMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>()
            .Property(d => d.Summary)
            .HasColumnName("summary");

        modelBuilder.Entity<Conversation>(b =>
        {
            b.ToTable("conversations");
            b.HasKey(x => x.Id);
            b.HasMany(x => x.Messages)
             .WithOne(x => x.Conversation)
             .HasForeignKey(x => x.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationMessage>(b =>
        {
            b.ToTable("conversation_messages");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ConversationId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
