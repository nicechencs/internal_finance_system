using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("import_batches");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(e => e.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.FileSize)
            .HasColumnName("file_size");

        builder.Property(e => e.ImportDate)
            .HasColumnName("import_date")
            .IsRequired();

        builder.Property(e => e.RecordCount)
            .HasColumnName("record_count")
            .IsRequired();

        builder.Property(e => e.SuccessCount)
            .HasColumnName("success_count")
            .IsRequired();

        builder.Property(e => e.DuplicateCount)
            .HasColumnName("duplicate_count")
            .IsRequired();

        builder.Property(e => e.ErrorCount)
            .HasColumnName("error_count")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<ImportBatchStatus>(v, true));

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.ImportedBy)
            .HasColumnName("imported_by");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        // Relationships
        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ImportedByUser)
            .WithMany()
            .HasForeignKey(e => e.ImportedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("idx_import_batches_account");

        builder.HasIndex(e => e.ImportDate)
            .HasDatabaseName("idx_import_batches_date");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_import_batches_status");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_import_batches_created_by")
            .HasFilter("is_deleted = false");
    }
}
