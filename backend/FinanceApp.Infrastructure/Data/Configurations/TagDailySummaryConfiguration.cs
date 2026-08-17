using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class TagDailySummaryConfiguration : IEntityTypeConfiguration<TagDailySummary>
{
    public void Configure(EntityTypeBuilder<TagDailySummary> builder)
    {
        builder.ToTable("tag_daily_summaries");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(s => s.SummaryDate).HasColumnName("summary_date").IsRequired();
        builder.Property(s => s.TagId).HasColumnName("tag_id").IsRequired();
        builder.Property(s => s.MetricScope).HasColumnName("metric_scope").HasMaxLength(20).IsRequired();
        builder.Property(s => s.IncomeAmount).HasColumnName("income_amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.ExpenseAmount).HasColumnName("expense_amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.NetAmount).HasColumnName("net_amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.TransactionCount).HasColumnName("transaction_count").IsRequired();
        builder.Property(s => s.Version).HasColumnName("version").IsRequired();

        // BaseEntity 字段
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");

        // 全局软删除过滤器
        builder.HasQueryFilter(s => !s.IsDeleted);

        // 外键
        builder.HasOne(s => s.Tag)
            .WithMany()
            .HasForeignKey(s => s.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // 唯一索引：同一 tag 同一 scope 同一日期只有一条记录
        builder.HasIndex(s => new { s.SummaryDate, s.TagId, s.MetricScope })
            .HasDatabaseName("ux_tag_daily_summaries_date_tag_scope")
            .IsUnique()
            .HasFilter("is_deleted = false");

        // 查询索引
        builder.HasIndex(s => new { s.TagId, s.MetricScope, s.SummaryDate })
            .HasDatabaseName("idx_tag_daily_summaries_tag_scope_date")
            .HasFilter("is_deleted = false");
    }
}
