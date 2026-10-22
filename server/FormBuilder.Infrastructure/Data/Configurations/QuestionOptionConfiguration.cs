
using FormBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FormBuilder.Infrastructure.Data.Configurations;

public class QuestionOptionConfiguration
    : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.HasKey(option => option.Id);

        builder.Property(option => option.Label)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(option => new
            {
                option.QuestionId,
                option.Position
            })
            .IsUnique();
    }
}
