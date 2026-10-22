
namespace FormBuilder.Core.Entities;

public class SubmissionAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SubmissionId { get; set; }

    public Guid QuestionId { get; set; }

    public string? Value { get; set; }

    public Submission Submission { get; set; } = null!;

    public Question Question { get; set; } = null!;

    public Guid FormVersionId { get; set; }
}
