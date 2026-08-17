using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ShortName)
            .HasColumnName("short_name")
            .HasMaxLength(100);

        builder.Property(e => e.ContactPerson)
            .HasColumnName("contact_person")
            .HasMaxLength(100);

        builder.Property(e => e.ContactPhone)
            .HasColumnName("contact_phone")
            .HasMaxLength(50);

        builder.Property(e => e.ContactEmail)
            .HasColumnName("contact_email")
            .HasMaxLength(100);

        builder.Property(e => e.Address)
            .HasColumnName("address");

        builder.Property(e => e.TaxNumber)
            .HasColumnName("tax_number")
            .HasMaxLength(50);

        builder.Property(e => e.BankAccount)
            .HasColumnName("bank_account")
            .HasMaxLength(50);

        builder.Property(e => e.BankName)
            .HasColumnName("bank_name")
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

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
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.TagBindings);

        // Indexes
        builder.HasIndex(e => e.Name)
            .HasDatabaseName("idx_customers_name")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("idx_customers_active")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_customers_created_by")
            .HasFilter("is_deleted = false");
    }
}
