using FormBuilder.Core.Enums;
using FormBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormBuilder.Api.DTOs.Forms;
using FormBuilder.Core.Entities;
using FormBuilder.Infrastructure.Services;

namespace FormBuilder.Api.Controllers;

[ApiController]
[Route("api/v1/forms")]
[Authorize]
public class FormQuestionsController : ControllerBase
{
    private readonly FormBuilderDbContext _dbContext;
    private readonly FormQuestionService _formQuestionService;

    public FormQuestionsController(
        FormBuilderDbContext dbContext,
        FormQuestionService formQuestionService)
    {
        _dbContext = dbContext;
        _formQuestionService = formQuestionService;
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

    var result = await _formQuestionService.GetQuestionsAsync(id, ownerId);

    if (!result.FormFound)
    {
        return NotFound();
    }

    return Ok(result.Questions);
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

    var result = await _formQuestionService.GetPublishedQuestionsAsync(
        id, ownerId);

    if (!result.FormFound)
    {
        return NotFound();
    }

    return Ok(result.Questions);
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

    var result = await _formQuestionService.CreateQuestionAsync(
        id,
        ownerId,
        request.Label,
        request.Type,
        request.IsRequired,
        request.Options);

    if (!result.FormFound)
    {
        return NotFound();
    }

    if (result.ErrorMessage is not null)
    {
        return BadRequest(new { message = result.ErrorMessage });
    }

    var question = result.Question!;

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
            Options = question.Options.Select(option => new
            {
                option.Id,
                option.Label,
                option.Position
            })
        });
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
        return Unauthorized();

    var result = await _formQuestionService.UpdateQuestionAsync(
        id,
        questionId,
        ownerId,
        request.Label,
        request.Type,
        request.IsRequired,
        request.Options);

    if (!result.QuestionFound)
        return NotFound();

    if (result.ErrorMessage is not null)
        return BadRequest(new { message = result.ErrorMessage });

    var question = result.Question!;

    return Ok(new
    {
        question.Id,
        question.FormVersionId,
        question.Label,
        question.Type,
        question.IsRequired,
        question.Position,
        Options = question.Options.Select(option => new
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
        return Unauthorized();

    var result = await _formQuestionService.DeleteQuestionAsync(
        id, questionId, ownerId);

    if (!result.QuestionFound)
        return NotFound();

    if (result.ErrorMessage is not null)
        return BadRequest(new { message = result.ErrorMessage });

    return NoContent();
}

    
[HttpPut("{id:guid}/questions/reorder")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ReorderQuestions(
    Guid id,
    [FromBody] ReorderQuestionsRequest request)
{
    var ownerId = User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(ownerId))
        return Unauthorized();

    var result = await _formQuestionService.ReorderQuestionsAsync(
        id, ownerId, request.QuestionIds);

    if (!result.FormFound)
        return NotFound();

    if (result.ErrorMessage is not null)
        return BadRequest(result.ErrorMessage);

    return NoContent();
}

}
