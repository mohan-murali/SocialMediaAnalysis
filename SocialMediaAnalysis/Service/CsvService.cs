using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.ML;
using Microsoft.ML;
using MongoDB.Bson;
using MongoDB.Driver;
using SocialMediaAnalysis.Models;

namespace SocialMediaAnalysis.Service;

public interface ICsvService
{
    Task ReadCsvFile(IFormFile file, string email);
}

public class CsvService : ICsvService
{
    private readonly IMongoCollection<Tweet> _tweet;
    private readonly IMongoCollection<Keyword> _keyword;
    private readonly PredictionEngine<ModelInput, ModelOutput> _predictionEngine;


    public CsvService(IMongoDatabase database, PredictionEnginePool<ModelInput, ModelOutput> predictionEnginePool)
    {
        _tweet = database.GetCollection<Tweet>(typeof(Tweet).Name);
        _keyword = database.GetCollection<Keyword>(typeof(Keyword).Name);
        _predictionEngine = predictionEnginePool.GetPredictionEngine("SentimentAnalysisModel");
    }

    public async Task ReadCsvFile(IFormFile file, string email)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };
        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, config);

        var data = csv.GetRecordsAsync<TweetCsv>();

        var tweets = new List<Tweet>();

        // Regular expression to match hashtags
        var regex = new Regex(@"#\w+");

        var existingKeywords = await _keyword.AsQueryable().ToListAsync();
        var newKeywords = new List<Keyword>();
        var keywordsToUpdate = new List<Keyword>();

        var hashtagCounts = new Dictionary<string, int>();
        var totalLikes = new Dictionary<string, int>();
        var totalRetweets = new Dictionary<string, int>();
        var totalNegativeCount = new Dictionary<string, int>();
        var totalPositiveCount = new Dictionary<string, int>();

        await foreach (var tweet in data)
        {
            try
            {
                var prediction = _predictionEngine.Predict(new ModelInput
                {
                    SentimentText = tweet.Tweet
                });

                var isValidRetweet = Int32.TryParse(tweet.Retweets ?? "", out int retweets);
                var isValidLike = Int32.TryParse(tweet.Likes ?? "", out int likes);

                var newTweet = new Tweet(tweet.Name, tweet.Tweet, tweet.Location, tweet.Created,
                    prediction.Sentiment ? "positive" : "negative", prediction.Score, prediction.Probability,
                    tweet.Retweets,
                    tweet.Likes, email)
                {
                    Id = ObjectId.GenerateNewId().ToString()
                };

                var matches = regex.Matches(tweet.Tweet);

                if (isValidLike && isValidRetweet)
                {
                    foreach (Match match in matches.Cast<Match>())
                    {
                        var hashtag = match.Value.ToLower();

                        if (hashtagCounts.ContainsKey(hashtag))
                        {
                            hashtagCounts[hashtag]++;
                        }
                        else
                        {
                            hashtagCounts[hashtag] = 1;
                        }

                        if (totalLikes.ContainsKey(hashtag))
                        {
                            totalLikes[hashtag] += likes;
                        }
                        else
                        {
                            totalLikes[hashtag] = likes;
                        }

                        if (totalRetweets.ContainsKey(hashtag))
                        {
                            totalRetweets[hashtag] += retweets;
                        }
                        else
                        {
                            totalRetweets[hashtag] = retweets;
                        }

                        if (prediction.Sentiment)
                        {
                            if (totalPositiveCount.ContainsKey(hashtag))
                            {
                                totalPositiveCount[hashtag]++;
                            }
                            else
                            {
                                totalPositiveCount[hashtag] = 1;
                            }
                        }
                        else
                        {

                            if (totalNegativeCount.ContainsKey(hashtag))
                            {
                                totalNegativeCount[hashtag]++;
                            }
                            else
                            {
                                totalNegativeCount[hashtag] = 1;
                            }
                        }
                    }
                }

                tweets.Add(newTweet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(tweet?.Retweets, tweet?.Likes, ex.Message);
                throw;
            }
        }

        foreach (var hashtag in hashtagCounts.Keys)
        {
            var existing = existingKeywords.FirstOrDefault(x => x.HashTag.ToLower() == hashtag);
            if (existing != null)
            {
                var existingKeywordUpdate = new Keyword(existing.HashTag,
                    existing.Count + hashtagCounts[hashtag],
                    totalNegativeCount.ContainsKey(hashtag) ?
                        existing.NegativeCount + totalNegativeCount[hashtag]
                        : existing.NegativeCount,
                    totalPositiveCount.ContainsKey(hashtag) ?
                        existing.PositiveCount + totalPositiveCount[hashtag]
                        : existing.PositiveCount,
                    totalRetweets.ContainsKey(hashtag) ?
                        existing.Retweets + totalRetweets[hashtag]
                        : existing.Retweets,
                    totalLikes.ContainsKey(hashtag) ?
                        existing.Likes + totalLikes[hashtag]
                        : existing.Likes)
                {
                    Id = existing.Id
                };

                keywordsToUpdate.Add(existingKeywordUpdate);
            }
            else
            {
                var newKeyword = new Keyword(hashtag,
                    hashtagCounts[hashtag],
                    totalNegativeCount.ContainsKey(hashtag) ? totalNegativeCount[hashtag] : 0,
                    totalPositiveCount.ContainsKey(hashtag) ? totalPositiveCount[hashtag] : 0,
                    totalRetweets.ContainsKey(hashtag) ? totalRetweets[hashtag] : 0,
                    totalLikes.ContainsKey(hashtag) ? totalLikes[hashtag] : 0);

                newKeywords.Add(newKeyword);
            }
        }

        var tasks = new List<Task>();
        if (keywordsToUpdate.Count > 0)
        {
            tasks.Add(UpdateManyAsync(keywordsToUpdate));
        }

        if (newKeywords.Count > 0)
        {
            tasks.Add(_keyword.InsertManyAsync(newKeywords));
        }
        tasks.Add(_tweet.InsertManyAsync(tweets));

        // var allTask = Task.WhenAll(up, ne, tw);
        var allTask = Task.WhenAll(tasks);

        await allTask;
    }

    public async Task UpdateManyAsync(IEnumerable<Keyword> entities)
    {
        var updates = new List<WriteModel<Keyword>>();
        var filterBuilder = Builders<Keyword>.Filter;

        foreach (var doc in entities)
        {
            var filter = filterBuilder.Where(x => x.Id == doc.Id);
            updates.Add(new ReplaceOneModel<Keyword>(filter, doc));
        }

        await _keyword.BulkWriteAsync(updates);
    }
}