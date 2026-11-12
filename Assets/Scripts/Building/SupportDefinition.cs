using UnityEngine;

[CreateAssetMenu(fileName = "SpiritScarecrow_Data", menuName = "Gravegarden/Data/Support Building")]
public class SupportDefinition : ScriptableObject
{
    [Header("Kích thước chiếm ô - theo Godot")]
    [SerializeField, Range(1, 2)] private int footprint = 1;
    public int Footprint => footprint;
    public int FootprintCells => footprint * 2;

    [SerializeField] private string displayName = "Bù Nhìn Linh Hồn";
    [SerializeField, Min(1)] private int buildCost = 260;
    [SerializeField, Min(0)] private int unlockWave = 4;
    [SerializeField, Min(0.01f)] private float range = 3.2f;
    [SerializeField, Range(0f, 1f)] private float baseAttackSpeedBonus = 0.25f;
    [SerializeField, Range(0f, 1f)] private float bonusPerLevel = 0.10f;

    public string DisplayName => displayName;
    public int BuildCost => buildCost;
    public int UnlockWave => unlockWave;
    public float Range => range;
    public int MaxLevel => 3;
    public bool IsValid => (footprint == 1 || footprint == 2) && !string.IsNullOrWhiteSpace(displayName)
        && buildCost > 0 && unlockWave >= 0
        && !float.IsNaN(range) && !float.IsInfinity(range) && range > 0f
        && !float.IsNaN(baseAttackSpeedBonus) && !float.IsInfinity(baseAttackSpeedBonus)
        && baseAttackSpeedBonus >= 0f && baseAttackSpeedBonus <= 1f
        && !float.IsNaN(bonusPerLevel) && !float.IsInfinity(bonusPerLevel)
        && bonusPerLevel >= 0f && bonusPerLevel <= 1f;

    public float GetBonusAtLevel(int level)
    {
        return baseAttackSpeedBonus + bonusPerLevel * (Mathf.Clamp(level, 1, MaxLevel) - 1);
    }

    public int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel < 1 || currentLevel >= MaxLevel)
            return 0;
        return Mathf.RoundToInt(buildCost * (currentLevel == 1 ? 1.5f : 3f));
    }
}
