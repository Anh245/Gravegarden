using System;

// Đơn vị giây gameplay; caller quyết định lúc được chạy.
public sealed class PreparationClock
{
    public const double MaximumSeconds=45;
    public double Remaining { get; private set; }
    public bool Armed { get; private set; }
    public int EarlyReward => Armed ? (int)Math.Floor(Remaining*4) : 0;
    public static double Duration(int completed,bool expanded)
    {
        if(completed<0)throw new ArgumentOutOfRangeException(nameof(completed));
        return !expanded?20:completed==5?45:30;
    }
    public static bool Valid(double seconds)=>!double.IsNaN(seconds)&&!double.IsInfinity(seconds)&&seconds>=0&&seconds<=MaximumSeconds;
    public void Reset(double seconds)
    {
        if(!Valid(seconds))throw new ArgumentOutOfRangeException(nameof(seconds));
        Remaining=seconds;Armed=true;
    }
    public bool Tick(double delta,bool allowed)
    {
        if(delta<0 || double.IsNaN(delta)||double.IsInfinity(delta))throw new ArgumentOutOfRangeException(nameof(delta));
        if(!Armed || !allowed)return false;
        Remaining=Math.Max(0,Remaining-delta);
        return Remaining<=0;
    }
    public void Consume(){Remaining=0;Armed=false;}
}
