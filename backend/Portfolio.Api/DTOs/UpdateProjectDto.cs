using System.ComponentModel.DataAnnotations;

namespace Portfolio.Api.DTOs;

public class UpdateProjectDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    public List<string> Technologies { get; set; } = new();

    [Url]
    public string? GitHubUrl { get; set; }

    [Url]
    public string? LiveUrl { get; set; }
    public DateTime Date { get; set; }
}
