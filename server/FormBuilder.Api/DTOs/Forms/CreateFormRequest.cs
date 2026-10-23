
using System.ComponentModel.DataAnnotations;

namespace FormBuilder.Api.DTOs.Forms;

public class CreateFormRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
}
