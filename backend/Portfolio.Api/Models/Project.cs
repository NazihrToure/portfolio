namespace Portfolio.Api.Models;

public class Project
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Technologies { get; set; } = new();
    public string? GitHubUrl { get; set; }
    public string? LiveUrl { get; set; }
    public DateTime Date { get; set; }
}
