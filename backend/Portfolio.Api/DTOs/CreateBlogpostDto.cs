using System.ComponentModel.DataAnnotations;

namespace Portfolio.Api.DTOs;

public class CreateBlogpostDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime PublicationDate { get; set; }
}
