
using System.ComponentModel.DataAnnotations;
using FormBuilder.Core.Enums;

namespace FormBuilder.Api.DTOs.Forms;

public class UpdateQuestionRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Label { get; set; } = string.Empty;

    public QuestionType Type { get; set; }

    public bool IsRequired { get; set; }

    public List<string> Options { get; set; } = [];
}
