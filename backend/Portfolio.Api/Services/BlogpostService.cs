using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.DTOs;
using Portfolio.Api.Models;

namespace Portfolio.Api.Services;

public class BlogpostService
{
    private readonly PortfolioDbContext _dbContext;

    public BlogpostService(PortfolioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<BlogpostDto>> GetAllAsync()
    {
        var blogposts = await _dbContext.Blogposts.AsNoTracking()
            .OrderBy(blogpost => blogpost.Id).ToListAsync();

        return blogposts.Select(ToDto).ToList();
    }

    public async Task<BlogpostDto?> GetByIdAsync(int id)
    {
        var blogpost = await _dbContext.Blogposts.AsNoTracking()
            .FirstOrDefaultAsync(blogpost => blogpost.Id == id);

        return blogpost is null ? null : ToDto(blogpost);
    }

    public async Task<BlogpostDto> CreateAsync(CreateBlogpostDto dto)
    {
        var blogpost = new Blogpost
        {
            Title = dto.Title,
            Category = dto.Category,
            Summary = dto.Summary,
            Content = dto.Content,
            PublicationDate = dto.PublicationDate
        };

        _dbContext.Blogposts.Add(blogpost);
        await _dbContext.SaveChangesAsync();

        return ToDto(blogpost);
    }

    public async Task<bool> UpdateAsync(int id, UpdateBlogpostDto dto)
    {
        var blogpost = await _dbContext.Blogposts.FindAsync(id);
        if (blogpost is null)
        {
            return false;
        }

        blogpost.Title = dto.Title;
        blogpost.Category = dto.Category;
        blogpost.Summary = dto.Summary;
        blogpost.Content = dto.Content;
        blogpost.PublicationDate = dto.PublicationDate;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var blogpost = await _dbContext.Blogposts.FindAsync(id);
        if (blogpost is null)
        {
            return false;
        }

        _dbContext.Blogposts.Remove(blogpost);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static BlogpostDto ToDto(Blogpost blogpost)
    {
        return new BlogpostDto
        {
            Id = blogpost.Id,
            Title = blogpost.Title,
            Category = blogpost.Category,
            Summary = blogpost.Summary,
            Content = blogpost.Content,
            PublicationDate = blogpost.PublicationDate
        };
    }
}
