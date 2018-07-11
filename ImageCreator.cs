namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using QuotesCheck.Evaluation;

    using ZedGraph;

    internal static class ImageCreator
    {
        public static Color[] LineColors =
            {
                ColorTranslator.FromHtml("#8dd3c7"), ColorTranslator.FromHtml("#ffffb3"), ColorTranslator.FromHtml("#bebada"),
                ColorTranslator.FromHtml("#fb8072"), ColorTranslator.FromHtml("#80b1d3"), ColorTranslator.FromHtml("#fdb462"),
                ColorTranslator.FromHtml("#b3de69"), ColorTranslator.FromHtml("#fccde5"), ColorTranslator.FromHtml("#d9d9d9"),
                ColorTranslator.FromHtml("#bc80bd"), ColorTranslator.FromHtml("#ccebc5"), ColorTranslator.FromHtml("#ffed6f"), Color.Red, Color.Green,
                Color.Yellow, Color.Blue, Color.Orange, Color.Purple, Color.Cyan, Color.Magenta, Color.Lime, Color.Pink, Color.Teal, Color.Lavender,
                Color.Brown, Color.Beige, Color.Maroon, Color.MintCream, Color.Olive, Color.Coral, Color.Navy
            };

        public static void Save(
            SymbolInformation symbol,
            EvaluationResult result,
            IList<(string Name, double[] Values, bool IsLine, bool IsDot)> curveData,
            string folderName)
        {
            Parallel.ForEach(
                result.Trades,
                trade =>
                    {
                        var pane = new GraphPane();
                        pane.Title.Text = $"{trade.BuyDate:yyyy-MM-dd} - {trade.SellDate:yyyy-MM-dd} - {trade.Gain:F0}% [{trade.PossibleGain:F0}%]";
                        pane.Title.FontSpec.Size = 8;
                        pane.Chart.Fill.IsScaled = false;
                        pane.Chart.Fill.Brush = new SolidBrush(Color.DimGray);
                        pane.Fill.Brush = new SolidBrush(Color.DimGray);
                        pane.Legend.Fill.Brush = new SolidBrush(Color.DimGray);
                        pane.Legend.FontSpec.Size = 6;

                        // Candles & Curves
                        var colorIdx = 0;
                        var curves = curveData.Select(
                            data =>
                                {
                                    var curve = pane.AddCurve(data.Name, new double[] { }, new double[] { }, LineColors[colorIdx++], SymbolType.Circle);
                                    curve.Line.Width = 5;
                                    curve.Line.IsVisible = data.IsLine;
                                    curve.Line.IsAntiAlias = true;
                                    curve.Symbol.IsAntiAlias = true;
                                    curve.Symbol.IsVisible = data.IsDot;
                                    curve.Symbol.Size = 1;
                                    curve.Symbol.Fill.IsVisible = true;
                                    curve.Symbol.Fill.Brush = new SolidBrush(curve.Color);
                                    curve.Symbol.Border.IsVisible = false;
                                    return curve;
                                }).ToArray();

                        var offset = 10 + Math.Max(0, 100 - (trade.BuyIndex - trade.SellIndex)) / 2;
                        var indexHigh = Math.Min(symbol.TimeSeries.Count - 1, trade.BuyIndex + offset);
                        var indexLow = Math.Max(0, trade.SellIndex - offset);
                        var candles = new StockPointList();
                        var tradeCandles = new StockPointList();
                        for (var i = indexHigh; i >= indexLow; i--)
                        {
                            var series = symbol.TimeSeries[i];
                            var day = new XDate(series.Day);

                            // candle
                            var candle = new StockPt(day, series.High, series.Low, series.Open, series.Close, series.Volume);

                            if (i == trade.BuyIndex || i == trade.SellIndex)
                            {
                                tradeCandles.Add(candle);
                            }
                            else
                            {
                                candles.Add(candle);
                            }
                            
                            // curves
                            for (var curveIdx = 0; curveIdx < curves.Length; curveIdx++)
                            {
                                curves[curveIdx].AddPoint(day, curveData[curveIdx].Values[i]);
                            }
                        }

                        // add standard candles
                        var candlesCurve = pane.AddJapaneseCandleStick($"{trade.BuyDate:yyyy-MM-dd} - {trade.SellDate:yyyy-MM-dd}", candles);
                        candlesCurve.Stick.IsAutoSize = true;
                        candlesCurve.Stick.FallingFill = new Fill(Color.Red);
                        candlesCurve.Stick.RisingFill = new Fill(Color.Green);

                        // add trading candles
                        var tradingCandlesCurve = pane.AddJapaneseCandleStick("", tradeCandles);
                        tradingCandlesCurve.Label.IsVisible = false;
                        tradingCandlesCurve.Stick.Size = 2;
                        tradingCandlesCurve.Stick.FallingFill = new Fill(Color.Orange);
                        tradingCandlesCurve.Stick.RisingFill = new Fill(Color.LightGreen);
                        
                        // Buy-Sell Line
                        var buySellCurve = pane.AddCurve(
                            $"{trade.BuyValue:F2} - {trade.SellValue:F2}",
                            new double[] { new XDate(trade.BuyDate), new XDate(trade.SellDate) },
                            new[] { trade.BuyValue, trade.SellValue },
                            Color.Purple,
                            SymbolType.Circle);
                        buySellCurve.Line.Width = 3;
                        buySellCurve.Line.Style = DashStyle.Dot;

                        // Lower/Upper bound curve
                        var lowerBoundCurve = pane.AddCurve(
                            "Lower Bound",
                            new double[] { },
                            new double[] { },
                            Color.FloralWhite,
                            SymbolType.None);
                        var upperBoundCurve = pane.AddCurve(
                            "Upper Bound",
                            new double[] { },
                            new double[] { },
                            Color.FloralWhite,
                            SymbolType.None);

                        lowerBoundCurve.Line.Width = 5;
                        lowerBoundCurve.Line.IsAntiAlias = true;
                        upperBoundCurve.Line.Width = 5;
                        upperBoundCurve.Line.IsAntiAlias = true;
                        for (var i = trade.BuyIndex; i > trade.SellIndex; i--)
                        {
                            var series = symbol.TimeSeries[i];
                            var day = new XDate(series.Day);

                            lowerBoundCurve.AddPoint(day, trade.LowerBoundCurve[trade.BuyIndex - i]);
                            upperBoundCurve.AddPoint(day, trade.UpperBoundCurve[trade.BuyIndex - i]);
                        }

                        // axis settings
                        pane.XAxis.Type = AxisType.Date;
                        pane.XAxis.Scale.FontSpec.Size = 6;
                        pane.XAxis.Scale.Min = new XDate(symbol.TimeSeries[indexHigh].Day);
                        pane.XAxis.Scale.Max = new XDate(symbol.TimeSeries[indexLow].Day);
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