using System;
public enum ShopCardState { Affordable, Unaffordable, Locked, Selected, Placed }
public static class ShopCardPolicy
{
    public static ShopCardState State(bool unlocked,bool affordable,bool selected,bool justPlaced)
        => !unlocked?ShopCardState.Locked:justPlaced?ShopCardState.Placed
        :!affordable?ShopCardState.Unaffordable:selected?ShopCardState.Selected:ShopCardState.Affordable;
    public static int PurchasePrice(int baseCost,bool producer,int copies)
    {
        if(baseCost<=0 || copies<0) return int.MaxValue;
        if(!producer) return baseCost;
        double price=baseCost*Math.Pow(1.18,copies);
        if(double.IsNaN(price) || double.IsInfinity(price) || price>=int.MaxValue) return int.MaxValue;
        return Math.Max(1,(int)Math.Round(price,MidpointRounding.AwayFromZero));
    }
}
