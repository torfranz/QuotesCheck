using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuotesCheck.Evaluation
{
    public class IterationResult
    {
        public int Iteration { get; set; }

        public double Value { get; set; }

        public double[] Parameters { get; set; }
    }
}
