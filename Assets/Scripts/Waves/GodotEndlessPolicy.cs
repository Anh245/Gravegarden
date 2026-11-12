using System;

public static class GodotEndlessPolicy
{
    public enum Kind { Rotling, BoneHound, BogBrute, Wraith, OssuaryGiant }
    // Giới hạn biểu diễn phần thưởng bằng int của PlayerWallet, không phải giới hạn thiết kế wave.
    public const int MaxSupportedWave = (int.MaxValue - 120) / 40;
    public sealed class Spec
    {
        public readonly int Wave, BaseCount, Reward;
        public readonly float Interval, HealthMultiplier;
        public int TotalCount => BaseCount * 2;
        public Spec(int wave, int count, float interval, float health, int reward)
        {Wave=wave;BaseCount=count;Interval=interval;HealthMultiplier=health;Reward=reward;}
        public Kind[] SpawnOrder()
        {
            var result=new Kind[TotalCount];
            for(int i=0;i<BaseCount;i++)
            {
                Kind kind=Wave%5==0 && i<3 ? Kind.OssuaryGiant : (Kind)(i%4);
                result[i*2]=kind;result[i*2+1]=kind;
            }
            return result;
        }
    }
    public static Spec Wave(int number)
    {
        if(number<6 || number>MaxSupportedWave)throw new ArgumentOutOfRangeException(nameof(number));
        long completed=(long)number-1;
        int count=(int)Math.Min(96L,44L+(completed-4)*8L);
        float interval=(float)Math.Max(.325,.625-(completed-5)*.0175);
        float health=(float)(1.0+Math.Max(0L,completed-4)*.08);
        return new Spec(number,count,interval,health,checked(120+number*40));
    }
}
