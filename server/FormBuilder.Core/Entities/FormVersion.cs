
using FormBuilder.Core.Enums;

namespace FormBuilder.Core.Entities;

public class FormVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FormId { get; set; }

    public int VersionNumber { get; set; }

    public FormVersionStatus Status { get; set; } =
        FormVersionStatus.Draft;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? PublishedAt { get; set; }

    public Form Form { get; set; } = null!;
}
