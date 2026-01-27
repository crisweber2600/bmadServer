using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bmadServer.ApiService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TranslationsController : ControllerBase
{
    private readonly ITranslationService _translationService;
    private readonly ILogger<TranslationsController> _logger;

    public TranslationsController(
        ITranslationService translationService,
        ILogger<TranslationsController> logger)
    {
        _translationService = translationService;
        _logger = logger;
    }

    [HttpGet("mappings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<TranslationMappingResponse>>> GetMappings()
    {
        var mappings = await _translationService.GetTranslationMappingsAsync();
        var response = mappings.Select(m => new TranslationMappingResponse
        {
            Id = m.Id,
            TechnicalTerm = m.TechnicalTerm,
            BusinessTerm = m.BusinessTerm,
            Context = m.Context,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        });

        return Ok(response);
    }

    [HttpPost("mappings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TranslationMappingResponse>> CreateMapping(
        [FromBody] TranslationMappingRequest request)
    {
        try
        {
            var mapping = await _translationService.AddTranslationMappingAsync(
                request.TechnicalTerm,
                request.BusinessTerm,
                request.Context
            );

            var response = new TranslationMappingResponse
            {
                Id = mapping.Id,
                TechnicalTerm = mapping.TechnicalTerm,
                BusinessTerm = mapping.BusinessTerm,
                Context = mapping.Context,
                IsActive = mapping.IsActive,
                CreatedAt = mapping.CreatedAt,
                UpdatedAt = mapping.UpdatedAt
            };

            _logger.LogInformation("Created translation mapping {Id}", mapping.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating translation mapping");
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to create translation mapping",
                statusCode: 500
            );
        }
    }

    [HttpPut("mappings/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TranslationMappingResponse>> UpdateMapping(
        Guid id,
        [FromBody] TranslationMappingRequest request)
    {
        try
        {
            var mapping = await _translationService.UpdateTranslationMappingAsync(
                id,
                request.TechnicalTerm,
                request.BusinessTerm,
                request.Context
            );

            var response = new TranslationMappingResponse
            {
                Id = mapping.Id,
                TechnicalTerm = mapping.TechnicalTerm,
                BusinessTerm = mapping.BusinessTerm,
                Context = mapping.Context,
                IsActive = mapping.IsActive,
                CreatedAt = mapping.CreatedAt,
                UpdatedAt = mapping.UpdatedAt
            };

            _logger.LogInformation("Updated translation mapping {Id}", id);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Translation mapping {Id} not found", id);
            return Problem(
                title: "Not Found",
                detail: ex.Message,
                statusCode: 404
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating translation mapping {Id}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to update translation mapping",
                statusCode: 500
            );
        }
    }

    [HttpDelete("mappings/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteMapping(Guid id)
    {
        try
        {
            var result = await _translationService.DeleteTranslationMappingAsync(id);
            if (!result)
            {
                return Problem(
                    title: "Not Found",
                    detail: $"Translation mapping with ID {id} not found",
                    statusCode: 404
                );
            }

            _logger.LogInformation("Deleted translation mapping {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting translation mapping {Id}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to delete translation mapping",
                statusCode: 500
            );
        }
    }
}
