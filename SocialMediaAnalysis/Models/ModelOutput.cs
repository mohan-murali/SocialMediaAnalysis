using Microsoft.ML.Data;

namespace SocialMediaAnalysis.Models;

public class ModelOutput
{
    [ColumnName("PredictedLabel")]
    public bool Sentiment {get;set;}
    public float Probability{get;set;}
    public float Score {get;set;}
}
