using FormBuilder.Core.Entities;
using FormBuilder.Core.Enums;
using FormBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Infrastructure.Services;

public class FormQuestionService
{
    private readonly FormBuilderDbContext _dbContext;

    public FormQuestionService(FormBuilderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    
public async Task<CreateQuestionResult> CreateQuestionAsync(
    Guid formId,
    string ownerId,
    string label,
    QuestionType type,
    bool isRequired,
    List<string> requestedOptions)
{
    var form = await _dbContext.Forms
        .Include(form => form.Versions)
            .ThenInclude(version => version.Questions)
                .ThenInclude(question => question.Options)
        .FirstOrDefaultAsync(
            form => form.Id == formId && form.OwnerId == ownerId);

    if (form is null)
    {
        return new CreateQuestionResult(
            FormFound: false,
            ErrorMessage: null,
            Question: null);
    }

    label = label.Trim();

    if (label.Length == 0)
    {
        return new CreateQuestionResult(
            FormFound: true,
            ErrorMessage: "Question text cannot be empty.",
            Question: null);
    }

    if (!Enum.IsDefined(typeof(QuestionType), type))
    {
        return new CreateQuestionResult(
            FormFound: true,
            ErrorMessage: "Invalid question type.",
            Question: null);
    }

    var needsOptions = type is QuestionType.SingleChoice
        or QuestionType.MultipleChoice
        or QuestionType.Dropdown;

    var options = requestedOptions
        .Select(option => option.Trim())
        .ToList();

    if (needsOptions &&
        (options.Count < 2 || options.Any(string.IsNullOrWhiteSpace)))
    {
        return new CreateQuestionResult(
            FormFound: true,
            ErrorMessage : "Choice questions require at least two non-empty options.",
            Question: null);
    }

    if (!needsOptions && options.Count > 0)
    {
        return new CreateQuestionResult(
            FormFound: true,
            ErrorMessage : "Options are only allowed for choice questions.",
            Question: null);
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
        Label = label,
        Type = type,
        IsRequired = isRequired,
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

    var response = new QuestionResponse(
        question.Id,
        question.FormVersionId,
        question.Label,
        question.Type,
        question.IsRequired,
        question.Position,
        question.Options
            .OrderBy(option => option.Position)
            .Select(option => new QuestionOptionResponse(
                option.Id,
                option.Label,
                option.Position))
            .ToList());

    return new CreateQuestionResult(
        FormFound: true,
        ErrorMessage: null,
        Question: response);
}

    
public async Task<UpdateQuestionResult> UpdateQuestionAsync(
    Guid formId,
    Guid questionId,
    string ownerId,
    string label,
    QuestionType type,
    bool isRequired,
    List<string> requestedOptions)
{
    var question = await _dbContext.Questions
        .Include(question => question.Options)
        .Include(question => question.FormVersion)
        .ThenInclude(version => version.Form)
        .FirstOrDefaultAsync(question =>
            question.Id == questionId &&
            question.FormVersion.FormId == formId &&
            question.FormVersion.Form.OwnerId == ownerId);

    if (question is null)
        return new UpdateQuestionResult(false, "Question not found.", null);

    if (question.FormVersion.Status != FormVersionStatus.Draft)
        return new UpdateQuestionResult(
            true, "Only draft questions can be edited.", null);

    var trimmedLabel = label.Trim();

    if (trimmedLabel.Length == 0)
        return new UpdateQuestionResult(
            true, "Question text cannot be empty.", null);

    if (!Enum.IsDefined(typeof(QuestionType), type))
        return new UpdateQuestionResult(
            true, "Invalid question type.", null);

    var needsOptions = type is QuestionType.SingleChoice
        or QuestionType.MultipleChoice
        or QuestionType.Dropdown;

    var options = (requestedOptions ?? [])
        .Select(option => option.Trim())
        .ToList();

    if (needsOptions &&
        (options.Count < 2 || options.Any(string.IsNullOrWhiteSpace)))
    {
        return new UpdateQuestionResult(
            true,
            "Choice questions require at least two non-empty options.",
            null);
    }

    if (!needsOptions && options.Count > 0)
    {
        return new UpdateQuestionResult(
            true,
            "Options are only allowed for choice questions.",
            null);
    }

    question.Label = trimmedLabel;
    question.Type = type;
    question.IsRequired = isRequired;

    var existingOptions = question.Options.ToList();

_dbContext.QuestionOptions.RemoveRange(existingOptions);

foreach (var option in existingOptions)
{
    question.Options.Remove(option);
}

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

    var response = new QuestionResponse(
        question.Id,
        question.FormVersionId,
        question.Label,
        question.Type,
        question.IsRequired,
        question.Position,
        question.Options
            .OrderBy(option => option.Position)
            .Select(option => new QuestionOptionResponse(
                option.Id, option.Label, option.Position))
            .ToList());

    return new UpdateQuestionResult(true, null, response);
}

    
public async Task<DeleteQuestionResult> DeleteQuestionAsync(
    Guid formId,
    Guid questionId,
    string ownerId)
{
    var question = await _dbContext.Questions
        .Include(question => question.Options)
        .Include(question => question.FormVersion)
        .ThenInclude(version => version.Form)
        .FirstOrDefaultAsync(question =>
            question.Id == questionId &&
            question.FormVersion.FormId == formId &&
            question.FormVersion.Form.OwnerId == ownerId);

    if (question is null)
        return new DeleteQuestionResult(false, null);

    if (question.FormVersion.Status != FormVersionStatus.Draft)
    {
        return new DeleteQuestionResult(
            true,
            "Only draft questions can be deleted.");
    }

    var form = question.FormVersion.Form;

    _dbContext.Questions.Remove(question);
    form.UpdatedAt = DateTimeOffset.UtcNow;

    await _dbContext.SaveChangesAsync();

    return new DeleteQuestionResult(true, null);
}


public async Task<ReorderQuestionsResult> ReorderQuestionsAsync(
    Guid formId,
    string ownerId,
    List<Guid> requestedIds)
{
    var form = await _dbContext.Forms
        .Include(form => form.Versions)
            .ThenInclude(version => version.Questions)
        .FirstOrDefaultAsync(form =>
            form.Id == formId && form.OwnerId == ownerId);

    if (form is null)
        return new ReorderQuestionsResult(
            false, "Form not found.");

    var draft = form.Versions
        .Where(version => version.Status == FormVersionStatus.Draft)
        .OrderByDescending(version => version.VersionNumber)
        .FirstOrDefault();

    if (draft is null)
        return new ReorderQuestionsResult(
            true, "This form has no draft version.");

    var currentIds = draft.Questions.Select(question => question.Id).ToHashSet();

    if (requestedIds is null ||
        requestedIds.Count != currentIds.Count ||
        requestedIds.Distinct().Count() != requestedIds.Count ||
        !requestedIds.ToHashSet().SetEquals(currentIds))
    {
        return new ReorderQuestionsResult(
            true,
            "The question order must contain every draft question exactly once.");
    }

    await using var transaction =
        await _dbContext.Database.BeginTransactionAsync();

    // Temporarily use negative positions to avoid unique-index conflicts.
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

    return new ReorderQuestionsResult(true, null);
}

    public async Task<GetQuestionsResult> GetQuestionsAsync(
        Guid formId,
        string ownerId)
    {
        var formExists = await _dbContext.Forms
            .AnyAsync(form =>
                form.Id == formId &&
                form.OwnerId == ownerId);

        if (!formExists)
        {
            return new GetQuestionsResult(
                FormFound: false,
                Questions: []);
        }

        var draftId = await _dbContext.FormVersions
            .Where(version =>
                version.FormId == formId &&
                version.Status == FormVersionStatus.Draft)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => (Guid?)version.Id)
            .FirstOrDefaultAsync();

        if (draftId is null)
        {
            return new GetQuestionsResult(
                FormFound: true,
                Questions: []);
        }

        var questions = await _dbContext.Questions
            .Where(question => question.FormVersionId == draftId.Value)
            .OrderBy(question => question.Position)
            .Select(question => new QuestionResponse(
                question.Id,
                question.FormVersionId,
                question.Label,
                question.Type,
                question.IsRequired,
                question.Position,
                question.Options
                    .OrderBy(option => option.Position)
                    .Select(option => new QuestionOptionResponse(
                        option.Id,
                        option.Label,
                        option.Position))
                    .ToList()))
            .ToListAsync();

        return new GetQuestionsResult(
            FormFound: true,
            Questions: questions);
    }

    
public async Task<GetQuestionsResult> GetPublishedQuestionsAsync(
    Guid formId,
    string ownerId)
{
    var formExists = await _dbContext.Forms
        .AnyAsync(form =>
            form.Id == formId &&
            form.OwnerId == ownerId);

    if (!formExists)
    {
        return new GetQuestionsResult(
            FormFound: false,
            Questions: []);
    }

    var publishedVersionId = await _dbContext.FormVersions
        .Where(version =>
            version.FormId == formId &&
            version.Status == FormVersionStatus.Published)
        .OrderByDescending(version => version.VersionNumber)
        .Select(version => (Guid?)version.Id)
        .FirstOrDefaultAsync();

    if (publishedVersionId is null)
    {
        return new GetQuestionsResult(
            FormFound: true,
            Questions: []);
    }

    var questions = await _dbContext.Questions
        .Where(question =>
            question.FormVersionId == publishedVersionId.Value)
        .OrderBy(question => question.Position)
        .Select(question => new QuestionResponse(
            question.Id,
            question.FormVersionId,
            question.Label,
            question.Type,
            question.IsRequired,
            question.Position,
            question.Options
                .OrderBy(option => option.Position)
                .Select(option => new QuestionOptionResponse(
                    option.Id,
                    option.Label,
                    option.Position))
                .ToList()))
        .ToListAsync();

    return new GetQuestionsResult(
        FormFound: true,
        Questions: questions);
}


}


public sealed record GetQuestionsResult(
    bool FormFound,
    IReadOnlyList<QuestionResponse> Questions);

public sealed record QuestionResponse(
    Guid Id,
    Guid FormVersionId,
    string Label,
    QuestionType Type,
    bool IsRequired,
    int Position,
    IReadOnlyList<QuestionOptionResponse> Options);

public sealed record QuestionOptionResponse(
    Guid Id,
    string Label,
    int Position);

public sealed record CreateQuestionResult(
    bool FormFound,
    string? ErrorMessage,
    QuestionResponse? Question);

public sealed record UpdateQuestionResult(
    bool QuestionFound,
    string? ErrorMessage,
    QuestionResponse? Question);

    public sealed record DeleteQuestionResult(
    bool QuestionFound,
    string? ErrorMessage);

    public sealed record ReorderQuestionsResult(
    bool FormFound,
    string? ErrorMessage);