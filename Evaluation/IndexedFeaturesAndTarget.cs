namespace QuotesCheck.Evaluation
{
    internal class IndexedFeaturesAndTarget
    {
        public int Index { get; set; }

        public double[] Features { get; set; }

        public double? Target { get; set; }
    }
}