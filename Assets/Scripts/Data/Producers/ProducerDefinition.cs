using UnityEngine;

[CreateAssetMenu(
    fileName = "Producer_New",
    menuName = "Gravegarden/Data/Producer"
)]
public class ProducerDefinition : ScriptableObject
{
    [Header("Thông tin")]
    [SerializeField]
    private string displayName = "Vườn Xương";

    [Header("Xây dựng")]
    [SerializeField, Min(1)]
    private int baseBuildCost = 120;

    [SerializeField, Min(0)]
    private int unlockWave = 0;

    [Header("Sản xuất ở cấp 1")]
    [SerializeField, Min(0.01f)]
    private float productionPerSecond = 1.6f;

    [SerializeField, Min(1)]
    private int capacity = 1440;

    public string DisplayName => displayName;
    public int BaseBuildCost => baseBuildCost;
    public int UnlockWave => unlockWave;
    public float ProductionPerSecond => productionPerSecond;
    public int Capacity => capacity;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(displayName) &&
        baseBuildCost > 0 &&
        unlockWave >= 0 &&
        productionPerSecond > 0f &&
        capacity > 0;


    public int MaxLevel => 3;

    public float GetProductionAtLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);

        float multiplier;

        switch (level)
        {
            case 2:
                multiplier = 1.6f;
                break;

            case 3:
                multiplier = 2.5f;
                break;

            default:
                multiplier = 1f;
                break;
        }

        return ProductionPerSecond * multiplier;
    }

    public int GetCapacityAtLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);

        int multiplier;

        switch (level)
        {
            case 2:
                multiplier = 3;
                break;

            case 3:
                multiplier = 8;
                break;

            default:
                multiplier = 1;
                break;
        }

        return Capacity * multiplier;
    }

    public int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel < 1 || currentLevel >= MaxLevel)
            return 0;

        float multiplier = currentLevel == 1 ? 1.5f : 3f;

        return Mathf.Max(
            1,
            Mathf.RoundToInt(BaseBuildCost * multiplier));
    }
}