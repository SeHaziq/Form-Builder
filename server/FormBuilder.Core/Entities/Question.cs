
using FormBuilder.Core.Enums;

namespace FormBuilder.Core.Entities;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FormVersionId { get; set; }

    public string Label { get; set; } = string.Empty;

    public QuestionType Type { get; set; }

    public bool IsRequired { get; set; }

    public int Position { get; set; }

    public FormVersion FormVersion { get; set; } = null!;

    public ICollection<QuestionOption> Options { get; set; } =
        new List<QuestionOption>();
}
