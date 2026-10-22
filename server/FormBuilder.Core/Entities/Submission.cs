
using FormBuilder.Core.Enums;

namespace FormBuilder.Core.Entities;

public class Submission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FormVersionId { get; set; }

    public SubmissionStatus Status { get; set; } =
        SubmissionStatus.InProgress;

    public DateTimeOffset StartedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    public FormVersion FormVersion { get; set; } = null!;

    public ICollection<SubmissionAnswer> Answers { get; set; } =
        new List<SubmissionAnswer>();
}
