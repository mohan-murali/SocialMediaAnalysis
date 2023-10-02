using CsvHelper.Configuration.Attributes;

namespace SocialMediaAnalysis.Models;

public class TweetCsv
{
    [Name("name")] public string Name { get; set; }
    [Name("tweet")] public string Tweet { get; set; }
    [Name("location")] public string? Location { get; set; }
    [Name("created")] public string? Created { get; set; }
    [Name("retweets")] public string? Retweets { get; set; }
    [Name("likes")] public string? Likes { get; set; }
}