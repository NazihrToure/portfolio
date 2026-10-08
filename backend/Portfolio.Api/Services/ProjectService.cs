using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.DTOs;
using Portfolio.Api.Models;

namespace Portfolio.Api.Services;

public class ProjectService
{
    private readonly PortfolioDbContext _dbContext;

    public ProjectService(PortfolioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ProjectDto>> GetAllAsync()
    {
        var projects = await _dbContext.Projects.AsNoTracking()
            .OrderBy(project => project.Id).ToListAsync();

        return projects.Select(ToDto).ToList();
    }

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        var project = await _dbContext.Projects.AsNoTracking()
            .FirstOrDefaultAsync(project => project.Id == id);

        return project is null ? null : ToDto(project);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
    {
        var project = new Project
        {
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            Technologies = new List<string>(dto.Technologies),
            GitHubUrl = dto.GitHubUrl,
            LiveUrl = dto.LiveUrl,
            Date = dto.Date
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();

        return ToDto(project);
    }

    public async Task<bool> UpdateAsync(int id, UpdateProjectDto dto)
    {
        var project = await _dbContext.Projects.FindAsync(id);
        if (project is null)
        {
            return false;
        }

        project.Title = dto.Title;
        project.Description = dto.Description;
        project.Category = dto.Category;
        project.Technologies = new List<string>(dto.Technologies);
        project.GitHubUrl = dto.GitHubUrl;
        project.LiveUrl = dto.LiveUrl;
        project.Date = dto.Date;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await _dbContext.Projects.FindAsync(id);
        if (project is null)
        {
            return false;
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static ProjectDto ToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            Category = project.Category,
            Technologies = new List<string>(project.Technologies),
            GitHubUrl = project.GitHubUrl,
            LiveUrl = project.LiveUrl,
            Date = project.Date
        };
    }
}
