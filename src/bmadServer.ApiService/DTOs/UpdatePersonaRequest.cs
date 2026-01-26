using bmadServer.ApiService.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace bmadServer.ApiService.DTOs;

/// <summary>
/// Request to update user persona preferences
/// </summary>
public class UpdatePersonaRequest
{
    /// <summary>
    /// Persona type preference
    /// </summary>
    [Required]
    public PersonaType PersonaType { get; set; }
}
