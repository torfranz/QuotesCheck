namespace QuotesCheck
{
    using System;

    using Avapi;
    using Avapi.AvapiEMA;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Updating symbol provider");
            var symbolProvider = new SymbolProvider();
            symbolProvider.UpdateReference();


            return;
            // Creating the connection object
            IAvapiConnection connection = AvapiConnection.Instance;

            // Set up the connection and pass the API_KEY provided by alphavantage.co
            connection.Connect("XS7IY6V9YRY2SL15");

            // Get the TIME_SERIES_DAILY query object
            var time_series_daily =
                connection.GetQueryObject_EMA();

            // Perform the TIME_SERIES_DAILY request and get the result
            var time_series_dailyResponse =
            time_series_daily.Query(
                 "A", Const_EMA.EMA_interval.daily, 20, Const_EMA.EMA_series_type.close);

            // Printout the results
            Console.WriteLine("******** RAW DATA TIME_SERIES_DAILY ********");
            Console.WriteLine(time_series_dailyResponse.RawData);

            
        }
    }
}
