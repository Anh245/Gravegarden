using UnityEngine;

[CreateAssetMenu(fileName="GodotEndless", menuName="GraveGarden/Waves/Godot Endless")]
public class GodotEndlessWaves : ScriptableObject
{
    [SerializeField] private GameObject rotling;
    [SerializeField] private GameObject boneHound;
    [SerializeField] private GameObject bogBrute;
    [SerializeField] private GameObject wraith;
    [SerializeField] private GameObject ossuaryGiant;

    private GameObject Prefab(GodotEndlessPolicy.Kind kind)
    {
        switch(kind)
        {
            case GodotEndlessPolicy.Kind.Rotling:return rotling;
            case GodotEndlessPolicy.Kind.BoneHound:return boneHound;
            case GodotEndlessPolicy.Kind.BogBrute:return bogBrute;
            case GodotEndlessPolicy.Kind.Wraith:return wraith;
            default:return ossuaryGiant;
        }
    }
    public string CheckpointSignature()
    {
        var text=new System.Text.StringBuilder("godot-endless-v1");
        for(int i=0;i<5;i++)
        {
            GameObject prefab=Prefab((GodotEndlessPolicy.Kind)i);
            text.Append('|').Append(prefab.name).Append(':').Append(JsonUtility.ToJson(prefab.GetComponent<EnemyHealth>().Definition));
        }
        return CheckpointHash.FromText(text.ToString());
    }

    public bool TryValidate(out string error)
    {
        for(int i=0;i<5;i++)
        {
            GameObject prefab=Prefab((GodotEndlessPolicy.Kind)i);
            var health=prefab==null?null:prefab.GetComponent<EnemyHealth>();
            var move=prefab==null?null:prefab.GetComponent<EnemyRouteFollower>();
            var siege=prefab==null?null:prefab.GetComponent<EnemySiegeAttack>();
            if(prefab==null || !prefab.activeSelf || health==null || !health.enabled
                || health.Definition==null || !health.Definition.IsValid || move==null || !move.enabled || siege==null || !siege.enabled)
            {error="GodotEndless: prefab "+(GodotEndlessPolicy.Kind)i+" thiếu hoặc chưa hợp lệ.";return false;}
        }
        error=null;return true;
    }
    public bool TryCreatePlan(int number,out MixedWaveSetDefinition.SpawnInstruction[] plan,
        out float multiplier,out int reward,out string error)
    {
        plan=null;multiplier=1;reward=0;
        if(!TryValidate(out error))return false;
        if(number<6 || number>GodotEndlessPolicy.MaxSupportedWave)
        {error="Số đợt vượt giới hạn biểu diễn tiền thưởng.";return false;}
        var spec=GodotEndlessPolicy.Wave(number);var order=spec.SpawnOrder();
        plan=new MixedWaveSetDefinition.SpawnInstruction[order.Length];
        for(int i=0;i<order.Length;i++)plan[i]=new MixedWaveSetDefinition.SpawnInstruction(Prefab(order[i]),i==0?0:spec.Interval);
        multiplier=spec.HealthMultiplier;reward=spec.Reward;return true;
    }
}
