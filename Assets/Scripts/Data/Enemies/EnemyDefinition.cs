using UnityEngine;

[CreateAssetMenu(
    fileName = "Enemy_New",
    menuName = "Gravegarden/Data/Enemy"
)]
public class EnemyDefinition : ScriptableObject
{
    [Header("Thông tin")]
    [SerializeField]
    private string displayName = "Rotling";

    [Header("Máu và phần thưởng")]
    [SerializeField, Min(1f)]
    private float maxHealth = 60f;

    [SerializeField, Min(0)]
    private int bounty = 8;

    [Header("Di chuyển")]
    [SerializeField, Min(0.01f)]
    private float moveSpeed = 0.64f;

    [Header("Tấn công nhà chính")]
    [SerializeField, Min(1)]
    private int siegeDamage = 1;

    [SerializeField, Min(0.01f)]
    private float firstHitDelay = 0.65f;

    [SerializeField, Min(0.01f)]
    private float attackInterval = 1.6f;

    public string DisplayName => displayName;
    public float MaxHealth => maxHealth;
    public int Bounty => bounty;
    public float MoveSpeed => moveSpeed;
    public int SiegeDamage => siegeDamage;
    public float FirstHitDelay => firstHitDelay;
    public float AttackInterval => attackInterval;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(displayName) &&
        maxHealth >= 1f &&
        bounty >= 0 &&
        moveSpeed > 0f &&
        siegeDamage >= 1 &&
        firstHitDelay > 0f &&
        attackInterval > 0f;
}