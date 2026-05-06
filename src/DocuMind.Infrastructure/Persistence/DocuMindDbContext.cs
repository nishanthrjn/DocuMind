using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace DocuMind.Infrastructure.Persistence;

public class DocuMindDbContext : DbContext
{
    public DocuMindDbContext(DbContextOptions<DocuMindDbContext> options)
        : base(options) { }

    public DbSet<Document>      Documents      { get; set; }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    public DbSet<QueryRecord>   QueryRecords   { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(b =>
        {
            b.ToTable("documents");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
            b.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(100);
            b.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
            b.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
            b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
            b.Property(x => x.ChunkCount).HasColumnName("chunk_count");
            b.Property(x => x.Metadata)
             .HasColumnName("metadata")
             .HasColumnType("jsonb")
             .HasConversion(
                 v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                 v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
            b.HasMany(x => x.Chunks)
             .WithOne(x => x.Document)
             .HasForeignKey(x => x.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(b =>
        {
            b.ToTable("document_chunks");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.DocumentId).HasColumnName("document_id");
            b.Property(x => x.ChunkIndex).HasColumnName("chunk_index");
            b.Property(x => x.Content).HasColumnName("content").IsRequired();
            b.Property(x => x.TokenCount).HasColumnName("token_count");
            b.Property(x => x.PageNumber).HasColumnName("page_number");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");

            // pgvector column — stores float array as a vector type
            b.Property(x => x.Embedding)
             .HasColumnName("embedding")
             .HasColumnType("vector(768)")
             .HasConversion(
                 v => v == null ? null : new Vector(v),
                 v => v == null ? null : v.ToArray());

            b.HasIndex(x => x.DocumentId)
             .HasDatabaseName("ix_document_chunks_document_id");
        });

        modelBuilder.Entity<QueryRecord>(b =>
        {
            b.ToTable("query_records");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Question).HasColumnName("question").IsRequired();
            b.Property(x => x.Answer).HasColumnName("answer").IsRequired();
            b.Property(x => x.Citations).HasColumnName("citations");
            b.Property(x => x.ChunksUsed).HasColumnName("chunks_used");
            b.Property(x => x.LatencyMs).HasColumnName("latency_ms");
            b.Property(x => x.AskedAt).HasColumnName("asked_at");
        });

        base.OnModelCreating(modelBuilder);
    }
}
