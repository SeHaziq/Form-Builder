using FormBuilder.Core.Enums;
using FormBuilder.Api.DTOs.Forms;
using FormBuilder.Core.Entities;
using FormBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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


    [HttpPost("{id:guid}/questions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestion(
        Guid id,
        [FromBody] CreateQuestionRequest request)
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

        var title = request.Label.Trim();

        if (title.Length == 0)
        {
            return BadRequest(new { message = "Question text cannot be empty." });
        }

        var needsOptions = request.Type is QuestionType.SingleChoice
            or QuestionType.MultipleChoice
            or QuestionType.Dropdown;

        var options = request.Options
            .Select(option => option.Trim())
            .ToList();

        if (needsOptions &&
            (options.Count < 2 || options.Any(string.IsNullOrWhiteSpace)))
        {
            return BadRequest(new
            {
                message = "Choice questions require at least two non-empty options."
            });
        }

        if (!needsOptions && options.Count > 0)
        {
            return BadRequest(new
            {
                message = "Options are only allowed for choice questions."
            });
        }

        var draft = form.Versions
        .Where(version => version.Status == FormVersionStatus.Draft)
        .OrderByDescending(version => version.VersionNumber)
        .FirstOrDefault();

        if (draft is null)
        {
            var latestPublished = form.Versions
                .Where(version => version.Status == FormVersionStatus.Published)
                .OrderByDescending(version => version.VersionNumber)
                .FirstOrDefault();

            draft = new FormVersion
            {
                FormId = form.Id,
                VersionNumber = form.Versions.Count == 0
                    ? 1
                    : form.Versions.Max(version => version.VersionNumber) + 1,
                Status = FormVersionStatus.Draft
            };

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
            await _dbContext.SaveChangesAsync();
        }

        var position = await _dbContext.Questions
            .Where(question => question.FormVersionId == draft.Id)
            .CountAsync();

        var question = new Question
        {
            FormVersionId = draft.Id,
            Label = title,
            Type = request.Type,
            IsRequired = request.IsRequired,
            Position = position
        };

        for (var index = 0; index < options.Count; index++)
        {
            question.Options.Add(new QuestionOption
            {
                Label = options[index],
                Position = index
            });
        }

        form.UpdatedAt = DateTimeOffset.UtcNow;
        _dbContext.Questions.Add(question);

        await _dbContext.SaveChangesAsync();

        return Created(
            $"/api/v1/forms/{id}/questions/{question.Id}",
            new
            {
                question.Id,
                question.FormVersionId,
                question.Label,
                question.Type,
                question.IsRequired,
                question.Position,
                Options = question.Options
                    .OrderBy(option => option.Position)
                    .Select(option => new
                    {
                        option.Id,
                        option.Label,
                        option.Position
                    })
            });
    }


    [HttpGet("{id:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid id)
    {
        var ownerId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            return Unauthorized();
        }

        var form = await _dbContext.Forms
            .Where(form => form.Id == id && form.OwnerId == ownerId)
            .Select(form => new { form.Id })
            .FirstOrDefaultAsync();

        if (form is null)
        {
            return NotFound();
        }

        var draft = await _dbContext.FormVersions
            .Where(version =>
                version.FormId == id &&
                version.Status == FormVersionStatus.Draft)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new { version.Id })
            .FirstOrDefaultAsync();

        if (draft is null)
        {
            return Ok(Array.Empty<object>());
        }

        var questions = await _dbContext.Questions
            .Where(question => question.FormVersionId == draft.Id)
            .OrderBy(question => question.Position)
            .Select(question => new
            {
                question.Id,
                question.FormVersionId,
                question.Label,
                question.Type,
                question.IsRequired,
                question.Position,
                Options = question.Options
                    .OrderBy(option => option.Position)
                    .Select(option => new
                    {
                        option.Id,
                        option.Label,
                        option.Position
                    })
            })
            .ToListAsync();

        return Ok(questions);
    }

    
[HttpGet("{id:guid}/published-questions")]
public async Task<IActionResult> GetPublishedQuestions(Guid id)
{
    var ownerId = User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(ownerId))
    {
        return Unauthorized();
    }

    var formExists = await _dbContext.Forms
        .AnyAsync(form => form.Id == id && form.OwnerId == ownerId);

    if (!formExists)
    {
        return NotFound();
    }

    var publishedVersion = await _dbContext.FormVersions
        .Where(version =>
            version.FormId == id &&
            version.Status == FormVersionStatus.Published)
        .OrderByDescending(version => version.VersionNumber)
        .Select(version => new { version.Id })
        .FirstOrDefaultAsync();

    if (publishedVersion is null)
    {
        return Ok(Array.Empty<object>());
    }

    var questions = await _dbContext.Questions
        .Where(question => question.FormVersionId == publishedVersion.Id)
        .OrderBy(question => question.Position)
        .Select(question => new
        {
            question.Id,
            question.FormVersionId,
            question.Label,
            question.Type,
            question.IsRequired,
            question.Position,
            Options = question.Options
                .OrderBy(option => option.Position)
                .Select(option => new
                {
                    option.Id,
                    option.Label,
                    option.Position
                })
        })
        .ToListAsync();

    return Ok(questions);
}


    [HttpPut("{id:guid}/questions/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderQuestions(
        Guid id,
        [FromBody] ReorderQuestionsRequest request)
    {
        var ownerId = User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var form = await _dbContext.Forms
            .Include(f => f.Versions)
                .ThenInclude(v => v.Questions)
            .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == ownerId);

        if (form is null)
        {
            return NotFound();
        }

        var draft = form.Versions
            .Where(v => v.Status == FormVersionStatus.Draft)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (draft is null)
        {
            return BadRequest("This form has no draft version.");
        }

        var currentIds = draft.Questions.Select(q => q.Id).ToHashSet();
        var requestedIds = request.QuestionIds;

        if (requestedIds.Count != currentIds.Count ||
            requestedIds.Distinct().Count() != requestedIds.Count ||
            !requestedIds.ToHashSet().SetEquals(currentIds))
        {
            return BadRequest("The question order must contain every draft question exactly once.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        // Temporarily use negative positions to avoid conflicts with the unique index.
        for (var i = 0; i < requestedIds.Count; i++)
        {
            var question = draft.Questions.Single(q => q.Id == requestedIds[i]);
            question.Position = -1 - i;
        }

        await _dbContext.SaveChangesAsync();

        for (var i = 0; i < requestedIds.Count; i++)
        {
            var question = draft.Questions.Single(q => q.Id == requestedIds[i]);
            question.Position = i;
        }

        form.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    [HttpPut("{id:guid}/questions/{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuestion(
        Guid id,
        Guid questionId,
        [FromBody] UpdateQuestionRequest request)
    {
        var ownerId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            return Unauthorized();
        }

        var question = await _dbContext.Questions
            .Include(question => question.Options)
            .Include(question => question.FormVersion)
            .ThenInclude(version => version.Form)
            .FirstOrDefaultAsync(question =>
                question.Id == questionId &&
                question.FormVersion.FormId == id &&
                question.FormVersion.Form.OwnerId == ownerId);

        if (question is null)
        {
            return NotFound();
        }

        if (question.FormVersion.Status != FormVersionStatus.Draft)
        {
            return BadRequest(new
            {
                message = "Only draft questions can be edited."
            });
        }

        var label = request.Label.Trim();

        if (label.Length == 0)
        {
            return BadRequest(new
            {
                message = "Question text cannot be empty."
            });
        }

        if (!Enum.IsDefined(typeof(QuestionType), request.Type))
        {
            return BadRequest(new
            {
                message = "Invalid question type."
            });
        }

        var needsOptions = request.Type is QuestionType.SingleChoice
            or QuestionType.MultipleChoice
            or QuestionType.Dropdown;

        var options = request.Options
            .Select(option => option.Trim())
            .ToList();

        if (needsOptions &&
            (options.Count < 2 || options.Any(string.IsNullOrWhiteSpace)))
        {
            return BadRequest(new
            {
                message = "Choice questions require at least two non-empty options."
            });
        }

        if (!needsOptions && options.Count > 0)
        {
            return BadRequest(new
            {
                message = "Options are only allowed for choice questions."
            });
        }


        question.Label = label;
        question.Type = request.Type;
        question.IsRequired = request.IsRequired;

        // Explicitly mark the existing options for deletion.
        var existingOptions = question.Options.ToList();

        _dbContext.QuestionOptions.RemoveRange(existingOptions);
        question.Options.Clear();

        // Add the replacement options.
        for (var index = 0; index < options.Count; index++)
        {
            _dbContext.QuestionOptions.Add(new QuestionOption
            {
                QuestionId = question.Id,
                Label = options[index],
                Position = index
            });
        }


        question.FormVersion.Form.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            question.Id,
            question.FormVersionId,
            question.Label,
            question.Type,
            question.IsRequired,
            question.Position,
            Options = question.Options
                .OrderBy(option => option.Position)
                .Select(option => new
                {
                    option.Id,
                    option.Label,
                    option.Position
                })
        });
    }



    [HttpDelete("{id:guid}/questions/{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(
        Guid id,
        Guid questionId)
    {
        var ownerId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            return Unauthorized();
        }

        var question = await _dbContext.Questions
            .Include(question => question.Options)
            .Include(question => question.FormVersion)
            .ThenInclude(version => version.Form)
            .FirstOrDefaultAsync(question =>
                question.Id == questionId &&
                question.FormVersion.FormId == id &&
                question.FormVersion.Form.OwnerId == ownerId);

        if (question is null)
        {
            return NotFound();
        }

        if (question.FormVersion.Status != FormVersionStatus.Draft)
        {
            return BadRequest(new
            {
                message = "Only draft questions can be deleted."
            });
        }

        var form = question.FormVersion.Form;

        _dbContext.Questions.Remove(question);
        form.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
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
