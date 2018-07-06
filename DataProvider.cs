namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

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
            Trace.TraceInformation(
                $"Download {(size == Const_TIME_SERIES_DAILY.TIME_SERIES_DAILY_outputsize.compact ? "compact" : "full")} data for {symbol.ISIN}");
            var list = new List<TimeSeries>();

            IAvapiResponse_TIME_SERIES_DAILY_Content seriesDaily = null;

            var retry = 0;
            while (seriesDaily == null)
            {
                try
                {
                    var response = this.connection.GetQueryObject_TIME_SERIES_DAILY().Query(symbol.Symbol, size);
                    if (response.Data != null)
                    {
                        seriesDaily = response.Data;
                    }
                }
                catch
                {
                    if (retry == 10)
                    {
                        throw;
                    }

                    Thread.Sleep(2000);
                    retry++;
                }
            }

            foreach (var series in seriesDaily.TimeSeries)
            {
                var close = double.Parse(series.close);
                var open = double.Parse(series.open);
                var high = double.Parse(series.high);
                var low = double.Parse(series.low);
                var volume = int.Parse(series.volume);
                var day = DateTime.Parse(series.DateTime);

                // ignore missing days or days with same value but no volume (holidays?)
                if (((open == 0.0) && (close == 0.0) && (high == 0.0) && (low == 0.0))
                    || ((open == close) && (high == close) && (low == close) && (volume == 0.0)))
                {
                    continue;
                }

                var timeSeries = new TimeSeries { Close = close, High = high, Open = open, Low = low, Volume = volume, Day = day };
                list.Add(timeSeries);
            }

            return list;
        }
    }
}