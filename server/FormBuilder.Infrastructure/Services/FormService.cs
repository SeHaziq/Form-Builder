
using FormBuilder.Core.Entities;
using FormBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Infrastructure.Services;

public class FormService
{
    private readonly FormBuilderDbContext _dbContext;

    public FormService(FormBuilderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateFormResult> CreateFormAsync(
        string ownerId,
        string title)
    {
        var trimmedTitle = title.Trim();

        if (trimmedTitle.Length == 0)
        {
            return new CreateFormResult(
                "Form title cannot be empty.", null);
        }

        var form = new Form
        {
            Title = trimmedTitle,
            OwnerId = ownerId
        };

        _dbContext.Forms.Add(form);
        await _dbContext.SaveChangesAsync();

        var response = new CreatedFormResponse(
            form.Id,
            form.Title,
            form.CreatedAt,
            form.UpdatedAt);

        return new CreateFormResult(null, response);
    }
}

public sealed record CreateFormResult(
    string? ErrorMessage,
    CreatedFormResponse? Form);

public sealed record CreatedFormResponse(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
