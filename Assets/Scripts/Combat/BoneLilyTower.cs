using UnityEngine;

public class BoneLilyTower : MonoBehaviour
{
    [SerializeField] private Animator visualAnimator;
    [SerializeField] private Transform muzzle;
    [SerializeField] private BoneProjectile projectilePrefab;
    [SerializeField] private TownHallHealth defendedHall;

    [SerializeField, Min(0.01f)] private float attackRange = 3.6f;
    [SerializeField, Min(0.01f)] private float attackInterval = 0.8f;
    [SerializeField, Min(0.01f)] private float damage = 18f;

    [SerializeField, Min(0.01f)]
    private float retrySearchInterval = 0.1f;

    private float cooldown;
    private float searchDelay;
    private EnemyHealth currentTarget;

    private void Start()
    {
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
        }
    }

    private void LateUpdate()
    {
        if (defendedHall.IsDestroyed)
        {
            currentTarget = null;
            visualAnimator.speed = 0f;
            return;
        }

        cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);
        searchDelay = Mathf.Max(0f, searchDelay - Time.deltaTime);

        if (cooldown > 0f || searchDelay > 0f)
            return;

        currentTarget = FindBestTarget();

        if (currentTarget == null)
        {
            searchDelay = retrySearchInterval;
            return;
        }

        Fire(currentTarget);
        cooldown = attackInterval;
    }

    private EnemyHealth FindBestTarget()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>();

        EnemyHealth best = null;
        float bestProgress = -1f;
        float bestDistanceSquared = float.PositiveInfinity;
        EntityId bestId = EntityId.None;

        float rangeSquared = attackRange * attackRange;

        foreach (EnemyHealth candidate in enemies)
        {
            if (!candidate.IsAlive)
                continue;

            EnemyRouteFollower follower =
                candidate.GetComponent<EnemyRouteFollower>();

            if (follower == null || !follower.IsReady)
                continue;

            Vector2 offset =
                (Vector2)candidate.transform.position -
                (Vector2)transform.position;

            float distanceSquared = offset.sqrMagnitude;

            if (distanceSquared > rangeSquared)
                continue;

            float progress = follower.Progress;
            EntityId candidateId = candidate.GetEntityId();

            bool better =
                best == null ||
                progress > bestProgress ||
                (
                    progress == bestProgress &&
                    distanceSquared < bestDistanceSquared
                ) ||
                (
                    progress == bestProgress &&
                    distanceSquared == bestDistanceSquared &&
                    candidateId.CompareTo(bestId) < 0
                );

            if (!better)
                continue;

            best = candidate;
            bestProgress = progress;
            bestDistanceSquared = distanceSquared;
            bestId = candidateId;
        }

        return best;
    }

    private void Fire(EnemyHealth target)
    {
        visualAnimator.Play("Base Layer.Attack", 0, 0f);

        BoneProjectile projectile = Instantiate(
            projectilePrefab,
            muzzle.position,
            Quaternion.identity
        );

        projectile.Initialize(target, damage, defendedHall);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentTarget != null && currentTarget.IsAlive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                transform.position,
                currentTarget.AimPosition
            );
        }
    }

    public void Configure(TownHallHealth newDefendedHall)
    {
        defendedHall = newDefendedHall;
    }
}