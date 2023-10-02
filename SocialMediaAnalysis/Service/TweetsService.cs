using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SocialMediaAnalysis.Models;

namespace SocialMediaAnalysis.Service;

public interface ITweetService
{
    Task<List<Tweet>> GetTweets(string filter, int skip, int take, string hashTag, string sentiment);

    Task<long> GetTweetsCount(string filter, string hashTag, string sentiment);

    Task<long> GetTweetsCountByUser(string email);

    Task<List<Tweet>> GetTweetsByUser(string email, int skip, int take);
}

public class TweetsService : ITweetService
{
    private readonly IMongoCollection<Tweet> _tweet;

    public TweetsService(IMongoDatabase database)
    {
        _tweet = database.GetCollection<Tweet>(typeof(Tweet).Name);
    }

    public async Task<List<Tweet>> GetTweets(string filter, int skip, int take, string hashTag, string sentiment)
    {
        var tweetsList = _tweet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(hashTag))
        {
            tweetsList = tweetsList.Where(x => x.Text.Contains(hashTag));
        }
        if (!string.IsNullOrWhiteSpace(sentiment))
        {
            tweetsList = tweetsList.Where(x => x.Sentiment.ToLower() == sentiment.ToLower());
        }
        if (!string.IsNullOrWhiteSpace(filter))
        {
            tweetsList = tweetsList.Where(x => x.Text.Contains(filter) || x.Name.Contains(filter));
        }

        var result = await tweetsList.Skip(skip).Take(take).ToListAsync();

        return result;
    }

    public async Task<long> GetTweetsCount(string filter, string hashTag, string sentiment)
    {
        long resultCount = 0;
        var tweetsList = _tweet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(hashTag))
        {
            tweetsList = tweetsList.Where(x => x.Text.Contains(hashTag));
        }
        if (!string.IsNullOrWhiteSpace(sentiment))
        {
            tweetsList = tweetsList.Where(x => x.Sentiment.ToLower() == sentiment.ToLower());
        }
        if (!string.IsNullOrWhiteSpace(filter))
        {
            tweetsList = tweetsList.Where(x => x.Text.Contains(filter) || x.Name.Contains(filter));
        }
        resultCount = await tweetsList.CountAsync();
        return resultCount;
    }

    public async Task<List<Tweet>> GetTweetsByUser(string email, int skip, int take)
    {

        var result = await _tweet.AsQueryable().Where(x => x.UploaderEmail == email).Skip(skip).Take(take)
            .ToListAsync();

        return result;

    }

    public async Task<long> GetTweetsCountByUser(string email)
    {

        var result = await _tweet.CountDocumentsAsync(x => x.UploaderEmail == email);

        return result;

    }
}