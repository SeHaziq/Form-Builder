
using FormBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FormBuilder.Infrastructure.Data.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(question => question.Id);

        builder.Property(question => question.Label)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(question => question.Type)
            .IsRequired();

        builder.HasIndex(question => new
            {
                question.FormVersionId,
                question.Position
            })
            .IsUnique();

        builder.HasMany(question => question.Options)
            .WithOne(option => option.Question)
            .HasForeignKey(option => option.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
