using UnityEngine;

[CreateAssetMenu(
    fileName ="Tower_New",
    menuName = "Gravegarden/Data/Projectile Tower"
)]
public class ProjectileTowerDefinition : ScriptableObject
{
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
        attackInterval > 0f;
}
