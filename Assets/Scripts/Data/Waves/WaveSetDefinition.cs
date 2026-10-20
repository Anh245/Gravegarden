using NUnit.Framework.Internal;
using UnityEngine;


[CreateAssetMenu(
        fileName = "WaveSet_New",
        menuName = "Gravegarden/Data/Wave Set"
)]

public class WaveSetDefinition : ScriptableObject
{
    [Header("Thông tin")]
    [SerializeField] private string displayName = "Bộ wave mới";


    [Header("Số địch trong từng đợt")]
    [SerializeField]
    private int[] enemiesPerWave = { 5, 8, 12 };

    [Header("Nhịp sinh địch")]
    [SerializeField, Min(0.1f)]
    private float spawnInterval = 2f;

    public string DisplayName => displayName;

    public float SpawnInterval => spawnInterval;

    public bool IsValid
    {
        get
        {
            if (enemiesPerWave == null || enemiesPerWave.Length == 0)
            {
                return false;
            }

            foreach(int count in enemiesPerWave)
            {
                if( count <= 0)
                {
                    return false;
                }
            }

            return spawnInterval >= 0.1f;
        }
    }

    public int[] CreateEnemyCountsCopy()
    {
        if( enemiesPerWave == null)
        {
            return new int[0];
        }

        return (int[])enemiesPerWave.Clone();
    }
}
