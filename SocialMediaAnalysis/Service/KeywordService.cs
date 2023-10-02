using System.Text.RegularExpressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SocialMediaAnalysis.Models;
using StopWord;

namespace SocialMediaAnalysis.Service;

public interface IKeywordService
{
    Task<List<Keyword>> GetPopularKeywords(int take, int skip);
    Task<List<Keyword>> GetPopularKeywordsWithFilter(int take, int skip, string filter);
    Task<List<Keyword>> GetLeastPopularKeywords(int take, int skip);
    Task<List<Keyword>> GetLeastPopularKeywordsWithFilter(int take, int skip, string filter);
    Task<TweetStatistics> GetStatistics(string hashTag);
    Task<List<Keyword>> GetAllKeywords();
}
public class KeywordService : IKeywordService
{
    private readonly IMongoCollection<Keyword> _keyword;
    private readonly IMongoCollection<Tweet> _tweet;

    public KeywordService(IMongoDatabase database)
    {
        _keyword = database.GetCollection<Keyword>(typeof(Keyword).Name);
        _tweet = database.GetCollection<Tweet>(typeof(Tweet).Name);
    }

    public async Task<List<Keyword>> GetPopularKeywords(int take, int skip)
    {
        var result = await _keyword.AsQueryable()
            .OrderByDescending(x => x.Count).Skip(skip)
            .Take(take).ToListAsync();

        return result;
    }

    public async Task<List<Keyword>> GetPopularKeywordsWithFilter(int take, int skip, string filter)
    {
        var result = await _keyword.AsQueryable()
            .Where(x => x.HashTag.Contains(filter))
            .OrderByDescending(x => x.Count).Skip(skip)
            .Take(take).ToListAsync();

        return result;
    }

    public async Task<List<Keyword>> GetLeastPopularKeywords(int take, int skip)
    {
        var result = await _keyword.AsQueryable()
            .OrderBy(x => x.Count).Skip(skip)
            .Take(take).ToListAsync();

        return result;
    }

    public async Task<List<Keyword>> GetLeastPopularKeywordsWithFilter(int take, int skip, string filter)
    {
        var result = await _keyword.AsQueryable()
            .Where(x => x.HashTag.Contains(filter))
            .OrderBy(x => x.Count).Skip(skip)
            .Take(take).ToListAsync();

        return result;
    }


    public async Task<TweetStatistics> GetStatistics(string hashTag)
    {
        hashTag = hashTag.Trim().ToLower();
        var startsWithHashTag = hashTag.StartsWith("#");
        var hashTagToQuery = startsWithHashTag ? $"{hashTag}" : $"#{hashTag}";
        var existingHashtag = await _keyword.AsQueryable().FirstOrDefaultAsync(x => x.HashTag == hashTagToQuery);
        List<string> similarHashTags = new();
        var statisticsData = new List<Statistics>();
        List<string> mostUsedPositiveWords = new();
        List<string> mostUsedNegativeWords = new();
        Dictionary<string, int> positiveWordCounts = new();
        Dictionary<string, int> negativeWordCounts = new();
        List<string> Tweets = new();

        if (existingHashtag != null)
        {
            var tweetsTask = _tweet.AsQueryable().Where(tweet => tweet.Text.Contains(startsWithHashTag ? $"{hashTag} " : $"#{hashTag} ")).ToListAsync();
            var similarHashTagsTask = _keyword.AsQueryable().Where(x => x.HashTag != hashTag && x.HashTag.Contains(startsWithHashTag ? $"{hashTag.TrimStart('#', ' ')}" : hashTag)).Select(x => x.HashTag).Take(5).ToListAsync();
            var tasks = new List<Task>
            {
                tweetsTask,similarHashTagsTask

            };

            var response = Task.WhenAll(tasks);

            await response;


            Dictionary<DateOnly, int> positiveCount = new();
            Dictionary<DateOnly, int> negativeCount = new();

            if (response.Status == TaskStatus.RanToCompletion)
            {
                var tweets = tweetsTask.Result;
                similarHashTags = similarHashTagsTask.Result;
                foreach (var tweet in tweets)
                {
                    var isValidDate = DateTime.TryParse(tweet.Created, out var dateTime);
                    Tweets.Add(tweet.Text);
                    if (isValidDate)
                    {
                        var date = DateOnly.FromDateTime(dateTime);
                        if (positiveCount.ContainsKey(date))
                        {
                            positiveCount[date] = tweet.Sentiment == "positive" ? positiveCount[date] + 1 : positiveCount[date];
                            negativeCount[date] = tweet.Sentiment == "positive" ? negativeCount[date] : negativeCount[date] + 1;
                        }
                        else
                        {
                            positiveCount[date] = tweet.Sentiment == "positive" ? 1 : 0;
                            negativeCount[date] = tweet.Sentiment == "positive" ? 0 : 1;
                        }
                    }

                    var stopWords = StopWords.GetStopWords("en");
                    var text = Regex.Replace(tweet.Text, @"[^a-zA-Z0-9#\s]", "");
                    var uniqueWords = text.RemoveStopWords("en").Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(x => !stopWords.Contains(x));

                    foreach (var word in uniqueWords)
                    {
                        if (word.ToLower() != hashTag && !word.ToLower().StartsWith("#") && !stopWords.Contains(word.ToLower()))
                        {
                            if (tweet.Sentiment == "positive")
                            {
                                if (positiveWordCounts.ContainsKey(word.ToLower()))
                                {
                                    positiveWordCounts[word.ToLower()]++;
                                }
                                else
                                {
                                    positiveWordCounts[word.ToLower()] = 1;
                                }
                            }
                            else
                            {
                                if (negativeWordCounts.ContainsKey(word.ToLower()))
                                {
                                    negativeWordCounts[word.ToLower()]++;
                                }
                                else
                                {
                                    negativeWordCounts[word.ToLower()] = 1;
                                }
                            }
                        }
                    }
                }
            }

            mostUsedPositiveWords = positiveWordCounts.OrderByDescending(x => x.Value).Take(5).Select(x => x.Key).ToList();
            mostUsedNegativeWords = negativeWordCounts.OrderByDescending(x => x.Value).Take(5).Select(x => x.Key).ToList();

            foreach (var day in positiveCount)
            {
                statisticsData.Add(new Statistics(hashTag, positiveCount[day.Key] + negativeCount[day.Key], positiveCount[day.Key], negativeCount[day.Key], day.Key.ToShortDateString()));
            }
        }
        else
        {
            similarHashTags = await _keyword.AsQueryable().Where(x => x.HashTag.Contains(startsWithHashTag ? $"{hashTag.TrimStart('#', ' ')}" : hashTag)).Select(x => x.HashTag).Take(5).ToListAsync();
        }

        var positiveCountsAndWords = positiveWordCounts.OrderByDescending(x => x.Value).Take(50).Select(x => new Word(x.Key, x.Value));

        var negativeCountAndWords = negativeWordCounts.OrderByDescending(x => x.Value).Take(50).Select(x => new Word(x.Key, x.Value));

        var result = new TweetStatistics(statisticsData, hashTag, mostUsedPositiveWords, mostUsedNegativeWords, similarHashTags, existingHashtag, Tweets.Take(50), positiveCountsAndWords, negativeCountAndWords);

        return result;
    }

    public async Task<List<Keyword>> GetAllKeywords()
    {
        var result = await _keyword.AsQueryable().ToListAsync();

        return result;
    }
}
