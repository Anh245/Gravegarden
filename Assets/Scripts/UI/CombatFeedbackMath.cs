using System;
public static class CombatFeedbackMath
{
    public static float HealthFraction(float current,float maximum)
    {
        if(float.IsNaN(current) || float.IsInfinity(current) || float.IsNaN(maximum)
            || float.IsInfinity(maximum) || maximum<=0f) return 0f;
        return Math.Max(0f,Math.Min(1f,current/maximum));
    }
}
public sealed class DamageBurst
{
    private double pending;
    private double elapsed;
    public void Add(double amount)
    {
        if(amount>0d && !double.IsNaN(amount) && !double.IsInfinity(amount)
            && pending<=double.MaxValue-amount) pending+=amount;
    }
    public bool Advance(double deltaSeconds,bool force,out double damage)
    {
        damage=0d;
        if(double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds<0d) return false;
        elapsed+=deltaSeconds;
        if(pending<=0d || (!force && elapsed<0.12d)) return false;
        damage=pending;pending=0d;elapsed=0d;return true;
    }
}
