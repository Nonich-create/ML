using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Linq;

namespace SpamClassifier
{
    class Program
    {
        static readonly string _path = "..\\..\\..\\Data\\ham-spam.csv";

        static readonly string[] _samples =
        {
            "If you can get the new revenue projections to me by Friday, I'll fold them into the forecast.",
            "Can you attend a meeting in Atlanta on the 16th? I'd like to get the team together to discuss in-person.",
            "Why pay more for expensive meds when you can order them online and save $$$?"
        };

        static void Main(string[] args)
        {
            var context = new MLContext(seed: 0);

            // Load the data
            var data = context.Data.LoadFromTextFile<Input>(_path, hasHeader: true, separatorChar: ',');

            // Split the data into a training set and a test set
            var trainTestData = context.Data.TrainTestSplit(data, testFraction: 0.2, seed: 0);
            var trainData = trainTestData.TrainSet;
            var testData = trainTestData.TestSet;

            // Build and train the model
            var pipeline = context.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: "Text")
                .Append(context.BinaryClassification.Trainers.SdcaLogisticRegression());

            Console.WriteLine("Training the model...");
            var model = pipeline.Fit(trainData);

            // Evaluate the model
            var predictions = model.Transform(testData);
            var metrics = context.BinaryClassification.Evaluate(predictions, "Label");

            Console.WriteLine();
            Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
            Console.WriteLine($"AUC: {metrics.AreaUnderPrecisionRecallCurve:P2}");
            Console.WriteLine($"F1: {metrics.F1Score:P2}");
            Console.WriteLine();

            // Use the model to make predictions
            var predictor = context.Model.CreatePredictionEngine<Input, Output>(model);

            foreach (var sample in _samples)
            {
                var input = new Input { Text = sample };
                var prediction = predictor.Predict(input);

                Console.WriteLine();
                Console.WriteLine($"{input.Text}");
                Console.WriteLine($"Spam score: {prediction.Probability}");
                Console.WriteLine($"Classification: {(Convert.ToBoolean(prediction.Prediction) ? "Spam" : "Not spam")}");
            }

            Console.WriteLine();
        }
    }

    public class Input
    {
        [LoadColumn(0), ColumnName("Label")]
        public bool IsSpam;

        [LoadColumn(1)]
        public string Text;
    }

    public class Output
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }
        public float Probability { get; set; }
    }
}