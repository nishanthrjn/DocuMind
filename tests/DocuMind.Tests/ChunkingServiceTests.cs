using DocuMind.Core.Services;
using Xunit;

namespace DocuMind.Tests;

public class ChunkingServiceTests
{
    private readonly ChunkingService _sut = new();

    [Fact]
    public void Chunk_EmptyText_ReturnsEmptyList()
    {
        var result = _sut.Chunk(Guid.NewGuid(), string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        var text   = string.Join(" ", Enumerable.Repeat("word", 100));
        var result = _sut.Chunk(Guid.NewGuid(), text, chunkSize: 512);

        Assert.Single(result);
        Assert.Equal(0, result[0].ChunkIndex);
        Assert.Equal(100, result[0].TokenCount);
    }

    [Fact]
    public void Chunk_LongText_ProducesMultipleChunks()
    {
        var text   = string.Join(" ", Enumerable.Repeat("word", 1000));
        var result = _sut.Chunk(Guid.NewGuid(), text, chunkSize: 100, overlap: 10);

        Assert.True(result.Count > 1);
        Assert.All(result, c => Assert.False(string.IsNullOrWhiteSpace(c.Content)));
    }

    [Fact]
    public void Chunk_WithOverlap_ConsecutiveChunksShareWords()
    {
        var words  = Enumerable.Range(1, 200).Select(i => $"word{i}").ToArray();
        var text   = string.Join(" ", words);
        var result = _sut.Chunk(Guid.NewGuid(), text, chunkSize: 100, overlap: 20);

        var chunk0Last20  = result[0].Content.Split(' ').TakeLast(20).ToArray();
        var chunk1First20 = result[1].Content.Split(' ').Take(20).ToArray();

        Assert.Equal(chunk0Last20, chunk1First20);
    }

    [Fact]
    public void Chunk_AllChunksHaveCorrectDocumentId()
    {
        var docId  = Guid.NewGuid();
        var text   = string.Join(" ", Enumerable.Repeat("word", 500));
        var result = _sut.Chunk(docId, text, chunkSize: 100, overlap: 10);

        Assert.All(result, c => Assert.Equal(docId, c.DocumentId));
    }

    [Fact]
    public void Chunk_ChunkIndexesAreSequential()
    {
        var text     = string.Join(" ", Enumerable.Repeat("word", 500));
        var result   = _sut.Chunk(Guid.NewGuid(), text, chunkSize: 100, overlap: 10);
        var expected = Enumerable.Range(0, result.Count).ToList();

        Assert.Equal(expected, result.Select(c => c.ChunkIndex).ToList());
    }
}
