using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SocialMediaAnalysis.Models;
using SocialMediaAnalysis.Service;

namespace SocialMediaAnalysis;

[ApiController]
[Route("api/[controller]")]
public class TweetsController : Controller
{
    private readonly ITweetService _tweetsService;

    public TweetsController(ITweetService tweetService)
    {
        _tweetsService = tweetService;
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> GetTweets([FromQuery] int skip, [FromQuery] int take, [FromBody] TweetFacet facets)
    {
        try
        {
            var tweetsCount = await _tweetsService.GetTweetsCount("", facets.HashTag ?? "", facets.Sentiment ?? "");
            var result = await _tweetsService.GetTweets("", skip, take, facets.HashTag ?? "", facets.Sentiment ?? "");
            var tweetsResult = new TweetResult(result, tweetsCount);
            return Ok(tweetsResult);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost, Authorize]
    [Route("Filter")]
    public async Task<IActionResult> GetFilteredTweets([FromQuery] int skip, [FromQuery] int take, [FromQuery] string filter, [FromBody] TweetFacet facets)
    {
        try
        {
            var tweetsCount = await _tweetsService.GetTweetsCount(filter, facets.HashTag ?? "", facets.Sentiment ?? "");
            var result = await _tweetsService.GetTweets(filter, skip, take, facets.HashTag ?? "", facets.Sentiment ?? "");
            var tweetsResult = new TweetResult(result, tweetsCount);
            return Ok(tweetsResult);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet, Authorize]
    [Route("User")]
    public async Task<IActionResult> GetTweetsByUser(int skip, int take)
    {
        try
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var email = claimsIdentity.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("could not get email from JWT");
            var tweetsCount = await _tweetsService.GetTweetsCountByUser(email);
            var result = await _tweetsService.GetTweetsByUser(email, skip, take);
            var tweetsResult = new TweetResult(result, tweetsCount);
            return Ok(tweetsResult);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}