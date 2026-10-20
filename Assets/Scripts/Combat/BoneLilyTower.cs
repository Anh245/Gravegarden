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

    public ProjectileTowerDefinition Definition => definition;

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

        damage = definition.Damage;
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

        cooldown = Mathf.Max(
            0f,
            cooldown - Time.deltaTime
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
        float rangeSquared = range * range;

        foreach (EnemyHealth candidate in enemies)
        {
            if (!candidate.IsAlive)
                continue;

            EnemyRouteFollower movement =
                candidate.GetComponent<EnemyRouteFollower>();

            if (movement == null || !movement.IsReady)
                continue;

            Vector2 aimPosition = candidate.AimPosition;

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
            defendedHall
        );
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

        Gizmos.DrawWireSphere(
            transform.position,
            displayedRange
        );
    }
}