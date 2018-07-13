namespace QuotesCheck.Evaluation
{
    using Accord;

    internal class LabelCreator
    {
        public double UpperBound { get; set; } = 10;

        public double LowerBound { get; set; } = -5;

        public int CandleCount { get; set; } = 50;

        public int[] GenerateLabels(SymbolInformation symbol, IntRange range)
        {
            var labels = new int[range.Length];
            for (var idx = range.Min; idx <= range.Max; idx++)
            {
                labels[idx] = this.FindLabel(symbol, symbol.Open[idx - 1], idx - 1);
            }

            return labels;
        }

        private int FindLabel(SymbolInformation symbol, double startValue, int startIndex)
        {
            for (var idx = startIndex; idx >= startIndex - this.CandleCount; idx--)
            {
                if (Helper.Delta(symbol.Close[idx], startValue) > this.UpperBound)
                {
                    return 1;
                }

                if (Helper.Delta(symbol.Close[idx], startValue) < this.LowerBound)
                {
                    return 0;
                }
            }

            return 0;
        }
    }
}