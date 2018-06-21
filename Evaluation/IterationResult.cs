namespace QuotesCheck.Evaluation
{
    using Newtonsoft.Json;

    public class IterationResult
    {
        public int Iteration { get; set; }

        [JsonConverter(typeof(DoubleJsonConverter))]
        public double Gain { get; set; }

        public double[] Parameters { get; set; }
    }
}