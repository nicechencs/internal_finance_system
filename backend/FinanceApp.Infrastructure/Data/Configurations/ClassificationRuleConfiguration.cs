using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class ClassificationRuleConfiguration : IEntityTypeConfiguration<ClassificationRule>
{
    public void Configure(EntityTypeBuilder<ClassificationRule> builder)
    {
        builder.ToTable("classification_rules");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.RuleName)
            .HasColumnName("rule_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(e => e.MatchField)
            .HasColumnName("match_field")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<RuleMatchField>(v, true));

        builder.Property(e => e.MatchOperator)
            .HasColumnName("match_operator")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<RuleMatchOperator>(v, true));

        builder.Property(e => e.MatchValue)
            .HasColumnName("match_value")
            .IsRequired();

        builder.Property(e => e.MatchValueMax)
            .HasColumnName("match_value_max");

        builder.Property(e => e.CategoryId)
            .HasColumnName("category_id");

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id");

        builder.Property(e => e.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(e => e.SupplierId)
            .HasColumnName("supplier_id");

        builder.Property(e => e.PersonId)
            .HasColumnName("person_id");

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
        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
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
        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("idx_rules_priority")
            .HasFilter("is_active = true");

        builder.HasIndex(e => e.MatchField)
            .HasDatabaseName("idx_rules_field")
            .HasFilter("is_active = true");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_rules_created_by")
            .HasFilter("is_deleted = false");
    }
}
