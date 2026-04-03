using System.ComponentModel.DataAnnotations;

namespace atlas_test.Application.DTOs;

public sealed class ChatRequestDto
{
    [Required]
    [MinLength(3)]
    public string Question { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; set; }
}

