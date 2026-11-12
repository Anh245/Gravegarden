using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MixedWaveSet", menuName = "GraveGarden/Waves/Mixed Wave Set")]
public class MixedWaveSetDefinition : ScriptableObject
{
    [Serializable]
    public class SpawnGroup
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField, Min(1)] private int count = 3;
        [SerializeField, Min(0.1f)] private float spawnInterval = 2f;
        [SerializeField, Min(0f)] private float startDelay;

        public GameObject EnemyPrefab => enemyPrefab;
        public int Count => count;
        public float SpawnInterval => spawnInterval;
        public float StartDelay => startDelay;
    }

    [Serializable]
    public class Wave
    {
        [SerializeField] private string displayName = "Đợt mới";
        [SerializeField] private SpawnGroup[] groups = new SpawnGroup[0];

        [SerializeField, Min(0)] private int completionReward;
        public int CompletionReward => completionReward;
        public string DisplayName => displayName;
        public SpawnGroup[] Groups => groups;
    }

    public sealed class SpawnInstruction
    {
        public GameObject Prefab { get; }
        public float DelayBeforeSpawn { get; }

        public SpawnInstruction(GameObject prefab, float delayBeforeSpawn)
        {
            Prefab = prefab;
            DelayBeforeSpawn = delayBeforeSpawn;
        }
    }

    [SerializeField] private Wave[] waves = new Wave[0];

    public int CompletionRewardAt(int index)=>waves!=null && index>=0 && index<waves.Length && waves[index]!=null
        ? waves[index].CompletionReward:0;

    // Tạo bản kế hoạch riêng cho trận; không ghi tiến trình vào asset.
    public bool TryCreatePlans(out SpawnInstruction[][] plans, out string error)
    {
        plans = null;
        error = null;

        if (waves == null || waves.Length == 0)
        {
            error = "Chưa cấu hình đợt nào.";
            return false;
        }

        var result = new SpawnInstruction[waves.Length][];

        for (int w = 0; w < waves.Length; w++)
        {
            Wave wave = waves[w];
            if (wave == null || wave.Groups == null || wave.Groups.Length == 0)
            {
                error = $"Đợt {w + 1}: chưa có nhóm quái.";
                return false;
            }

            if(wave.CompletionReward<0)
            {error=$"Đợt {w+1}: thưởng hoàn thành không hợp lệ.";return false;}
            var entries = new List<SpawnInstruction>();

            for (int g = 0; g < wave.Groups.Length; g++)
            {
                SpawnGroup group = wave.Groups[g];
                string location = $"Đợt {w + 1}, nhóm {g + 1}";

                if (group == null || group.EnemyPrefab == null)
                {
                    error = $"{location}: chưa gán prefab quái.";
                    return false;
                }

                if (group.Count < 1
                    || !IsFinite(group.SpawnInterval) || group.SpawnInterval < 0.1f
                    || !IsFinite(group.StartDelay) || group.StartDelay < 0f)
                {
                    error = $"{location}: số lượng hoặc thời gian không hợp lệ.";
                    return false;
                }

                GameObject prefab = group.EnemyPrefab;
                EnemyHealth health = prefab.GetComponent<EnemyHealth>();
                EnemyRouteFollower movement = prefab.GetComponent<EnemyRouteFollower>();
                EnemySiegeAttack siege = prefab.GetComponent<EnemySiegeAttack>();

                if (!prefab.activeSelf
                    || health == null || !health.enabled || health.Definition == null
                    || movement == null || !movement.enabled
                    || siege == null || !siege.enabled)
                {
                    error = $"{location}: prefab phải bật, có EnemyHealth và Definition, " +
                            "EnemyRouteFollower, EnemySiegeAttack đang bật trên đối tượng cha.";
                    return false;
                }

                for (int i = 0; i < group.Count; i++)
                {
                    float delay = i == 0 ? group.StartDelay : group.SpawnInterval;
                    entries.Add(new SpawnInstruction(prefab, delay));
                }
            }

            result[w] = entries.ToArray();
        }

        plans = result;
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
