
namespace FormBuilder.Core.Entities;

public class QuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionId { get; set; }

    public string Label { get; set; } = string.Empty;

    public int Position { get; set; }

    public Question Question { get; set; } = null!;
}
