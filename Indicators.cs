﻿namespace QuotesCheck
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using MathNet.Numerics.Statistics;

    internal enum MovingAverage
    {
        SMA,

        EMA,

        DEMA,

        TEMA
    }

    internal static class Indicators
    {
        private static readonly Dictionary<MovingAverage, Func<double[], int, double[]>> maFuncs =
            new Dictionary<MovingAverage, Func<double[], int, double[]>>
                {
                    { MovingAverage.SMA, SMA },
                    { MovingAverage.EMA, EMA },
                    { MovingAverage.DEMA, DEMA },
                    { MovingAverage.TEMA, TEMA },
                };

        public static double[] EMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return EMA(symbol.Data(sourceType), period);
        }

        public static double[] SMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return SMA(symbol.Data(sourceType), period);
        }

        public static double[] DEMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return DEMA(symbol.Data(sourceType), period);
        }

        public static double[] VWMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return VWMA(symbol.Data(sourceType), symbol.Volume, period);
        }

        public static double[] VPT(SymbolInformation symbol, SourceType sourceType)
        {
            return VPT(symbol.Data(sourceType), symbol.Volume);
        }

        public static double[] OBV(SymbolInformation symbol, SourceType sourceType)
        {
            return OBV(symbol.Data(sourceType), symbol.Volume);
        }

        public static double[] MD(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return MD(symbol.Data(sourceType), period);
        }

        public static double[] WMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return WMA(symbol.Data(sourceType), period);
        }

        public static (double[] MACD, double[] Signal) MACD(
            SymbolInformation symbol,
            SourceType sourceType,
            int fastPeriod,
            int slowPeriod,
            int signalPeriod,
            MovingAverage movingAverage = MovingAverage.EMA)
        {
            return MACD(symbol.Data(sourceType), fastPeriod, slowPeriod, signalPeriod, movingAverage);
        }

        public static (double[] MACD, double[] Signal) RelativeMACD(
            SymbolInformation symbol,
            SourceType sourceType,
            int fastPeriod,
            int slowPeriod,
            int signalPeriod,
            MovingAverage movingAverage = MovingAverage.EMA)
        {
            return RelativeMACD(symbol.Data(sourceType), fastPeriod, slowPeriod, signalPeriod, movingAverage);
        }

        public static double[] TEMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return TEMA(symbol.Data(sourceType), period);
        }

        public static double[] DIX(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return DIX(symbol.Data(sourceType), period);
        }

        public static double[] VOLA(SymbolInformation symbol, SourceType sourceType, int period, int periodYear)
        {
            return VOLA(symbol.Data(sourceType), period, periodYear);
        }

        public static double[] HH(SymbolInformation symbol, int period)
        {
            return HH(symbol.High, period);
        }

        public static double[] LL(SymbolInformation symbol, int period)
        {
            return LL(symbol.Low, period);
        }

        public static double[] ST(SymbolInformation symbol, int period, double factor)
        {
            // p = integer("Period", 10)
            // f = integer("Factor", 3)

            // # calculation
            // atr = sum(max(high - low, high - close[1], close[1] - low), p) / p
            // up = (high + low) / 2 - f * atr
            // down = (high + low) / 2 + f * atr

            // trendUp = close[1] > trendUp[1] ? max(up, trendUp[1]) : up
            // trendDown = close[1] < trendDown[1] ? min(down, trendDown[1]) : down
            // trend = close > trendDown[1] ? 1 : (close < trendUp[1] ? -1 : nn(trend[1], 1))

            // st = skip(trend == 1 ? trendUp : trendDown, p)

            var high = symbol.High;
            var low = symbol.Low;
            var close = symbol.Close;

            var atr = ATR(symbol, period);

            var trendUp = Create(close.Length);
            var trendDown = Create(close.Length);
            var trend = new int[close.Length];
            var st = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                var up = (high[index] + low[index]) / 2 - factor * atr[index];
                var down = (high[index] + low[index]) / 2 + factor * atr[index];

                trendUp[index] = close.At(index + 1) > trendUp.At(index + 1) ? Math.Max(up, trendUp.At(index + 1)) : up;
                trendDown[index] = close.At(index + 1) < trendDown.At(index + 1) ? Math.Min(down, trendDown.At(index + 1)) : down;
                trend[index] = close[index] > trendDown.At(index + 1) ? 1 : (close[index] < trendUp.At(index + 1) ? -1 : trend.At(index + 1, 1));

                st[index] = nn(trend[index] == 1 ? trendUp[index] : trendDown[index], close[index]);
            }

            return st;
        }

        public static double[] HiLoDiff(SymbolInformation symbol)
        {
            var high = symbol.High;
            var low = symbol.Low;
            var hiLoDiff = Create(high.Length);
            for (var index = high.Length - 1; index >= 0; index--)
            {
                hiLoDiff[index] = high[index] - low[index];
            }

            return hiLoDiff;
        }

        public static double[] DayOfWeek(SymbolInformation symbol)
        {
            var day = symbol.Day;
            var dayOfWeek = Create(day.Length);
            for (var index = day.Length - 1; index >= 0; index--)
            {
                dayOfWeek[index] = Convert.ToInt32(day[index].DayOfWeek);
            }

            return dayOfWeek;
        }

        public static double[] Month(SymbolInformation symbol)
        {
            var day = symbol.Day;
            var month = Create(day.Length);
            for (var index = day.Length - 1; index >= 0; index--)
            {
                month[index] = day[index].Month;
            }

            return month;
        }

        public static double[] PSAR(SymbolInformation symbol, double factor, double increment, double factorMax)
        {
            // Difference of High and Low
            var hiLoDiff = HiLoDiff(symbol);

            // STDEV of differences
            var stDev = hiLoDiff.StandardDeviation();

            var high = symbol.High;
            var low = symbol.Low;
            var sarArr = Create(high.Length);

            /* Find first non-NA value */
            var beg = high.Length - 2;
            for (var i = high.Length - 1; i >= 0; i++)
            {
                if ((high[i] == 0) || (low[i] == 0))
                {
                    sarArr[i] = 0;
                    beg--;
                }
                else
                {
                    break;
                }
            }

            /* Initialize values needed by the routine */
            var sig0 = 1;
            var xpt0 = high[beg + 1];
            var af0 = factor;
            sarArr[beg + 1] = low[beg + 1] - stDev;

            for (var idx = beg; idx >= 0; idx--)
            {
                /* Increment signal, extreme point, and acceleration factor */
                var sig1 = sig0;
                var xpt1 = xpt0;
                var af1 = af0;

                /* Local extrema */
                var lmin = low[idx + 1] > low[idx] ? low[idx] : low[idx + 1];
                var lmax = high[idx + 1] > high[idx] ? high[idx + 1] : high[idx];
                /* Create signal and extreme price vectors */
                if (sig1 == 1)
                {
                    /* Previous buy signal */
                    sig0 = low[idx] > sarArr[idx + 1] ? 1 : -1; /* New signal */
                    xpt0 = lmax > xpt1 ? lmax : xpt1; /* New extreme price */
                }
                else
                {
                    /* Previous sell signal */
                    sig0 = high[idx] < sarArr[idx + 1] ? -1 : 1; /* New signal */
                    xpt0 = lmin > xpt1 ? xpt1 : lmin; /* New extreme price */
                }

                /*
                    * Calculate acceleration factor (af)
                    * and stop-and-reverse (sar) vector
                */

                /* No signal change */
                if (sig0 == sig1)
                {
                    /* Initial calculations */
                    sarArr[idx] = sarArr[idx + 1] + (xpt1 - sarArr[idx + 1]) * af1;
                    af0 = af1 >= factorMax ? factorMax : factor + increment;
                    /* Current buy signal */
                    if (sig0 == 1)
                    {
                        af0 = xpt0 > xpt1 ? af0 : af1; /* Update acceleration factor */
                        sarArr[idx] = sarArr[idx] > lmin ? lmin : sarArr[idx]; /* Determine sar value */
                    }
                    /* Current sell signal */
                    else
                    {
                        af0 = xpt0 < xpt1 ? af0 : af1; /* Update acceleration factor */
                        sarArr[idx] = sarArr[idx] > lmax ? sarArr[idx] : lmax; /* Determine sar value */
                    }
                }
                else /* New signal */
                {
                    af0 = factor; /* reset acceleration factor */
                    sarArr[idx] = xpt0; /* set sar value */
                }
            }

            return sarArr;
        }

        public static double[] TP(SymbolInformation symbol)
        {
            // tp = (close + high + low) / 3

            var high = symbol.High;
            var low = symbol.Low;
            var close = symbol.Close;

            var tp = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                tp[index] = (close[index] + low[index] + high[index]) / 3;
            }

            return tp;
        }

        public static (double[] Bullish, double[] Bearish) ELR(SymbolInformation symbol, int length)
        {
            // x = ema(close, length)
            // bullish = high-x
            // bearish = low-x

            var high = symbol.High;
            var low = symbol.Low;
            var close = symbol.Close;

            var x = EMA(close, length);
            var bullish = Create(close.Length);
            var bearish = Create(close.Length);

            for (var index = close.Length - 1; index >= 0; index--)
            {
                bullish[index] = high[index] - x[index];
                bearish[index] = low[index] - x[index];
            }

            return (bullish, bearish);
        }

        public static double[] ADX(SymbolInformation symbol, int period)
        {
            // n = integer("Period", 14)

            // # calculation
            // tr = sum(max(high - low, high - close[1], low - close[1]), n)
            // diPlus = sum(max(high - high[1], 0), n) / tr
            // diMinus = sum(max(high <= high[1] ? low[1] - low : 0, 0), n) / tr
            // dmi = abs((diPlus - diMinus) / (diPlus + diMinus)) * 100
            // adx = sma(dmi, n)

            var (dmi, _, _) = DMI(symbol, period);

            return SMA(dmi, period);
        }

        public static double[] AOS(SymbolInformation symbol, int period)
        {
            // aos = aroonUp - aroonDown

            var close = symbol.Close;
            var aroonUp = AROUp(symbol.High, period);
            var aroonDown = ARODown(symbol.Low, period);

            var aos = Create(close.Length);

            for (var index = close.Length - 1; index >= 0; index--)
            {
                aos[index] = aroonUp[index] - aroonDown[index];
            }

            return aos;
        }

        public static double[] ROC(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return ROC(symbol.Data(sourceType), period);
        }

        public static double[] MOM(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return MOM(symbol.Data(sourceType), period);
        }

        public static double[] RSI(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return RSI(symbol.Data(sourceType), period);
        }

        public static (double[] DMI, double[] DIPlus, double[] DIMinus) DMI(SymbolInformation symbol, int period)
        {
            // n = integer("Period", 14)

            // # calculation
            // tr = sum(max(high - low, high - close[1], low - close[1]), n)
            // diPlus = sum(max(high - high[1], 0), n) / tr
            // diMinus = sum(max(high <= high[1] ? low[1] - low : 0, 0), n) / tr
            // dmi = abs((diPlus - diMinus) / (diPlus + diMinus)) * 100

            var high = symbol.High;
            var low = symbol.Low;
            var close = symbol.Close;
            var tr = TR(symbol, period);

            var dmi = Create(close.Length);
            var diPlus = Create(close.Length);
            var diMinus = Create(close.Length);

            for (var index = close.Length - 1; index >= 0; index--)
            {
                var diP = 0.0;
                var diM = 0.0;
                for (var i = 0; i < period; i++)
                {
                    diP += Math.Max(high.At(index + i) - high.At(index + i + 1), 0);
                    diM += Math.Max(high.At(index + i) <= high.At(index + i + 1) ? low.At(index + i + 1) - low.At(index + i) : 0, 0);
                }

                diPlus[index] = nn(diP / tr[index] * 100);
                diMinus[index] = nn(diM / tr[index] * 100);
                dmi[index] = nn(Math.Abs((diPlus[index] - diMinus[index]) / (diPlus[index] + diMinus[index])) * 100);
            }

            return (dmi, diPlus, diMinus);
        }

        public static (double[] StochasticLine, double[] TriggerLine) DSSBR(
            SymbolInformation symbol,
            int stochasticPeriod,
            int smoothingPeriod,
            int triggerPeriod)
        {
            // # input
            // n = integer("Stochastic Period", 21)
            // m = integer("Smoothing Period", 3)
            // tr = integer("Trigger Period", 8)

            // # calculation
            // hh = highest(high, n)
            // ll = lowest(low, n)
            // v1 = skip((hh == ll) ? 0 : (close - ll) / (hh - ll) * 100, n - 1)
            // smoothedV1 = ema(v1, m)

            // hh = highest(smoothedV1, n)
            // ll = lowest(smoothedV1, n)
            // v2 = skip((hh == ll) ? 0 : (smoothedV1 - ll) / (hh - ll) * 100, n - 1)

            // sV2 = ema(v2, m)
            // ssV2 = ema(sV2, tr)

            var close = symbol.Close;
            var hh = HH(symbol.High, stochasticPeriod);
            var ll = LL(symbol.Low, stochasticPeriod);

            var v = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                v[index] = hh[index] == ll[index] ? 0 : (close[index] - ll[index]) / (hh[index] - ll[index]) * 100;
            }

            var smoothedV1 = EMA(v, smoothingPeriod);
            hh = HH(smoothedV1, stochasticPeriod);
            ll = LL(smoothedV1, stochasticPeriod);

            for (var index = close.Length - 1; index >= 0; index--)
            {
                v[index] = hh[index] == ll[index] ? 0 : (smoothedV1[index] - ll[index]) / (hh[index] - ll[index]) * 100;
            }

            var sV2 = EMA(v, smoothingPeriod);
            var ssV2 = EMA(sV2, triggerPeriod);
            return (sV2, ssV2);
        }

        public static double[] AROUp(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return AROUp(symbol.Data(sourceType), period);
        }

        public static double[] ARODown(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return ARODown(symbol.Data(sourceType), period);
        }

        public static (double[] StochasticLine, double[] SmoothedLine) FSTOC(SymbolInformation symbol, int periodK, int periodD)
        {
            // n = integer("%%K period", 5)
            // n2 = integer("%%D period", 3)

            // # calculation
            // hh = highest(high, n)
            // ll = lowest(low, n)
            // fstoc = skip((hh == ll) ? 0 : (close - ll) / (hh - ll) * 100, n)
            // smoothed = sma(fstoc, n2)

            var close = symbol.Close;
            var hh = HH(symbol.High, periodK);
            var ll = LL(symbol.Low, periodK);

            var fstoc = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                fstoc[index] = hh[index] == ll[index] ? 0 : (close[index] - ll[index]) / (hh[index] - ll[index]) * 100;
            }

            var smoothed = SMA(fstoc, periodD);

            return (fstoc, smoothed);
        }

        public static (double[] pK, double[] pD, double[] pJ, double[] pKema) KDJ(SymbolInformation symbol, int lenL, int lenS, int lenK)
        {
            // lenL = integer("Period 1",5) 
            // lenS = integer("Period 2", 3)
            // lenK = integer("%%K Smoothing", 3)

            // # calculation
            // pK = 100 * ((close - lowest(close, lenL)) / (highest(high, lenL) - lowest(low, lenL)))
            // pD = 100 * (highest(high, lenS) / lowest(low, lenS))
            // pJ = (3 * pD) - (2 * pK)

            // pKema = ema(pK, lenK)

            var close = symbol.Close;
            var hhL = HH(symbol.High, lenL);
            var llL = LL(symbol.Low, lenL);
            var lcL = LL(symbol.Close, lenL);
            var hhS = HH(symbol.High, lenS);
            var llS = LL(symbol.Low, lenS);

            var pK = Create(close.Length);
            var pD = Create(close.Length);
            var pJ = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                pK[index] = 100 * ((close[index] - lcL[index]) / (hhL[index] - llL[index]));
                pD[index] = 100 * (hhS[index] / llS[index]);
                pJ[index] = 3 * pD[index] - 2 * pK[index];
            }

            var pKema = EMA(pK, lenK);

            return (pK, pD, pJ, pKema);
        }

        public static (double[] StochasticLine, double[] SmoothedLine) SSTOC(SymbolInformation symbol, int KPeriod, int DPeriod, int DPeriod2)
        {
            // n = integer("%%K period", 5)
            // n2 = integer("%%D period", 5)
            // n3 = integer("2th %%D period", 3)

            // # calculation
            // hh = highest(high, n)
            // ll = lowest(low, n)
            // fstoc = (hh == ll) ? 0 : ((close - ll) / (hh - ll) * 100)
            // sstoc = sma(fstoc, n2)
            // smoothed = sma(sstoc, n3)

            var (_, sstoc) = FSTOC(symbol, KPeriod, DPeriod);
            var smoothed = SMA(sstoc, DPeriod2);
            return (sstoc, smoothed);
        }

        public static double[] CCI(SymbolInformation symbol, int period)
        {
            // n = integer("Period", 20)

            // # calculation
            // tp = (high + low + close) / 3
            // avg = sum(tp, n) / n
            // sAvg = loop((i, res) { res + abs(tp[i] - avg) }, n) / n
            // cci = (sAvg == 0 ? 0 : ((tp - avg) / (0.015 * sAvg)))

            var close = symbol.Close;

            var tp = TP(symbol);
            var cci = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                var avg = Sum(tp, index, period) / period;
                var res = 0.0;
                for (var i = 0; i < period; i++)
                {
                    res += Math.Abs(tp.At(index + i) - avg);
                }

                var sAvg = res / period;
                cci[index] = (tp[index] - avg) / (0.015 * sAvg);
            }

            return cci;
        }

        public static double[] PCR(SymbolInformation symbol, int period)
        {
            // n = integer("%%R period", 14)

            // # calculation
            // hhlld = highest(high, n) - lowest(low, n)
            // pcr = skip(hhlld ? 100 - ((highest(high, n) - close) / hhlld) * 100 : 0, n - 1)

            var close = symbol.Close;
            var hh = HH(symbol, period);
            var ll = LL(symbol, period);

            var pcr = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                var hhlld = hh[index] - ll[index];
                pcr[index] = hhlld != 0 ? 100 - (hh[index] - close[index]) / hhlld * 100 : 0;
            }

            return pcr;
        }

        public static (double[] Smi, double[] signal) SMI(SymbolInformation symbol, int periodK, int periodD)
        {
            // a = integer("K", 5)
            // b = integer("D", 3)

            // # calculation
            // ll = lowest(low, a)
            // hh = highest(high, a)
            // diff = hh - ll

            // rdiff = close - (hh + ll) / 2

            // avgrel = ema(ema(rdiff, b), b)
            // avgdiff = ema(ema(diff, b), b)

            // SMI = avgdiff != 0 ? (avgrel / (avgdiff / 2) * 100) : 0
            // SMIsignal = ema(SMI, b)

            var close = symbol.Close;
            var hh = HH(symbol, periodK);
            var ll = LL(symbol, periodK);

            var diff = Create(close.Length);
            var rdiff = Create(close.Length);

            for (var index = close.Length - 1; index >= 0; index--)
            {
                diff[index] = hh[index] - ll[index];
                rdiff[index] = close[index] - (hh[index] + ll[index]) / 2;
            }

            var avgrel = EMA(EMA(rdiff, periodD), periodD);
            var avgdiff = EMA(EMA(diff, periodD), periodD);

            var smi = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                smi[index] = avgdiff[index] != 0 ? avgrel[index] / (avgdiff[index] / 2) * 100 : 0;
            }

            var signal = EMA(smi, periodD);

            return (smi, signal);
        }

        public static double[] OBOS(SymbolInformation symbol, int period)
        {
            // n = integer("Period", 14)

            // # calculation
            // denom = highest(high, n) - lowest(low, n)
            // obos = denom == 0 ? 0 : (close - lowest(low, n)) / denom * 100

            var highest = HH(symbol, period);
            var lowest = LL(symbol, period);
            var close = symbol.Close;

            var obos = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                var denom = highest[index] - lowest[index];
                obos[index] = nn((close[index] - lowest[index]) / denom * 100);
            }

            return obos;
        }

        public static (double[] ShortStop, double[] LongStop) ELSZ(SymbolInformation symbol, int period, double coeffienct)
        {
            // # input
            // coeff = float("CoEff", 2.5)
            // lookbackLength = integer("LookBackLength", 15)

            // # calculation
            // countShort = high > high[1] ? 1 : 0
            // diffShort = high > high[1] ? high - high[1] : 0
            // totalCountShort = sum(countShort, lookbackLength)
            // totalSumShort = sum(diffShort, lookbackLength)
            // penAvgShort = (totalSumShort / totalCountShort)
            // safetyShort = high[1] + (penAvgShort[1] * coeff)
            // finalSafetyShort = min(min(safetyShort, safetyShort[1]), safetyShort[2])

            // count = low < low[1] ? 1 : 0
            // diff = low < low[1] ? low[1] - low : 0
            // totalCount = sum(count, lookbackLength)
            // totalSum = sum(diff, lookbackLength)
            // penAvg = (totalSum / totalCount)
            // safety = low[1] - (penAvg[1] * coeff)
            // finalSafetyLong = max(max(safety, safety[1]), safety[2])

            var close = symbol.Close;
            var high = symbol.High;
            var low = symbol.Low;
            var finalSafetyShort = Create(close.Length);
            var finalSafetyLong = Create(close.Length);
            var penAvgShort = Create(close.Length);
            var safetyShort = Create(close.Length);
            var penAvgLong = Create(close.Length);
            var safetyLong = Create(close.Length);
            for (var index = finalSafetyShort.Length - 1; index >= 0; index--)
            {
                var totalCountShort = 0;
                var totalSumShort = 0.0;
                var totalCountLong = 0;
                var totalSumLong = 0.0;
                for (var i = 0; i < period; i++)
                {
                    var high1 = high.At(index + i + 1);
                    var high0 = high.At(index + i);
                    if (high0 > high1)
                    {
                        totalCountShort++;
                        totalSumShort += high0 - high1;
                    }

                    var low1 = low.At(index + i + 1);
                    var low0 = low.At(index + i);
                    if (low0 < low1)
                    {
                        totalCountLong++;
                        totalSumLong += low1 - low0;
                    }
                }

                penAvgShort[index] = totalSumShort / totalCountShort;
                safetyShort[index] = high.At(index + 1) + penAvgShort.At(index + 1) * coeffienct;
                finalSafetyShort[index] = Math.Min(Math.Min(safetyShort[index], safetyShort.At(index + 1)), safetyShort.At(index + 2));

                penAvgLong[index] = totalSumLong / totalCountLong;
                safetyLong[index] = low.At(index + 1) - penAvgLong.At(index + 1) * coeffienct;
                finalSafetyLong[index] = Math.Max(Math.Max(safetyLong[index], safetyLong.At(index + 1)), safetyLong.At(index + 2));
            }

            return (finalSafetyShort, finalSafetyLong);
        }

        public static double[] ATR(SymbolInformation symbol, int period)
        {
            // atr = sum(max(high - low, high - close[1], close[1] - low), n) / n
            var close = symbol.Close;
            var high = symbol.High;
            var low = symbol.Low;

            var max = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                max[index] = Math.Max(Math.Max(high[index] - low[index], high[index] - close.At(index + 1)), close.At(index + 1) - low[index]);
            }

            var atr = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                atr[index] = Sum(max, index, period) / period;
            }

            return atr;
        }

        public static double[] TMA(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return TMA(symbol.Data(sourceType), period);
        }

        public static double[] STDEV(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return STDEV(symbol.Data(sourceType), period);
        }

        public static (double[] Upper, double[] Lower, double[] Middle) BB(SymbolInformation symbol, SourceType sourceType, int period, double factor)
        {
            return BB(symbol.Data(sourceType), period, factor);
        }

        public static double[] RSL(SymbolInformation symbol, SourceType sourceType, int period)
        {
            return RSL(symbol.Data(sourceType), period);
        }

        public static double[] KAMA(SymbolInformation symbol, SourceType sourceType, int length)
        {
            return KAMA(symbol.Data(sourceType), length);
        }

        public static double[] EMA(double[] data, int period)
        {
            // wf = 2 / (n + 1)
            // ema = nn(ema[1], close) + wf * nn(close - ema[1], 0)

            var wf = 2.0 / (period + 1);

            var ema = Create(data.Length);

            // EMA(t) = ((Close(t) – EMA(t-1)) * Ew(t)) + EMA(t-1)
            for (var index = data.Length - 1; index >= 0; index--)
            {
                var ema1 = nn(ema.At(index + 1), data[index]);
                ema[index] = (data[index] - ema1) * wf + ema1;
            }

            Debug.Assert(ema.Length == data.Length);
            return ema;
        }

        public static double[] SMA(double[] data, int period)
        {
            // sma = nn(sma[1], 0) + (close / n) - nn(close[n] / n, 0)
            var sma = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                sma[index] = nn(sma.At(index + 1)) + data[index] / period - nn(data.At(index + period) / period);
            }

            Debug.Assert(sma.Length == data.Length);
            return sma;
        }

        public static double[] DEMA(double[] data, int period)
        {
            // dema = 2*ema(close, n) - ema(ema(close, n), n)

            var ema1 = EMA(data, period);
            var ema2 = EMA(ema1, period);

            var dema = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                dema[index] = 2 * ema1[index] - ema2[index];
            }

            Debug.Assert(dema.Length == data.Length);
            return dema;
        }

        public static double[] Distance(double[] data1, double[] data2)
        {
            Debug.Assert(data1.Length == data2.Length);
            var distance = Create(data1.Length);

            // macd
            for (var index = data1.Length - 1; index >= 0; index--)
            {
                distance[index] = data1[index] - data2[index];
            }

            return distance;
        }

        public static double[] Ratio(double[] data1, double[] data2)
        {
            Debug.Assert(data1.Length == data2.Length);
            var ratio = Create(data1.Length);

            for (var index = data1.Length - 1; index >= 0; index--)
            {
                ratio[index] = data1[index] / data2[index];
            }

            return ratio;
        }

        public static double[] RelativeDistance(double[] data1, double[] data2)
        {
            Debug.Assert(data1.Length == data2.Length);
            var relativeDistance = Create(data1.Length);

            // macd
            for (var index = data1.Length - 1; index >= 0; index--)
            {
                relativeDistance[index] = nn((data1[index] - data2[index]) / data2[index]);
            }

            return relativeDistance;
        }

        public static (double[] MACD, double[] Signal) MACD(double[] data, int fastPeriod, int slowPeriod, int signalPeriod, MovingAverage movingAverag)
        {
            // macd = ema(close, p1) - ema(close, p2)
            // signal = ema(macd, pS)

            var maFast = maFuncs[movingAverag](data, fastPeriod);
            var maSlow = maFuncs[movingAverag](data, slowPeriod);

            var macd = Distance(maFast, maSlow);

            return (macd, maFuncs[movingAverag](macd, signalPeriod));
        }

        public static (double[] RelativeMACD, double[] RelativeSignal) RelativeMACD(
            double[] data,
            int fastPeriod,
            int slowPeriod,
            int signalPeriod,
            MovingAverage movingAverage)
        {
            var (macd, signal) = MACD(data, fastPeriod, slowPeriod, signalPeriod, movingAverage);

            return (Ratio(macd, data), Ratio(signal, data));
        }

        public static double[] TEMA(double[] data, int period)
        {
            // ema1 = ema(close, n)
            // ema2 = ema(ema1, n)
            // ema3 = ema(ema2, n)
            // tema = 3 * ema1 - 3 * ema2 + ema3

            var ema1 = EMA(data, period);
            var ema2 = EMA(ema1, period);
            var ema3 = EMA(ema2, period);

            var tema = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                tema[index] = 3 * ema1[index] - 3 * ema2[index] + ema3[index];
            }

            Debug.Assert(tema.Length == data.Length);
            return tema;
        }

        public static double[] RSI(double[] data, int n)
        {
            // # input
            // n = integer("Period", 14)

            // # calculation
            // w = 1 / n

            // up = close > close[1] ? close - close[1] : 0
            // down = close > close[1] ? 0 : close[1] - close

            // upSmoothed = up * w + (1 - w) * nn(upSmoothed[1], 0)
            // downSmoothed = down * w + (1 - w) * nn(downSmoothed[1], 0)

            // rsi = 100 - (100 / ((1 + upSmoothed / downSmoothed)))

            var w = 1.0 / n;
            var rsi = Create(data.Length);
            var upSmoothed = Create(data.Length);
            var downSmoothed = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var up = data[index] > data.At(index + 1) ? data[index] - data.At(index + 1) : 0;
                var down = data[index] > data.At(index + 1) ? 0 : data.At(index + 1) - data[index];
                upSmoothed[index] = up * w + (1 - w) * nn(upSmoothed.At(index + 1));
                downSmoothed[index] = down * w + (1 - w) * nn(downSmoothed.At(index + 1));

                rsi[index] = nn(100.0 - 100.0 / (1 + upSmoothed[index] / downSmoothed[index]));
            }

            Debug.Assert(rsi.Length == data.Length);
            return rsi;
        }

        public static double[] VOLA(double[] data, int period, int periodYear)
        {
            // # input
            // n = integer("Period", 30)
            // tp = integer("Periods/year", 250)

            // # calculation
            // dC = log(close) - log(close[1])
            // mC = sum(dC, n) / n
            // xC = sum(pow(dC, 2), n) - 2 * sum(dC, n) * mC + n * pow(mC, 2)
            // VOLA = sqrt(tp * xC / (n - 1))

            var dC = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                dC[index] = Math.Log(data[index]) - Math.Log(data.At(index + 1));
            }

            var vola = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var mC = Sum(dC, index, period) / period;
                var xC = Sum(dC, index, period, summand => Math.Pow(summand, 2)) - 2 * Sum(dC, index, period) * mC + period * Math.Pow(mC, 2);
                vola[index] = nn(Math.Sqrt(periodYear * xC / (period - 1)));
            }

            Debug.Assert(vola.Length == data.Length);
            return vola;
        }

        private static double[] AROUp(double[] data, int period)
        {
            // aroonUp = 100 * (n - offsetHighest(high, n)) / n

            var aroonUp = Create(data.Length);
            for (var index = data.Length - 1; index >= 0; index--)
            {
                var offset = 0;
                var highest = data[index];
                for (var i = 1; i < period; i++)
                {
                    if (data.At(index + i) > highest)
                    {
                        highest = data.At(index + i);
                        offset = i;
                    }
                }

                aroonUp[index] = 100.0 * (period - offset) / period;
            }

            return aroonUp;
        }

        private static double[] ARODown(double[] data, int period)
        {
            // aroonDown = 100 * (n - offsetLowest(low, n)) / n

            var aroonDown = Create(data.Length);
            for (var index = data.Length - 1; index >= 0; index--)
            {
                var offset = 0;
                var lowest = data[index];
                for (var i = 1; i < period; i++)
                {
                    if (data.At(index + i) < lowest)
                    {
                        lowest = data.At(index + i);
                        offset = i;
                    }
                }

                aroonDown[index] = 100.0 * (period - offset) / period;
            }

            return aroonDown;
        }

        private static double[] TR(SymbolInformation symbol, int period)
        {
            // tr  = sum(max(high - low, high - close[1], low - close[1]), n)
            var close = symbol.Close;
            var high = symbol.High;
            var low = symbol.Low;

            var max = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                max[index] = Math.Max(Math.Max(high[index] - low[index], high[index] - close.At(index + 1)), low[index] - close.At(index + 1));
            }

            var tr = Create(close.Length);
            for (var index = close.Length - 1; index >= 0; index--)
            {
                tr[index] = Sum(max, index, period);
            }

            return tr;
        }

        private static double Median(double[] data, int startIndex, int length)
        {
            var workingData = new double[length];

            for (var i = 0; i < length; i++)
            {
                workingData[i] = data.At(startIndex + i);
            }

            Array.Sort(workingData);
            return workingData.Length % 2 == 0
                       ? (workingData[workingData.Length / 2 - 1] + workingData[workingData.Length / 2]) / 2
                       : workingData[workingData.Length / 2];
        }

        private static double[] MD(double[] data, int period)
        {
            // md = md(close, n)

            var median = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                median[index] = Math.Abs(data[index] - Median(data, index, period));
            }

            Debug.Assert(median.Length == data.Length);
            return median;
        }

        private static double[] VWMA(double[] data, int[] volume, int period)
        {
            // vwma = loop((i, res){ res+close[i]*volume[i] }, n) / sum(volume, n)

            var vwma = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var divisor = Sum(volume, index, period);

                var res = 0.0;
                for (var i = 0; i < period; i++)
                {
                    res += data.At(index + i) * volume.At(index + i);
                }

                vwma[index] = res / divisor;
            }

            Debug.Assert(vwma.Length == data.Length);
            return vwma;
        }

        private static double[] VPT(double[] data, int[] volume)
        {
            // vpt = nn(vpt[1],0) + volume*((close-close[1])/close[1])

            var vpt = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                vpt[index] = nn(vpt.At(index + 1)) + volume[index] * ((data[index] - data.At(index + 1)) / data.At(index + 1));
            }

            Debug.Assert(vpt.Length == data.Length);
            return vpt;
        }

        private static double[] OBV(double[] data, int[] volume)
        {
            // obv = nn(close < close[1] ? obv[1]-volume : (close > close[1] ? obv[1]+volume : obv[1]), 0)

            var obv = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                obv[index] = nn(
                    data[index] < data.At(index + 1)
                        ? obv.At(index + 1) - volume[index]
                        : (data[index] > data.At(index + 1) ? obv.At(index + 1) + volume[index] : obv.At(index + 1)));
            }

            Debug.Assert(obv.Length == data.Length);
            return obv;
        }

        private static double[] WMA(double[] data, int period)
        {
            // wma = loop((i, res){ res+close[i]*(n-i) }, n) / ((pow(n, 2)-n)/2+n)

            var wma = Create(data.Length);

            var divisor = (Math.Pow(period, 2) - period) / 2 + period;

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var res = 0.0;
                for (var i = 0; i < period; i++)
                {
                    res += data.At(index + i) * (period - i);
                }

                wma[index] = res / divisor;
            }

            Debug.Assert(wma.Length == data.Length);
            return wma;
        }

        private static double[] DIX(double[] data, int period)
        {
            // n = integer("Period", 28)

            // # calculation
            // mc = sum(close, n) / n
            // dix = skip(100 * (close - mc) / mc, n - 1)

            // # plotting
            // plotThreshold("threshold", dix, 0, 0)("legend", 0)
            // plotLine("line", dix)

            var dix = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var mc = Sum(data, index, period) / period;
                dix[index] = 100 * (data[index] - mc) / mc;
            }

            Debug.Assert(dix.Length == data.Length);
            return dix;
        }

        private static double[] ROC(double[] data, int period)
        {
            // roc = close[n] == 0? 0 : 100 * (close - close[n]) / close[n]

            var roc = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                roc[index] = 100 * (data[index] - data.At(index + period)) / data.At(index + period);
            }

            Debug.Assert(roc.Length == data.Length);
            return roc;
        }

        private static double[] MOM(double[] data, int period)
        {
            // mom = close - close[n]

            var mom = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                mom[index] = data[index] - data.At(index + period);
            }

            Debug.Assert(mom.Length == data.Length);
            return mom;
        }

        private static double[] HH(double[] data, int period)
        {
            var hh = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var highest = data[index];
                for (var i = 0; i < period; i++)
                {
                    highest = Math.Max(data.At(index + i), highest);
                }

                hh[index] = highest;
            }

            Debug.Assert(hh.Length == data.Length);
            return hh;
        }

        private static double[] LL(double[] data, int period)
        {
            var ll = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                var lowest = data[index];
                for (var i = 0; i < period; i++)
                {
                    lowest = Math.Min(data.At(index + i), lowest);
                }

                ll[index] = lowest;
            }

            Debug.Assert(ll.Length == data.Length);
            return ll;
        }

        private static double[] TMA(double[] data, int period)
        {
            // a = n%2 == 0? n/2 : (n+1)/2
            // b = n % 2 == 0 ? a + 1 : a
            // tma = sma(sma(close, a), b)

            var a = period % 2 == 0 ? period / 2 : (period + 1) / 2;
            var b = period % 2 == 0 ? a + 1 : a;

            var sma = SMA(SMA(data, a), b);

            var tma = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                tma[index] = sma[index];
            }

            Debug.Assert(tma.Length == data.Length);
            return tma;
        }

        private static double[] STDEV(double[] data, int period)
        {
            // stdev(close, n)
            var stdev = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                stdev[index] = Stdev(data, index, period);
            }

            Debug.Assert(stdev.Length == data.Length);
            return stdev;
        }

        private static (double[] Upper, double[] Lower, double[] Middle) BB(double[] data, int period, double factor)
        {
            // # input
            // n = integer("Period", 20)
            // f = float("Factor", 2)

            // # calculation
            // middle = sma(close, n)
            // upper = middle + f * stdev(close, n)
            // lower = middle - f * stdev(close, n)

            var middle = SMA(data, period);
            var upper = Create(data.Length);
            var lower = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                upper[index] = middle[index] + factor * Stdev(data, index, period);
                lower[index] = middle[index] - factor * Stdev(data, index, period);
            }

            Debug.Assert(middle.Length == data.Length);
            return (upper, lower, middle);
        }

        private static double[] RSL(double[] data, int period)
        {
            // rsl = close / sma(close, n)

            var sma = SMA(data, period);

            var rsl = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                rsl[index] = data[index] / sma[index];
            }

            Debug.Assert(rsl.Length == data.Length);
            return rsl;
        }

        private static double[] KAMA(double[] data, int length)
        {
            // # input
            // length = integer("length", 21)
            // nFastend = 0.666
            // nSlowend = 0.0645

            // # calculation
            // xPrice = close

            // xvNoise = abs(xPrice - xPrice[1])
            // nSignal = abs(xPrice - xPrice[length])
            // nNoise = sum(xvNoise, length)

            // nefRatio = nNoise != 0 ? nSignal / nNoise : 0
            // nSmooth = pow(nefRatio * (nFastend - nSlowend) + nSlowend, 2)
            // nAMA = nn(nAMA[1]) + nSmooth * (xPrice - nn(nAMA[1]))

            var nFastend = 0.666;
            var nSlowend = 0.0645;
            var xPrice = data;

            var kama = Create(data.Length);
            var xvNoise = Create(data.Length);
            var nSignal = Create(data.Length);

            for (var index = data.Length - 1; index >= 0; index--)
            {
                xvNoise[index] = Math.Abs(xPrice[index] - xPrice.At(index + 1));
                nSignal[index] = Math.Abs(xPrice[index] - xPrice.At(index + length));
                var nNoise = Sum(xvNoise, index, length);

                var nefRatio = nSignal[index] / nNoise;
                var nSmooth = Math.Pow(nefRatio * (nFastend - nSlowend) + nSlowend, 2);

                kama[index] = nn(nn(kama.At(index + 1)) + nSmooth * (xPrice[index] - nn(kama.At(index + 1))), xPrice[index]);
            }

            Debug.Assert(kama.Length == data.Length);
            return kama;
        }

        private static double Stdev(double[] data, int startIndex, int length)
        {
            var sum = 0.0;
            for (var index = startIndex; index < startIndex + length; index++)
            {
                sum += data.At(index);
            }

            var average = sum / length;
            var sumOfDerivation = 0.0;
            for (var index = startIndex; index < startIndex + length; index++)
            {
                sumOfDerivation += (data.At(index) - average) * (data.At(index) - average);
            }

            return Math.Sqrt(sumOfDerivation / (length - 1));
        }

        private static double At(this double[] data, int index)
        {
            return (index >= 0) && (index < data.Length) ? data[index] : double.NaN;
        }

        private static int At(this int[] data, int index, int defaultValue = 0)
        {
            return (index >= 0) && (index < data.Length) ? data[index] : defaultValue;
        }

        private static double nn(double value, double defaultValue = 0)
        {
            return double.IsNaN(value) ? defaultValue : value;
        }

        private static double[] Create(int length, double value = double.NaN)
        {
            var values = new double[length];

            for (var index = length - 1; index >= 0; index--)
            {
                values[index] = value;
            }

            return values;
        }

        private static double Sum(double[] data, int startIndex, int length, Func<double, double> summandFunc = null)
        {
            var result = 0.0;
            for (var index = startIndex; index < startIndex + length; index++)
            {
                result += summandFunc?.Invoke(data.At(index)) ?? data.At(index);
            }

            return result;
        }

        private static double Sum(int[] data, int startIndex, int length)
        {
            var result = 0.0;
            for (var index = startIndex; index < startIndex + length; index++)
            {
                result += data.At(index);
            }

            return result;
        }
    }
}