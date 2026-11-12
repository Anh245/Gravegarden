using System;
using System.Collections.Generic;
public static class AudioMixMath
{
    public static float Unit(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, Math.Min(1f,value));
    public static float Gain(float master,float channel,bool muted) => muted ? 0f : Unit(master)*Unit(channel);
    public static float Calm(float mix) => (float)Math.Sqrt(1f-Unit(mix));
    public static float Battle(float mix) => (float)Math.Sqrt(Unit(mix));
}
public sealed class SoundCooldown
{
    private readonly Dictionary<GameSound,double> last = new Dictionary<GameSound,double>();
    public bool TryEnter(GameSound sound,double now,double interval)
    {
        if (double.IsNaN(now) || double.IsInfinity(now) || now<0d
            || double.IsNaN(interval) || double.IsInfinity(interval) || interval<0d) return false;
        if (last.TryGetValue(sound,out double previous) && now>=previous && now-previous<interval) return false;
        last[sound]=now;
        return true;
    }
}
