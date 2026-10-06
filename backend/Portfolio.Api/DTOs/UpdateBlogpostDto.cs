namespace Portfolio.Api.DTOs;

public class UpdateBlogpostDto
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime PublicationDate { get; set; }
}
