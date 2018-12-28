using System;
using Microsoft.ML.Runtime.Api;

public class SentimentData
{
    [Column(ordinal: "0", name: "Label")]
    public double Sentiment;
    [Column(ordinal: "1")]
    public string SentimentText;
}

public class SentimentPrediction
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}