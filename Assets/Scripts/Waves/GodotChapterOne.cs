using System;
using System.Collections.Generic;

// Dữ liệu đối chiếu từ wave_1..5.tres và luồng runtime game_director.gd/enemy.gd.
public static class GodotChapterOne
{
    public sealed class WaveSpec
    {
        public readonly string[] Ids;
        public readonly int[] Counts;
        public readonly float Interval;
        public readonly int Reward;
        public WaveSpec(string[] ids,int[] counts,float interval,int reward)
        {Ids=ids;Counts=counts;Interval=interval;Reward=reward;}
        public string[] SpawnOrder()
        {
            var remaining=(int[])Counts.Clone();var result=new List<string>();bool more=true;
            while(more)
            {
                more=false;
                for(int i=0;i<remaining.Length;i++)if(remaining[i]>0)
                {result.Add(Ids[i]);result.Add(Ids[i]);remaining[i]--;more=true;}
            }
            return result.ToArray();
        }
    }
    public sealed class EnemySpec
    {
        public readonly string Id,PrefabName;
        public readonly float Health,Speed,SlowMultiplier;
        public readonly int Bounty,HeartDamage;
        public EnemySpec(string id,string prefab,float health,float speed,int bounty,int damage,float slow=1f)
        {Id=id;PrefabName=prefab;Health=health;Speed=speed;Bounty=bounty;HeartDamage=damage;SlowMultiplier=slow;}
    }
    public static EnemySpec[] Enemies()=>new[]{
        new EnemySpec("rotling","Rotling",60f,0.64f,8,1),
        new EnemySpec("bone_hound","BoneHound",36f,1.3f,10,1),
        new EnemySpec("bog_brute","BogBrute",240f,0.48f,22,2),
        new EnemySpec("wraith","Wraith",110f,0.76f,16,1,0.7f),
        new EnemySpec("ossuary_giant","OssuaryGiant",1600f,0.32f,250,5)
    };
    public static WaveSpec Wave(int number)
    {
        switch(number)
        {
            case 1:return new WaveSpec(new[]{"bone_hound","rotling"},new[]{4,8},1.25f,160);
            case 2:return new WaveSpec(new[]{"bog_brute","bone_hound","rotling","wraith"},new[]{2,4,8,4},1.125f,200);
            case 3:return new WaveSpec(new[]{"bog_brute","bone_hound","rotling","wraith"},new[]{3,6,10,5},1f,240);
            case 4:return new WaveSpec(new[]{"bog_brute","bone_hound","rotling","wraith"},new[]{5,8,10,7},0.875f,280);
            case 5:return new WaveSpec(new[]{"bog_brute","bone_hound","ossuary_giant","rotling","wraith"},new[]{4,6,1,8,6},0.75f,320);
            default:throw new ArgumentOutOfRangeException(nameof(number));
        }
    }
}
