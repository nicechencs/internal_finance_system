using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class TagRuleConfiguration : IEntityTypeConfiguration<TagRule>
{
    public void Configure(EntityTypeBuilder<TagRule> builder)
    {
        builder.ToTable("tag_rules");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.RuleName).HasColumnName("rule_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Priority).HasColumnName("priority").IsRequired();
        builder.Property(e => e.TargetScope).HasColumnName("target_scope").HasMaxLength(50).IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<TagScope>(v, true));
        builder.Property(e => e.MatchField).HasColumnName("match_field").HasMaxLength(50).IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<RuleMatchField>(v, true));
        builder.Property(e => e.MatchOperator).HasColumnName("match_operator").HasMaxLength(20).IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<RuleMatchOperator>(v, true));
        builder.Property(e => e.MatchValue).HasColumnName("match_value").IsRequired();
        builder.Property(e => e.MatchValueMax).HasColumnName("match_value_max");
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");

        builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Priority).HasDatabaseName("idx_tag_rules_priority").HasFilter("is_active = true");
        builder.HasIndex(e => e.TargetScope).HasDatabaseName("idx_tag_rules_target_scope").HasFilter("is_active = true");
        builder.HasIndex(e => e.CreatedBy).HasDatabaseName("idx_tag_rules_created_by").HasFilter("is_deleted = false");
    }
}
