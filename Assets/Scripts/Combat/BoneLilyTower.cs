using UnityEngine;

public class BoneLilyTower : MonoBehaviour
{
    [Header("Dữ liệu")]
    [SerializeField]
    private ProjectileTowerDefinition definition;

    [Header("Tham chiếu trong prefab")]
    [SerializeField] private Animator visualAnimator;
    [SerializeField] private Transform muzzle;
    [SerializeField] private BoneProjectile projectilePrefab;

    [Header("Tham chiếu trong scene")]
    [SerializeField] private TownHallHealth defendedHall;

    [Header("Tìm mục tiêu")]
    [SerializeField, Min(0.01f)]
    private float retrySearchInterval = 0.1f;

    private float damage;
    private float range;
    private float attackInterval;

    private float cooldown;
    private float searchDelay;
    private bool initialized;
    private float supportBonus;
    private float nextSupportCheck;

    public float CurrentEffectiveRange => (initialized ? range : definition!=null ? definition.Range : 0f) + GetFootprintRadius();
    public float CurrentSupportBonus => supportBonus;
    public float CurrentAttackInterval => attackInterval / (1f + supportBonus);

    public ProjectileTowerDefinition Definition => definition;
    public bool CheckpointReady => initialized && isActiveAndEnabled;
    public bool RestoreLevel(int savedLevel)
    {
        if (definition == null || !definition.IsValid || savedLevel < 1 || savedLevel > definition.MaxLevel
            || visualAnimator == null || visualAnimator.runtimeAnimatorController == null
            || muzzle == null || projectilePrefab == null)
            return false;
        Level = savedLevel;
        if (initialized) damage = definition.GetDamageAtLevel(Level);
        return true;
    }

    public int Level { get; private set; } = 1;

    public bool IsMaxLevel =>
        definition != null && Level >= definition.MaxLevel;

    public float CurrentDamage => damage;

    public int NextUpgradeCost =>
        definition != null ? definition.GetUpgradeCost(Level) : 0;

    public bool CanUpgradeNow(WaveDirector waveDirector)
    {
        return initialized
            && isActiveAndEnabled
            && definition != null
            && defendedHall != null
            && !defendedHall.IsDestroyed
            && waveDirector != null
            && waveDirector.CanBuildNow
            && Time.timeScale > 0f
            && !IsMaxLevel;
    }

    public bool TryUpgrade(
        PlayerWallet wallet,
        WaveDirector waveDirector)
    {
        if (wallet == null || !CanUpgradeNow(waveDirector))
            return false;

        int cost = NextUpgradeCost;

        if (cost <= 0 || !wallet.TrySpend(cost))
            return false;

        GetComponent<BuildingPlacement>()?.RecordAdditionalInvestment(cost);
        Level++;
        GameAudio.Play(GameSound.Upgrade);
        damage = definition.GetDamageAtLevel(Level);

        Debug.Log(
            $"{definition.DisplayName} đã lên cấp {Level}. " +
            $"Sát thương: {damage}. Chi phí: {cost}.",
            this);

        return true;
    }

    public void Configure(TownHallHealth newDefendedHall)
    {
        defendedHall = newDefendedHall;
    }

    private void Start()
    {
        if (definition == null || !definition.IsValid)
        {
            Debug.LogError(
                "Bone Lily thiếu dữ liệu hoặc dữ liệu không hợp lệ.",
                this
            );

            enabled = false;
            return;
        }

        if (visualAnimator == null ||
            muzzle == null ||
            projectilePrefab == null ||
            defendedHall == null)
        {
            Debug.LogError(
                "Bone Lily chưa được gán đủ tham chiếu.",
                this
            );

            enabled = false;
            return;
        }

        damage = definition.GetDamageAtLevel(Level);
        range = definition.Range;
        attackInterval = definition.AttackInterval;

        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        if (defendedHall.IsDestroyed)
        {
            visualAnimator.speed = 0f;
            return;
        }

        if (Time.timeScale <= 0f)
            return;

        // Kiểm tra aura 5 lần/giây; nhiều aura chỉ lấy mức mạnh nhất.
        if (Time.time >= nextSupportCheck)
        {
            supportBonus = SupportBuilding.GetStrongestBonusAt(transform.position, defendedHall);
            nextSupportCheck = Time.time + 0.2f;
        }

        cooldown = Mathf.Max(
            0f,
            cooldown - Time.deltaTime * (1f + supportBonus)
        );

        searchDelay = Mathf.Max(
            0f,
            searchDelay - Time.deltaTime
        );

        if (cooldown > 0f || searchDelay > 0f)
            return;

        EnemyHealth target = FindBestTarget();

        if (target == null)
        {
            searchDelay = retrySearchInterval;
            return;
        }

        Fire(target);

        cooldown = attackInterval;
    }

    private EnemyHealth FindBestTarget()
    {
        EnemyHealth[] enemies =
            FindObjectsByType<EnemyHealth>();

        EnemyHealth best = null;
        float bestProgress = -1f;
        float bestDistance = float.PositiveInfinity;

        Vector2 origin = transform.position;
        float effectiveRange = CurrentEffectiveRange;
        float rangeSquared = effectiveRange * effectiveRange;

        foreach (EnemyHealth candidate in enemies)
        {
            if (!candidate.IsAlive)
                continue;

            EnemyRouteFollower movement =
                candidate.GetComponent<EnemyRouteFollower>();

            if (movement == null || !movement.IsReady)
                continue;

            Vector2 aimPosition = candidate.transform.position;

            float distance =
                (aimPosition - origin).sqrMagnitude;

            if (distance > rangeSquared)
                continue;

            float progress = movement.Progress;

            bool shouldChoose = best == null;

            if (best != null)
            {
                if (!Mathf.Approximately(progress, bestProgress))
                {
                    shouldChoose = progress > bestProgress;
                }
                else if (!Mathf.Approximately(distance, bestDistance))
                {
                    shouldChoose = distance < bestDistance;
                }
                else
                {
                    shouldChoose =
                        candidate.GetEntityId().CompareTo(
                            best.GetEntityId()
                        ) < 0;
                }
            }

            if (!shouldChoose)
                continue;

            best = candidate;
            bestProgress = progress;
            bestDistance = distance;
        }

        return best;
    }

    private void Fire(EnemyHealth target)
    {
        visualAnimator.Play(
            "Base Layer.Attack",
            0,
            0f
        );

        BoneProjectile projectile = Instantiate(
            projectilePrefab,
            muzzle.position,
            Quaternion.identity
        );

        projectile.Initialize(
            target,
            damage,
            defendedHall,
            range
        );
        GameAudio.Play(GameSound.Shot);
    }

    private float GetFootprintRadius()
    {
        BuildingPlacement placement = GetComponent<BuildingPlacement>();
        return placement != null && placement.IsPlaced ? placement.HalfSide
            : definition != null ? definition.Footprint * 0.64f : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (definition == null || !definition.IsValid)
            return;

        Gizmos.color = Color.green;

        float displayedRange =
            Application.isPlaying && initialized
                ? range
                : definition.Range;
        displayedRange += GetFootprintRadius();

        Gizmos.DrawWireSphere(
            transform.position,
            displayedRange
        );
    }
}