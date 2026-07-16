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
        modelBuilder.Entity<Document>(b =>
        {
            b.ToTable("documents");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.FileName).HasColumnName("file_name");
            b.Property(x => x.ContentType).HasColumnName("content_type");
            b.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
            b.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
            b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.ChunkCount).HasColumnName("chunk_count");
            b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("text");
            b.Property(x => x.Summary).HasColumnName("summary");
        });

        modelBuilder.Entity<DocumentChunk>(b =>
        {
            b.ToTable("document_chunks");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.DocumentId).HasColumnName("document_id");
            b.Property(x => x.Content).HasColumnName("content");
            b.Property(x => x.PageNumber).HasColumnName("page_number");
            b.Property(x => x.ChunkIndex).HasColumnName("chunk_index");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.TokenCount).HasColumnName("token_count");
            b.Property(x => x.Embedding).HasColumnName("embedding");
        });

        modelBuilder.Entity<QueryRecord>(b =>
        {
            b.ToTable("query_records");
            b.HasKey(x => x.Id);
        });

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
