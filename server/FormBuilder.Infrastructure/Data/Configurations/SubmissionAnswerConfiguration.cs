
using FormBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FormBuilder.Infrastructure.Data.Configurations;

public class SubmissionAnswerConfiguration
    : IEntityTypeConfiguration<SubmissionAnswer>
{
    public void Configure(EntityTypeBuilder<SubmissionAnswer> builder)
    {
        builder.HasKey(answer => answer.Id);

        builder.Property(answer => answer.Value)
            .HasColumnType("jsonb");

        builder.HasOne(answer => answer.Question)
            .WithMany()
            .HasForeignKey(answer => new
            {
                answer.QuestionId,
                answer.FormVersionId
            })
            .HasPrincipalKey(question => new
            {
                question.Id,
                question.FormVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
