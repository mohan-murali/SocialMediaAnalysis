using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SocialMediaAnalysis.Models;

public class Keyword
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Id { get; set; }
    public string HashTag { get; set; }
    public int Count { get; set; }
    public int NegativeCount { get; set; }
    public int PositiveCount { get; set; }
    public int Retweets { get; set; }
    public int Likes { get; set; }

    public Keyword(string hashTag, int count, int negativeCount, int positiveCount, int retweets, int likes)
    {
        Count = count;
        NegativeCount = negativeCount;
        PositiveCount = positiveCount;
        Retweets = retweets;
        Likes = likes;
        HashTag = hashTag;
    }
}

public record Statistics(string HashTag, int Count, int PositiveCount, int NegativeCount, string Date);

public record Word(string Text, int Value);

public record TweetStatistics(IEnumerable<Statistics> Statistics, string HashTag, IEnumerable<string> MostUsedPositiveKeyWords, IEnumerable<string> MostUsedNegativeKeywords, IEnumerable<string> SimilarHashtags, Keyword? Keyword, IEnumerable<string> Tweets, IEnumerable<Word> PositiveKeywordAndCount, IEnumerable<Word> NegativeKeywordAndCount);
