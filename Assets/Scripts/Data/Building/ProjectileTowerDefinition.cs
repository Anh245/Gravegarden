using UnityEngine;

[CreateAssetMenu(
    fileName ="Tower_New",
    menuName = "Gravegarden/Data/Projectile Tower"
)]
public class ProjectileTowerDefinition : ScriptableObject
{

    [Header("Mở khóa")]
    [SerializeField, Min(0)]
    private int unlockWave = 0;

    public int UnlockWave => unlockWave;

    [Header("Thong tin")]
    [SerializeField]
    private string displayName = "Hoa Xương";

    [Header("Xây dựng")]
    [SerializeField, Min(1)]
    private int buildCost = 150;

    [Header("Chiến đấu")]
    [SerializeField, Min(0.01f)]
    private float damage = 18f;

    [SerializeField, Min(0.01f)]
    private float range = 3.6f;

    [SerializeField, Min(0.01f)]
    private float attackInterval = 0.8f;

    public string DisplayName => displayName;
    public int BuildCost => buildCost;
    public float Damage => damage;
    public float Range => range;
    public float AttackInterval => attackInterval;


    public bool IsValid =>
     !string.IsNullOrWhiteSpace(displayName) &&
     buildCost > 0 &&
     damage > 0f &&
     range > 0f &&
     attackInterval > 0f &&
     unlockWave >= 0;

    public int MaxLevel => 3;

    public int GetDamageAtLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);

        float multiplier;

        switch (level)
        {
            case 2:
                multiplier = 1.65f;
                break;

            case 3:
                multiplier = 2.6f;
                break;

            default:
                multiplier = 1f;
                break;
        }

        return Mathf.Max(1, Mathf.RoundToInt(Damage * multiplier));
    }

    public int GetUpgradeCost(int currentLevel)
    {
        // Giá bằng 0 biểu thị không có cấp tiếp theo.
        if (currentLevel < 1 || currentLevel >= MaxLevel)
            return 0;

        float multiplier = currentLevel == 1 ? 1.5f : 3f;

        return Mathf.Max(1, Mathf.RoundToInt(BuildCost * multiplier));
    }
}
