using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAnalysis.Service;

namespace SocialMediaAnalysis;

[ApiController]
[Route("api/[controller]")]
public class KeywordController : Controller
{
    private readonly IKeywordService _keywordService;

    public KeywordController(IKeywordService keywordService)
    {
        _keywordService = keywordService;
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> GetKeywords(int take, int skip)
    {
        try
        {
            var keywords = await _keywordService.GetPopularKeywords(take, skip);
            return Ok(keywords);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet, Authorize, Route("filter")]
    public async Task<IActionResult> GetKeywordsWithFilter(int take, int skip, string filter)
    {
        try
        {
            var keywords = await _keywordService.GetPopularKeywordsWithFilter(take, skip, filter);
            return Ok(keywords);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet, Authorize, Route("least")]
    public async Task<IActionResult> GetLeastPopularKeywords(int take, int skip)
    {
        try
        {
            var keywords = await _keywordService.GetLeastPopularKeywords(take, skip);
            return Ok(keywords);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet, Authorize, Route("least/filter")]
    public async Task<IActionResult> GetKeywoGetLeastPopularKeywordsWithFilterrdsWithFilter(int take, int skip, string filter)
    {
        try
        {
            var keywords = await _keywordService.GetLeastPopularKeywordsWithFilter(take, skip, filter);
            return Ok(keywords);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet, Route("statistics")]
    public async Task<IActionResult> GetKeywordStatistics(string hashTag)
    {
        try
        {
            var keywords = await _keywordService.GetStatistics(hashTag);
            return Ok(keywords);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet, Route("all")]
    public async Task<IActionResult> GetAllKeywords()
    {
        try
        {
            var keywords = await _keywordService.GetAllKeywords();
            return Ok(keywords);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
