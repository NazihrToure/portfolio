using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.DTOs;
using Portfolio.Api.Services;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/blogposts")]
public class BlogpostsController : ControllerBase
{
    private readonly BlogpostService _blogpostService;

    public BlogpostsController(BlogpostService blogpostService)
    {
        _blogpostService = blogpostService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BlogpostDto>>> GetAll()
    {
        return Ok(await _blogpostService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<BlogpostDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogpostDto>> GetById(int id)
    {
        var blogpost = await _blogpostService.GetByIdAsync(id);
        if (blogpost is null)
        {
            return NotFound();
        }

        return Ok(blogpost);
    }

    [HttpPost]
    [ProducesResponseType<BlogpostDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BlogpostDto>> Create(CreateBlogpostDto dto)
    {
        var blogpost = await _blogpostService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = blogpost.Id }, blogpost);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateBlogpostDto dto)
    {
        if (!await _blogpostService.UpdateAsync(id, dto))
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _blogpostService.DeleteAsync(id))
        {
            return NotFound();
        }

        return NoContent();
    }
}
