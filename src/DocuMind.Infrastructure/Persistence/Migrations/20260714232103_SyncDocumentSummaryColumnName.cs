using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocuMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncDocumentSummaryColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: the "summary" column already exists with this exact name
            // (Postgres folds unquoted identifiers to lowercase, so the prior
            // AddDocumentSummary migration created it as "summary" despite
            // its literal "Summary" spelling). This migration exists only to
            // reconcile the EF model snapshot with DocuMindDbContext's
            // explicit HasColumnName("summary") mapping.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
