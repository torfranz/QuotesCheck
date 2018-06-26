namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using QuotesCheck.Evaluation;

    using ZedGraph;

    internal static class ImageCreator
    {
        public static Color[] LineColors = { Color.Red, Color.Blue, Color.Green, Color.Brown };
        public static void Save(SymbolInformation symbol, EvaluationResult result, IList<(string Name, double[] Values)> curveData, string folderName)
        {
            Parallel.ForEach(
                result.Trades,
                trade =>
                    {
                        var pane = new GraphPane();
                        pane.Title.Text = $"{trade.BuyDate:yyyy-MM-dd} - {trade.SellDate:yyyy-MM-dd} - {trade.Gain:F0}% [{trade.PossibleGain:F0}%]";
                        
                        // Candles & Curves
                        var spl = new StockPointList();

                        var colorIdx = 0;
                        var curves = curveData.Select(data => pane.AddCurve(data.Name, new double[] { }, new double[] { }, LineColors[colorIdx++], SymbolType.None)).ToArray();

                        for (var i = trade.BuyIndex + 10; i >= Math.Max(0, trade.SellIndex - 10); i--)
                        {
                            var series = symbol.TimeSeries[i];
                            var day = new XDate(series.Day);

                            // candle
                            var pt = new StockPt(day, series.High, series.Low, series.Open, series.Close, series.Volume);
                            spl.Add(pt);

                            // curves
                            for  (var curveIdx = 0; curveIdx <  curves.Length; curveIdx++)
                            {
                                curves[curveIdx].AddPoint(day, curveData[curveIdx].Values[i]);
                            }
                        }
                        var myCurve = pane.AddJapaneseCandleStick($"{trade.BuyDate:yyyy-MM-dd} - {trade.SellDate:yyyy-MM-dd}", spl);
                        myCurve.Stick.IsAutoSize = true;
                        myCurve.Stick.FallingFill = new Fill(Color.Red);
                        myCurve.Stick.RisingFill = new Fill(Color.Green);

                        foreach (var curve in curves)
                        {
                            curve.Line.Width = 3;
                            curve.Line.IsAntiAlias = true;
                        }

                        // Buy-Sell Line
                        pane.AddCurve(
                            $"{trade.BuyValue:F2} - {trade.SellValue:F2}",
                            new double[] { new XDate(trade.BuyDate), new XDate(trade.SellDate) },
                            new[] { trade.BuyValue, trade.SellValue },
                            Color.Black,
                            SymbolType.Circle).Line.Width = 3 ;

                        // Stop-Loss
                        var stopLossCurve = pane.AddCurve($"Stop-Loss {result.Parameters[0]:F1}%", new double[] { }, new double[] { }, Color.DarkKhaki, SymbolType.None);
                        var stopLoss = trade.GetStopLossCurve(symbol, result.Parameters[0]);
                        stopLossCurve.Line.Width = 3;
                        stopLossCurve.Line.IsAntiAlias = true;
                        for (var i = trade.BuyIndex - 1; i > trade.SellIndex; i--)
                        {
                            var series = symbol.TimeSeries[i];
                            var day = new XDate(series.Day);

                            stopLossCurve.AddPoint(day, stopLoss[trade.BuyIndex - i]);
                        }
                        
                        // axis settings
                        pane.XAxis.Type = AxisType.Date;
                        pane.XAxis.Scale.FontSpec.Size = 6;
                        pane.XAxis.Scale.Min = new XDate(symbol.TimeSeries[trade.BuyIndex + 10].Day);
                        pane.XAxis.Scale.Max = new XDate(symbol.TimeSeries[Math.Max(0, trade.SellIndex - 10)].Day);
                        pane.XAxis.MinorGrid.IsVisible = true;
                        pane.XAxis.MajorGrid.IsVisible = true;

                        pane.YAxis.Scale.FontSpec.Size = 6;
                        pane.YAxis.MinorGrid.IsVisible = true;
                        pane.YAxis.MajorGrid.IsVisible = true;


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