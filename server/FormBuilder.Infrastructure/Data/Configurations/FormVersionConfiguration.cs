
using FormBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FormBuilder.Infrastructure.Data.Configurations;

public class FormVersionConfiguration
    : IEntityTypeConfiguration<FormVersion>
{
    public void Configure(EntityTypeBuilder<FormVersion> builder)
    {
        builder.HasKey(version => version.Id);

        builder.Property(version => version.VersionNumber)
            .IsRequired();

        builder.Property(version => version.Status)
            .IsRequired();

        builder.HasIndex(version => new
            {
                version.FormId,
                version.VersionNumber
            })
            .IsUnique();
    }
}
