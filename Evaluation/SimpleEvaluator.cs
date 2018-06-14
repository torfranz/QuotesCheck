using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuotesCheck.Evaluation
{
    internal class SimpleEvaluator : Evaluator
    {
        protected override void GenerateFixtures()
        {
            
        }

        protected override (bool, double) IsEntry(int index, double[] parameters)
        {
            Debug.Assert(parameters.Length == 2);
            var ema20 = Indicators.EMA(this.Symbol, SourceType.Close, Convert.ToInt32(parameters[0]));
            var ema50 = Indicators.EMA(this.Symbol, SourceType.Close, Convert.ToInt32(parameters[1]));

            if (ema20[index + 1] < ema50[index + 1] && ema20[index] > ema50[index])
            {
                return (true, this.Symbol.Open[index + 1]);
            }

            return (false, 0);
        }

        protected override (bool, double) IsExit(int index, double[] parameters)
        {
            Debug.Assert(parameters.Length == 2);
            var ema20 = Indicators.EMA(this.Symbol, SourceType.Close, Convert.ToInt32(parameters[0]));
            var ema50 = Indicators.EMA(this.Symbol, SourceType.Close, Convert.ToInt32(parameters[1]));

            if (ema20[index + 1] > ema50[index + 1] && ema20[index] < ema50[index])
            {
                return (true, this.Symbol.Open[index + 1]);
            }

            return (false, 0);
        }
    }
}
