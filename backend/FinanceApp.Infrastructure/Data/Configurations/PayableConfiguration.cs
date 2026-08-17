using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class PayableConfiguration : IEntityTypeConfiguration<Payable>
{
    public void Configure(EntityTypeBuilder<Payable> builder)
    {
        builder.ToTable("payables", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_payables_exactly_one_counterparty",
                "(CASE WHEN supplier_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN customer_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN person_id IS NOT NULL THEN 1 ELSE 0 END) = 1");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.SupplierId)
            .HasColumnName("supplier_id");

        builder.Property(e => e.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(e => e.PersonId)
            .HasColumnName("person_id");

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id");

        builder.Property(e => e.PayableTypeId)
            .HasColumnName("payable_type_id");

        builder.Property(e => e.TotalAmount)
            .HasColumnName("total_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.PaidAmount)
            .HasColumnName("paid_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.RemainingAmount)
            .HasColumnName("remaining_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.DueDate)
            .HasColumnName("due_date");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<PayableStatus>(v, true));

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(e => e.SettledAt)
            .HasColumnName("settled_at");

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        // 乐观并发版本列（提供商无关的显式并发令牌）
        builder.Property(e => e.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        // Relationships
        builder.HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PayableType)
            .WithMany(pt => pt.Payables)
            .HasForeignKey(e => e.PayableTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.TagBindings);

        // Indexes
        builder.HasIndex(e => e.SupplierId)
            .HasDatabaseName("idx_payables_supplier");

        builder.HasIndex(e => e.CustomerId)
            .HasDatabaseName("idx_payables_customer");

        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("idx_payables_person");

        builder.HasIndex(e => e.ProjectId)
            .HasDatabaseName("idx_payables_project");

        builder.HasIndex(e => e.PayableTypeId)
            .HasDatabaseName("idx_payables_payable_type");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_payables_status");

        builder.HasIndex(e => e.DueDate)
            .HasDatabaseName("idx_payables_due_date");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_payables_created_by")
            .HasFilter("is_deleted = false");
    }
}
