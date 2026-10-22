
using FormBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FormBuilder.Infrastructure.Data.Configurations;

public class SubmissionConfiguration
    : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.HasKey(submission => submission.Id);

        builder.Property(submission => submission.Status)
            .IsRequired();

        builder.HasOne(submission => submission.FormVersion)
            .WithMany()
            .HasForeignKey(submission => submission.FormVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(submission => submission.Answers)
            .WithOne(answer => answer.Submission)
            .HasForeignKey(answer => new
            {
                answer.SubmissionId,
                answer.FormVersionId
            })
            .HasPrincipalKey(submission => new
            {
                submission.Id,
                submission.FormVersionId
            })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
