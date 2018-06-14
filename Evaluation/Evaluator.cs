using QuotesCheck;
using System.Collections.Generic;
using System.Diagnostics;

internal abstract class Evaluator
{
    protected SymbolInformation Symbol { get; private set; }
    protected abstract void GenerateFixtures();
    internal IList<Trade> Evaluate(SymbolInformation symbol, double[] parametersEntry, double[] parametersExit)
    {
        var sw = Stopwatch.StartNew();

        this.Symbol = symbol;
        Debug.Assert(Symbol.TimeSeries.Count > 100);

        this.GenerateFixtures();

        var buySellInfo = new Trade();

        var result = new List<Trade>();
        for (int i = Symbol.TimeSeries.Count - 100; i >= 1 ; i--)
        {
            if (buySellInfo.BuyValue == 0)
            {
                var (isEntry, value) = IsEntry(i, parametersEntry);
                if (isEntry)
                {
                    buySellInfo.BuyValue = value;
                    buySellInfo.BuyDate = Symbol.TimeSeries[i].Day;
                }
            }
            else
            {   
                    var (isExit, value) = IsExit(i, parametersExit);
                    if (isExit)
                    {
                        buySellInfo.SellValue = value;
                        buySellInfo.SellDate = Symbol.TimeSeries[i].Day;

                        result.Add(buySellInfo);
                        buySellInfo = new Trade();
                    }
                 
            }

            
        }

        // BuySell still open? -> exit with last close
        if (buySellInfo.SellValue == 0)
        {
            // last data point is always considered an exit point
            buySellInfo.SellValue = Symbol.TimeSeries[0].Close;
            buySellInfo.SellDate = Symbol.TimeSeries[0].Day;

            result.Add(buySellInfo);
        }

        Trace.TraceInformation($"Evaluation took {sw.ElapsedMilliseconds:F0}ms for {Symbol}");

        return result;
    }

    abstract protected (bool, double) IsEntry(int index, double[] parameters);
    abstract protected (bool, double) IsExit(int index, double[] parameters);
}
