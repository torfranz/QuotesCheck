namespace QuotesCheck.Evaluation
{
    using Accord;

    internal class TargetResult
    {
        public TargetResult(IntRange range)
        {
            this.Range = range;
            this.Targets = new IndexedTarget[range.Length + 1];
        }

        public IntRange Range { get; }
        
        public IndexedTarget[] Targets { get; }
    }
}