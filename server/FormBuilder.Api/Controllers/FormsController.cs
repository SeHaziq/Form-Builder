using FormBuilder.Core.Enums;
using FormBuilder.Api.DTOs.Forms;
using FormBuilder.Core.Entities;
using FormBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormBuilder.Infrastructure.Services;

namespace FormBuilder.Api.Controllers;

[ApiController]
[Route("api/v1/forms")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly FormBuilderDbContext _dbContext;

    public FormsController(FormBuilderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> CreateForm(
        [FromBody] CreateFormRequest request)
    {
        var title = request.Title.Trim();

        if (title.Length == 0)
        {
            return BadRequest(new
            {
                message = "Form title cannot be empty."
            });
        }

        var ownerId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            return Unauthorized();
        }

        var form = new Form
        {
            Title = title,
            OwnerId = ownerId
        };

        _dbContext.Forms.Add(form);
        await _dbContext.SaveChangesAsync();

        return Created(
            $"/api/v1/forms/{form.Id}",
            new
            {
                form.Id,
                form.Title,
                form.CreatedAt,
                form.UpdatedAt
            });
    }

    [HttpGet]
    public async Task<IActionResult> GetForms()
    {
        var ownerId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            return Unauthorized();
        }

        var forms = await _dbContext.Forms
            .Where(form => form.OwnerId == ownerId)
            .OrderByDescending(form => form.UpdatedAt)
            .Select(form => new
            {
                form.Id,
                form.Title,
                form.CreatedAt,
                form.UpdatedAt
            })
            .ToListAsync();

        return Ok(forms);
    }


    


    

    


   
    


    

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetForm(Guid id)
    {
        var ownerId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            return Unauthorized();
        }

        var form = await _dbContext.Forms
            .Where(form => form.Id == id && form.OwnerId == ownerId)
            .Select(form => new
            {
                form.Id,
                form.Title,
                form.CreatedAt,
                form.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (form is null)
        {
            return NotFound();
        }

        return Ok(form);
    }


    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishForm(Guid id)
    {
        var ownerId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            return Unauthorized();
        }

        var form = await _dbContext.Forms
            .Include(form => form.Versions)
                .ThenInclude(version => version.Questions)
            .FirstOrDefaultAsync(
                form => form.Id == id && form.OwnerId == ownerId);

        if (form is null)
        {
            return NotFound();
        }

        var draft = form.Versions
            .Where(version => version.Status == FormVersionStatus.Draft)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        if (draft is null)
        {
            return BadRequest(new
            {
                message = "There is no draft version to publish."
            });
        }

        if (draft.Questions.Count == 0)
        {
            return BadRequest(new
            {
                message = "Add at least one question before publishing."
            });
        }

        draft.Status = FormVersionStatus.Published;
        draft.PublishedAt = DateTimeOffset.UtcNow;
        form.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            formId = form.Id,
            versionId = draft.Id,
            versionNumber = draft.VersionNumber,
            status = draft.Status.ToString(),
            publishedAt = draft.PublishedAt
        });
    }


   
[HttpPost("{id:guid}/draft")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateDraft(Guid id)
{
    var ownerId = User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(ownerId))
    {
        return Unauthorized();
    }

    var form = await _dbContext.Forms
        .Include(form => form.Versions)
            .ThenInclude(version => version.Questions)
                .ThenInclude(question => question.Options)
        .FirstOrDefaultAsync(
            form => form.Id == id && form.OwnerId == ownerId);

    if (form is null)
    {
        return NotFound();
    }

    // Reuse an existing draft whenever possible.
    var existingDraft = form.Versions
        .Where(version => version.Status == FormVersionStatus.Draft)
        .OrderByDescending(version => version.VersionNumber)
        .FirstOrDefault();

    if (existingDraft is not null)
    {
        return Ok(new
        {
            versionId = existingDraft.Id,
            versionNumber = existingDraft.VersionNumber,
            status = existingDraft.Status.ToString()
        });
    }

    var latestPublished = form.Versions
        .Where(version => version.Status == FormVersionStatus.Published)
        .OrderByDescending(version => version.VersionNumber)
        .FirstOrDefault();

    var latestVersionNumber = form.Versions
        .Select(version => version.VersionNumber)
        .DefaultIfEmpty(0)
        .Max();

    var draft = new FormVersion
    {
        FormId = form.Id,
        VersionNumber = latestVersionNumber + 1,
        Status = FormVersionStatus.Draft
    };

    // Copy questions and options if a published version exists.
    if (latestPublished is not null)
    {
        foreach (var publishedQuestion in latestPublished.Questions
                     .OrderBy(question => question.Position))
        {
            var draftQuestion = new Question
            {
                FormVersion = draft,
                Label = publishedQuestion.Label,
                Type = publishedQuestion.Type,
                IsRequired = publishedQuestion.IsRequired,
                Position = publishedQuestion.Position
            };

            foreach (var publishedOption in publishedQuestion.Options
                         .OrderBy(option => option.Position))
            {
                draftQuestion.Options.Add(new QuestionOption
                {
                    Label = publishedOption.Label,
                    Position = publishedOption.Position
                });
            }

            draft.Questions.Add(draftQuestion);
        }
    }

    _dbContext.FormVersions.Add(draft);
    form.UpdatedAt = DateTimeOffset.UtcNow;

    try
    {
        await _dbContext.SaveChangesAsync();
    }
    catch (DbUpdateException ex)
        when (ex.InnerException is Npgsql.PostgresException pgException
              && pgException.SqlState == "23505"
              && pgException.ConstraintName ==
                 "IX_FormVersions_FormId_VersionNumber")
    {
        // A concurrent request may have created the draft first.
        _dbContext.ChangeTracker.Clear();

        var recoveredDraft = await _dbContext.FormVersions
            .Where(version =>
                version.FormId == id &&
                version.Status == FormVersionStatus.Draft)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new
            {
                version.Id,
                version.VersionNumber,
                version.Status
            })
            .FirstOrDefaultAsync();

        if (recoveredDraft is null)
        {
            throw;
        }

        return Ok(new
        {
            versionId = recoveredDraft.Id,
            versionNumber = recoveredDraft.VersionNumber,
            status = recoveredDraft.Status.ToString()
        });
    }

    return Ok(new
    {
        versionId = draft.Id,
        versionNumber = draft.VersionNumber,
        status = draft.Status.ToString()
    });
}


}
