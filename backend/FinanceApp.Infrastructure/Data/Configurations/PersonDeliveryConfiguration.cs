using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class PersonDeliveryConfiguration : IEntityTypeConfiguration<PersonDelivery>
{
    public void Configure(EntityTypeBuilder<PersonDelivery> builder)
    {
        builder.ToTable("person_deliveries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id");

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<PersonDeliveryStatus>(v, true));

        builder.Property(e => e.AcceptedAt)
            .HasColumnName("accepted_at");

        builder.Property(e => e.AcceptedBy)
            .HasColumnName("accepted_by")
            .HasMaxLength(100);

        builder.Property(e => e.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

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

        builder.HasOne(e => e.Person)
            .WithMany(p => p.Deliveries)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("idx_person_deliveries_person_id")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.ProjectId)
            .HasDatabaseName("idx_person_deliveries_project_id")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_person_deliveries_status")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_person_deliveries_created_by")
            .HasFilter("is_deleted = false");
    }
}
