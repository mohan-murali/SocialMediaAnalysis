using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SocialMediaAnalysis.Models;

public record Tweet(string Name, string Text, string? Location, string? Created, string Sentiment,
    float? ConfidenceScore, float? Probability, string? Retweets, string? Likes, string UploaderEmail)
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Id { get; set; }
}

public record TweetResult(List<Tweet> Tweets, long TotalTweets);

public record TweetFacet(string? Sentiment, string? HashTag);