using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuotesCheck.Evaluation
{
    class ResultCreator
    {
        public ResultCreator(double costOfTrades)
        {
            this.CostOfTrades = costOfTrades;
        }

        public double CostOfTrades { get; }
        public double UpperBound { get; set; } = 10;

        public double LowerBound { get; set; } = -5;
        public EvaluationResult CreateResult(SymbolInformation symbol, int[] labels, int startIndex, int endIndex = 0)
        {
            var result = new EvaluationResult(
                symbol.CompanyName,
                symbol.ISIN,
                Helper.Delta(symbol.Open[endIndex - 1], symbol.Open[startIndex - 1]));

            Trade trade = null;

            double upperBound = 0;
            double lowerBound = 0;
            for (var i = startIndex; i >= endIndex; i--)
            {
                var label = labels[i];

                if ((trade == null) && (label == 1))
                {
                    trade = new Trade
                    {
                        BuyIndex = i - 1,
                        BuyValue = symbol.Open[i - 1],
                        BuyDate = symbol.Day[i - 1],
                        CostOfTrades = this.CostOfTrades
                    };
                    result.Trades.Add(trade);

                    upperBound = (1 + this.UpperBound / 100.0) * trade.BuyValue;
                    lowerBound = (1 + this.LowerBound / 100.0) * trade.BuyValue;

                    trade.LowerBoundCurve.Add(lowerBound);
                    trade.UpperBoundCurve.Add(upperBound);
                }
                else if (trade != null)
                {
                    var close = symbol.Close[i];

                    // is this day also expecting more gains, adapt upper and lower for followng days based on todays close
                    if (label == 1)
                    {
                        upperBound = Math.Max(upperBound, (1 + this.UpperBound / 100.0) * close);
                        lowerBound = Math.Max(lowerBound, (1 + this.LowerBound / 100.0) * close);
                    }

                    // set lower/upper bound for next day
                    trade.LowerBoundCurve.Add(lowerBound);
                    trade.UpperBoundCurve.Add(upperBound);

                    // did the close leave the lowerBound -> upperBound range, close the trade on next day open
                    if ((close >= upperBound) || (close <= lowerBound))
                    {
                        trade.SellIndex = i - 1;
                        trade.SellValue = symbol.Open[i - 1];
                        trade.SellDate = symbol.Day[i - 1];

                        this.SetHighestValueForTrade(symbol, trade);

                        trade = null;
                    }
                }
            }

            // Trade still open? -> exit with last close
            if (trade != null)
            {
                // last data point is always considered an exit point
                trade.SellIndex = endIndex;
                trade.SellValue = symbol.Close[endIndex];
                trade.SellDate = symbol.Day[endIndex];
                this.SetHighestValueForTrade(symbol, trade);
            }

            return result;
        }

        private void SetHighestValueForTrade(SymbolInformation symbol, Trade trade)
        {
            double max = 0;
            for (var i = trade.SellIndex; i < trade.BuyIndex; i++)
            {
                max = Math.Max(max, symbol.Open[i]);
            }

            trade.HighestValue = max;
        }
    }
}
