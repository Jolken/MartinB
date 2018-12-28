using System.IO;
using Microsoft.ML;
using Microsoft.ML.Core.Data;
using Microsoft.ML.Runtime.Data;
using Microsoft.ML.Runtime.Api;

namespace ProjectMartinB
{
    class MartinMention
    {
        static MLContext ctx = new MLContext(seed: 0);
        static string path;
        static ITransformer loadedModel;
        static IDataView predictions;

        public MartinMention(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                loadedModel = ctx.Model.Load(stream);
            }
            
        }
        public bool Predict(string str)
        {
            var predictionFunction = loadedModel.MakePredictionFunction<SentimentData, SentimentPrediction>(ctx);
            var resultprediction = predictionFunction.Predict(new SentimentData
            {
                SentimentText = str
            });
            return resultprediction.Prediction;

        }
    }
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

    class MartinClassify
    {
        static MLContext ctx = new MLContext(seed: 0);
        static string path;
        static ITransformer loadedModel;
        static IDataView predictions;

        public MartinClassify(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                loadedModel = ctx.Model.Load(stream);
            }

        }
        public static byte Classify(string str)
        {
            return 0;
        }
    }
}
