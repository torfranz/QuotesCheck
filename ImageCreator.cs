namespace QuotesCheck
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Threading.Tasks;

    using QuotesCheck.Evaluation;

    using ZedGraph;

    internal static class ImageCreator
    {
        public static void Save(SymbolInformation symbol, EvaluationResult result, string folderName)
        {
            Parallel.ForEach(
                result.Trades,
                trade =>
                    {
                        var pane = new GraphPane();
                        pane.Title.Text = $"{trade.BuyDate:yyyy-MM-dd} - {trade.SellDate:yyyy-MM-dd} - {trade.Gain:F0}% [{trade.PossibleGain:F0}%]";
                        
                        var spl = new StockPointList();

                        for (var i = trade.BuyIndex + 10; i >= Math.Max(0, trade.SellIndex - 10); i--)
                        {
                            var series = symbol.TimeSeries[i];
                            var pt = new StockPt(new XDate(series.Day), series.High, series.Low, series.Open, series.Close, series.Volume);
                            spl.Add(pt);
                        }

                        pane.AddCurve(
                            $"{trade.BuyValue:F2} - {trade.SellValue:F2}",
                            new double[] { new XDate(trade.BuyDate), new XDate(trade.SellDate) },
                            new[] { trade.BuyValue, trade.SellValue },
                            Color.Black,
                            SymbolType.Circle);

                        var myCurve = pane.AddJapaneseCandleStick($"{trade.BuyDate:yyyy-MM-dd} - {trade.SellDate:yyyy-MM-dd}", spl);
                        myCurve.Stick.IsAutoSize = true;
                        myCurve.Stick.FallingFill = new Fill(Color.Red);
                        myCurve.Stick.RisingFill = new Fill(Color.Green);
                        
                        pane.XAxis.Type = AxisType.Date;
                        pane.XAxis.Scale.Min = new XDate(symbol.TimeSeries[trade.BuyIndex + 10].Day);

                        // force an axischange to plot all data and recalculate all axis
                        // this is normally done by the control, but this is not possible in mvc3
                        var bm = new Bitmap(1, 1);
                        using (var g = Graphics.FromImage(bm))
                        {
                            pane.ReSize(g, new RectangleF(0, 0, 1280 * 5, 960 * 5));
                            pane.AxisChange(g);
                        }

                        // create a stream to store a PNG-format image
                        var actualFolder = Path.Combine(folderName, symbol.ISIN);
                        Directory.CreateDirectory(actualFolder);
                        var image = pane.GetImage(true);
                        image.Save(
                            Path.Combine(
                                actualFolder,
                                $"{trade.BuyDate:yyyy-MM-dd} - {trade.SellDate:yyyy-MM-dd} - {trade.Gain:F0}% [{trade.PossibleGain:F0}%].png"),
                            ImageFormat.Png);
                    });
        }
    }
}