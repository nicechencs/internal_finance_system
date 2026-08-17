using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class TagRuleTagConfiguration : IEntityTypeConfiguration<TagRuleTag>
{
    public void Configure(EntityTypeBuilder<TagRuleTag> builder)
    {
        builder.ToTable("tag_rule_tags");
        builder.HasKey(e => new { e.TagRuleId, e.TagId });
        builder.Property(e => e.TagRuleId).HasColumnName("tag_rule_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.HasOne(e => e.TagRule).WithMany(r => r.TagRuleTags).HasForeignKey(e => e.TagRuleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}
