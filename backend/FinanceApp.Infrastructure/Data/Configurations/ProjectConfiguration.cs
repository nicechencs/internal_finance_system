using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ProjectCode)
            .HasColumnName("project_code")
            .HasMaxLength(50);

        builder.Property(e => e.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(e => e.ContractAmount)
            .HasColumnName("contract_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.ReceivedAmount)
            .HasColumnName("received_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.ReceivableAmount)
            .HasColumnName("receivable_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.TotalCost)
            .HasColumnName("total_cost")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.ProfitAmount)
            .HasColumnName("profit_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.ProfitRate)
            .HasColumnName("profit_rate")
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(e => e.StartDate)
            .HasColumnName("start_date");

        builder.Property(e => e.EndDate)
            .HasColumnName("end_date");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<ProjectStatus>(v, true));

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        // Relationships
        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.TagBindings);

        // Indexes
        builder.HasIndex(e => e.CustomerId)
            .HasDatabaseName("idx_projects_customer")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_projects_status")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.ProjectCode)
            .HasDatabaseName("idx_projects_code")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_projects_created_by")
            .HasFilter("is_deleted = false");
    }
}
