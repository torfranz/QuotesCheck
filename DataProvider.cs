namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Avapi;
    using Avapi.AvapiTIME_SERIES_DAILY;

    internal class DataProvider
    {
        private readonly IAvapiConnection connection = AvapiConnection.Instance;

        public DataProvider()
        {
            // Set up the connection and pass the API_KEY provided by alphavantage.co
            this.connection.Connect("XS7IY6V9YRY2SL15");
        }

        public IList<TimeSeries> GetTimeSeries(
            SymbolInformation symbol,
            Const_TIME_SERIES_DAILY.TIME_SERIES_DAILY_outputsize size = Const_TIME_SERIES_DAILY.TIME_SERIES_DAILY_outputsize.full)
        {
            Trace.TraceInformation($"Download {(size == Const_TIME_SERIES_DAILY.TIME_SERIES_DAILY_outputsize.compact ? "compact" : "full")} data for {symbol.ISIN}");
            var list = new List<TimeSeries>();

            var previousSeries = new TimeSeries();
            foreach (var series in this.connection.GetQueryObject_TIME_SERIES_DAILY().Query(symbol.Symbol, size).Data.TimeSeries)
            {
                var close = double.Parse(series.close);
                var open = double.Parse(series.open);
                var high = double.Parse(series.high);
                var low = double.Parse(series.low);
                var volume = int.Parse(series.volume);
                var day = DateTime.Parse(series.DateTime);

                if (close == 0 && open == 0)
                {
                    Trace.TraceWarning($"Data at {day:d} is missing and will be substituted with previous day data");
                }

                var timeSeries = new TimeSeries
                                     {
                                         Close = close > 0 ? close : previousSeries.Close,
                                         High = high > 0 ? high : previousSeries.High,
                                         Open = open > 0 ? open : previousSeries.Open,
                                         Low = low > 0 ? low : previousSeries.Low,
                                         Volume = volume > 0 ? volume : previousSeries.Volume,
                                         Day = day
                                     };
                list.Add(timeSeries);
                previousSeries = timeSeries;
            }

            return list;
        }
    }
}