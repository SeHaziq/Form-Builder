
using FormBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FormBuilder.Infrastructure.Data.Configurations;

public class FormConfiguration : IEntityTypeConfiguration<Form>
{
    public void Configure(EntityTypeBuilder<Form> builder)
    {
        builder.HasKey(form => form.Id);

        builder.Property(form => form.OwnerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(form => form.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(form => form.Versions)
            .WithOne(version => version.Form)
            .HasForeignKey(version => version.FormId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
