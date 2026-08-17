using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class TransactionAllocationConfiguration : IEntityTypeConfiguration<TransactionAllocation>
{
    public void Configure(EntityTypeBuilder<TransactionAllocation> builder)
    {
        builder.ToTable("transaction_allocations");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id");

        builder.Property(e => e.PersonId)
            .HasColumnName("person_id");

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.AllocationRate)
            .HasColumnName("allocation_rate")
            .HasColumnType("decimal(5,2)");

        builder.Property(e => e.Description)
            .HasColumnName("description");

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
        builder.HasOne(e => e.Transaction)
            .WithMany(e => e.Allocations)
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.TransactionId)
            .HasDatabaseName("idx_allocations_transaction");

        builder.HasIndex(e => e.ProjectId)
            .HasDatabaseName("idx_allocations_project")
            .HasFilter("project_id IS NOT NULL");

        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("idx_allocations_person")
            .HasFilter("person_id IS NOT NULL");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_allocations_created_by")
            .HasFilter("is_deleted = false");
    }
}
