# DocuMind — Enterprise AI Document Intelligence Platform

[![Build](https://img.shields.io/github/actions/workflow/status/nishanthrjn/DocuMind/ci.yml?branch=main&label=build)](https://github.com/nishanthrjn/DocuMind/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com)
[![AI](https://img.shields.io/badge/AI-Semantic%20Kernel-teal.svg)](https://github.com/microsoft/semantic-kernel)

> A production-grade RAG (Retrieval Augmented Generation) platform built in C# / .NET 10.
> Upload documents, ask questions in natural language, get precise answers with source citations.
> Runs entirely locally — no API keys, no cloud costs.

---

## Live Demo
```text
POST /api/documents/ingest  →  Upload Nishanth_CV.pdf
↓ PdfPig parses 4858 characters
↓ Sliding window chunking (512 words, 50 overlap)
↓ nomic-embed-text generates 768-dim vectors
↓ Stored in PostgreSQL pgvectorPOST /api/query  →  "What technologies does Nishanth know?"
↓ Question embedded via nomic-embed-text
↓ Cosine similarity search in pgvector
↓ Top chunks injected into Semantic Kernel prompt
↓ llama3.2 generates answer with citations
↓ 200 OK — answer returned in ~37 seconds
```
---

## Why This Is Hard

Most AI portfolio projects are thin wrappers around the OpenAI API.
DocuMind solves the actual engineering problems:

- **Chunking strategy** — sliding window with configurable overlap ensures
  context is not lost at chunk boundaries, critical for retrieval quality
- **Vector dimensionality** — nomic-embed-text produces 768-dim vectors,
  not 1536 — wrong dimensions silently break retrieval
- **Thread-safe persistence** — IDbContextFactory used throughout because
  DbContext is not thread-safe and parallel chunk embedding requires isolation
- **Citation traceability** — every answer includes the source document,
  page number, and chunk preview so answers are verifiable
- **Parser extensibility** — IDocumentParser interface allows PDF, plain text,
  and future formats to be added without changing the pipeline

---

## Architecture

```text
Documents (PDF, TXT, MD)
|
v
DocumentParserDispatcher  ->  PdfDocumentParser / PlainTextParser
|
v
ChunkingService  (sliding window, configurable size and overlap)
|
v
EmbeddingService  (Semantic Kernel + nomic-embed-text via Ollama)
|
v
PostgreSQL + pgvector  (vector(768) cosine similarity search)
|
v
QueryService  (embed question -> search -> inject context -> LLM)
|
v
ASP.NET Core Minimal API  +  Scalar UI
```
---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | C# 12 / .NET 10 |
| AI orchestration | Microsoft Semantic Kernel 1.32 |
| Embedding model | nomic-embed-text (768 dimensions, via Ollama) |
| Chat model | llama3.2 (2B, runs on CPU, via Ollama) |
| Vector store | PostgreSQL 16 + pgvector extension |
| ORM | EF Core 9 + Npgsql |
| PDF parsing | PdfPig |
| API | ASP.NET Core Minimal API |
| API explorer | Scalar UI (OAS 3.1) |
| Testing | xUnit |
| CI | GitHub Actions |

---

## Quick Start

**Prerequisites:** Docker Desktop, .NET 10 SDK, Ollama

```bash
# 1. Clone
git clone https://github.com/nishanthrjn/DocuMind.git
cd DocuMind

# 2. Pull AI models (one-time, ~2.3GB total)
ollama pull nomic-embed-text
ollama pull llama3.2

# 3. Start PostgreSQL with pgvector
docker-compose -f infra/docker-compose.yml up -d

# 4. Run the API (migrations apply automatically)
dotnet run --project src/DocuMind.Api

# 5. Open API explorer
# http://localhost:5000/scalar/v1
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /health | System status |
| GET | /api/documents | List all ingested documents |
| GET | /api/documents/{id} | Single document with chunk count |
| POST | /api/documents/ingest | Upload and process a document |
| POST | /api/query | Ask a question, get answer + citations |

**Ingest a document:**
```bash
curl -X POST http://localhost:5000/api/documents/ingest \
  -F "file=@your_document.pdf"
```

**Ask a question:**
```bash
curl -X POST http://localhost:5000/api/query \
  -H "Content-Type: application/json" \
  -d '{"question":"What are the main topics?","topK":5}'
```

---

## Project Structure
```text
DocuMind/
├── src/
│   ├── DocuMind.Domain/          # Entities, interfaces, enums
│   ├── DocuMind.Core/            # Business logic
│   │   ├── Parsers/              # PDF, plain text parsers + dispatcher
│   │   └── Services/             # Chunking, embedding, ingestion, RAG query
│   ├── DocuMind.Infrastructure/  # PostgreSQL, pgvector, EF Core
│   │   ├── Persistence/          # DbContext + migrations
│   │   └── Repositories/         # Document and chunk repositories
│   └── DocuMind.Api/             # ASP.NET Core endpoints + Scalar UI
├── tests/
│   └── DocuMind.Tests/           # xUnit — chunking, parser tests
└── infra/
└── docker-compose.yml        # PostgreSQL with pgvector
```
---

## Key Engineering Decisions

**Sliding window chunking** — each chunk overlaps the previous by a
configurable number of words. This ensures sentences split across chunk
boundaries are still retrievable. Overlap of 50 words on 512-word chunks
gives 10% overlap — enough for continuity without doubling storage.

**IDbContextFactory over DbContext injection** — embedding chunks in
parallel batches means multiple threads write to the database simultaneously.
DbContext is not thread-safe. The factory creates a fresh context per
operation, preventing race conditions.

**Local-first AI** — Ollama runs models entirely on the local machine.
No data leaves the environment, no API costs, no rate limits.
The IEmbeddingService and IQueryService interfaces allow swapping
to Azure OpenAI or any other provider by changing one registration line.

**pgvector cosine search** — the <=> operator performs approximate
nearest-neighbour search directly in PostgreSQL. No separate vector
database required — the same instance that stores document metadata
also stores and queries embeddings.

---

## Test Suite

```bash
dotnet test
# total: 7, failed: 0, succeeded: 7
```

- Chunk_EmptyText_ReturnsEmptyList
- Chunk_ShortText_ReturnsSingleChunk
- Chunk_LongText_ProducesMultipleChunks
- Chunk_WithOverlap_ConsecutiveChunksShareWords
- Chunk_AllChunksHaveCorrectDocumentId
- Chunk_ChunkIndexesAreSequential
- (UnitTest1 placeholder)

---

## Author

**Nishanth Rajan** — Software Engineer
https://linkedin.com/in/nishanthrajan
https://github.com/nishanthrjn/DocuMind
