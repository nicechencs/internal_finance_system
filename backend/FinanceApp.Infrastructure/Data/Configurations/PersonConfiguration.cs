using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Data.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("persons");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PersonType)
            .HasColumnName("person_type")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<PersonType>(v, true));

        builder.Property(e => e.Department)
            .HasColumnName("department")
            .HasMaxLength(100);

        builder.Property(e => e.Position)
            .HasColumnName("position")
            .HasMaxLength(100);

        builder.Property(e => e.IdNumber)
            .HasColumnName("id_number")
            .HasMaxLength(50);

        builder.Property(e => e.Phone)
            .HasColumnName("phone")
            .HasMaxLength(50);

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(100);

        builder.Property(e => e.BankAccount)
            .HasColumnName("bank_account")
            .HasMaxLength(50);

        builder.Property(e => e.BankName)
            .HasColumnName("bank_name")
            .HasMaxLength(100);

        builder.Property(e => e.JoinDate)
            .HasColumnName("join_date");

        builder.Property(e => e.LeaveDate)
            .HasColumnName("leave_date");

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
        builder.HasIndex(e => e.PersonType)
            .HasDatabaseName("idx_persons_type")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("idx_persons_active")
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("idx_persons_created_by")
            .HasFilter("is_deleted = false");
    }
}
