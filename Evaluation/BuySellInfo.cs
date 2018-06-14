using System;

internal class Trade
{
    internal double BuyValue { get; set; }
    internal double SellValue { get; set; }
    internal DateTime BuyDate { get; set; }
    internal DateTime SellDate { get; set; }

    internal double Gain => BuyValue > 0 && SellValue > 0 ? (SellValue - BuyValue) / BuyValue * 100 : 0;

    public override string ToString()
    {
        return $"{Gain:F}";
    }
}
