using System.Collections.Generic;
using UnityEngine;

public class BoneProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float speed = 10.5f;
    [SerializeField, Min(0.1f)] private float maxLifetime = 5f;

    [Header("Hiệu ứng làm chậm")]
    [SerializeField] private bool appliesSlow;
    [SerializeField, Range(0f, 0.95f)] private float slowStrength = 0.25f;
    [SerializeField, Min(0.01f)] private float slowDuration = 1f;

    [Header("Sát thương diện rộng")]
    [SerializeField] private bool appliesSplash;
    [SerializeField, Min(0.01f)] private float splashRadius = 1.35f;

    [Header("Đường bay vòng cung - dùng cho Pháo Mộ")]
    [SerializeField] private bool usesArc;
    [SerializeField, Min(0f)] private float arcHeight = 2.8f;
    [SerializeField, Min(0.01f)] private float minimumFlightTime = 0.3f;

    [Header("Sét truyền - dùng cho Đèn Bão")]
    [SerializeField] private bool appliesChain;
    [SerializeField, Range(1, 3)] private int maxChainTargets = 3;
    [SerializeField, Min(0.01f)] private float chainRadius = 2.5f;
    [SerializeField, Range(0f, 1f)] private float chainDamageRetention = 0.8f;
    [SerializeField] private LineRenderer chainLinePrefab;
    [SerializeField, Min(0.01f)] private float chainFlashDuration = 0.12f;
    [SerializeField] private bool logChainHits;

    [Header("Xuyên theo đường bắn - dùng cho Nỏ Gai")]
    [SerializeField] private bool appliesPierce;
    [SerializeField, Min(0f)] private float pierceExtraDistance = 1.28f;
    [SerializeField, Min(0.01f)] private float pierceHalfWidth = 0.6f;
    [SerializeField] private LineRenderer pierceLinePrefab;
    [SerializeField, Min(0.01f)] private float pierceFlashDuration = 0.12f;
    [SerializeField] private float pierceSpriteAngleOffset = -90f;
    [SerializeField] private bool logPierceHits;

    [Header("Thiêu đốt - dùng cho Vạc Lửa")]
    [SerializeField] private bool appliesBurn;
    [SerializeField, Min(0.01f)] private float burnDamagePerSecond = 18f;
    [SerializeField, Min(0.01f)] private float burnDuration = 3f;

    private float launchAttackRange;
    private EnemyHealth target;
    private TownHallHealth defendedHall;
    private float damage;
    private float age;
    private bool initialized;
    private bool resolved;
    private bool impactDamageApplied;
    private Vector3 launchPosition;
    private Vector3 lastImpactPosition;
    private float flightDuration;

    public void Initialize(EnemyHealth newTarget, float newDamage, TownHallHealth hall, float attackRange = 0f)
    {
        target = newTarget;
        damage = newDamage;
        defendedHall = hall;
        launchAttackRange = attackRange;
        age = 0f;
        resolved = false;
        impactDamageApplied = false;

        if (target == null || !target.IsAlive || hall == null || hall.IsDestroyed
            || speed <= 0f || float.IsNaN(speed) || float.IsInfinity(speed)
            || damage <= 0f || (usesArc && !appliesSplash)
            || (appliesChain && (usesArc || appliesSplash || appliesSlow
                || maxChainTargets < 1 || maxChainTargets > 3
                || chainRadius <= 0f || float.IsNaN(chainRadius) || float.IsInfinity(chainRadius)
                || chainDamageRetention < 0f || chainDamageRetention > 1f
                || float.IsNaN(chainDamageRetention)))
            || (appliesPierce && (usesArc || appliesSplash || appliesSlow || appliesChain
                || launchAttackRange <= 0f || float.IsNaN(launchAttackRange) || float.IsInfinity(launchAttackRange)
                || pierceExtraDistance < 0f || float.IsNaN(pierceExtraDistance) || float.IsInfinity(pierceExtraDistance)
                || pierceHalfWidth <= 0f || float.IsNaN(pierceHalfWidth) || float.IsInfinity(pierceHalfWidth)))
            || (appliesBurn && (!appliesSplash || appliesSlow || appliesChain || appliesPierce
                || burnDamagePerSecond <= 0f || float.IsNaN(burnDamagePerSecond) || float.IsInfinity(burnDamagePerSecond)
                || burnDuration <= 0f || float.IsNaN(burnDuration) || float.IsInfinity(burnDuration))))
        {
            Debug.LogError("BoneProjectile: cấu hình không hợp lệ. Burn/Arc cần Splash; Burn không dùng chung Slow, Chain hoặc Pierce. Pierce cần tầm đánh từ tháp.", this);
            Remove();
            return;
        }

        launchPosition = transform.position;
        // Pháo nổ trên mặt đất, dùng root của quái để đồng nhất tâm vùng sát thương.
        lastImpactPosition = target.transform.position;
        lastImpactPosition.z = launchPosition.z;
        flightDuration = Mathf.Max(minimumFlightTime,
            Vector2.Distance(launchPosition, lastImpactPosition) / speed);
        initialized = true;
    }

    private void Update()
    {
        if (resolved)
            return;
        if (!initialized || defendedHall == null || defendedHall.IsDestroyed)
        {
            Remove();
            return;
        }
        if (Time.timeScale <= 0f)
            return;

        age += Time.deltaTime;
        if (age >= maxLifetime)
        {
            Remove();
            return;
        }

        if (usesArc)
        {
            UpdateArc();
            return;
        }

        // Các prefab cũ giữ đường bay đuổi theo AimPoint.
        if (target == null || !target.IsAlive)
        {
            Remove();
            return;
        }
        Vector3 destination = target.AimPosition;
        if (appliesPierce)
        {
            Vector3 heading = destination - transform.position;
            if (heading.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg + pierceSpriteAngleOffset);
        }
        float step = speed * Time.deltaTime;
        if (Vector3.Distance(transform.position, destination) <= step)
        {
            transform.position = destination;
            resolved = true;
            ApplySlowTo(target);
            DealImpactDamage(target, damage);
            Destroy(gameObject);
            return;
        }
        transform.position = Vector3.MoveTowards(transform.position, destination, step);
    }

    private void UpdateArc()
    {
        // Giống Godot: cập nhật điểm rơi khi mục tiêu còn sống.
        // Nếu mục tiêu chết, tiếp tục tới vị trí cuối đã ghi nhận và vẫn nổ.
        if (target != null && target.IsAlive)
        {
            lastImpactPosition = target.transform.position;
            lastImpactPosition.z = launchPosition.z;
        }

        float t = Mathf.Clamp01(age / flightDuration);
        Vector3 position = Vector3.Lerp(launchPosition, lastImpactPosition, t);
        // Unity 2D dùng chiều Y dương hướng lên.
        position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = position;

        if (t < 1f)
            return;

        transform.position = lastImpactPosition;
        resolved = true;
        GameAudio.Play(GameSound.Impact);
        DealSplashDamage(lastImpactPosition, damage);
        Destroy(gameObject);
    }

    private void Remove()
    {
        resolved = true;
        Destroy(gameObject);
    }

    private void ApplySlowTo(EnemyHealth enemy)
    {
        if (!appliesSlow || enemy == null || !enemy.IsAlive)
            return;
        EnemyRouteFollower movement = enemy.GetComponent<EnemyRouteFollower>();
        if (movement != null)
            movement.ApplySlow(slowStrength, slowDuration);
    }

    private void DealImpactDamage(EnemyHealth hitEnemy, float hitDamage)
    {
        if (impactDamageApplied || hitEnemy == null || !hitEnemy.IsAlive || hitDamage <= 0f)
            return;
        GameAudio.Play(GameSound.Impact);
        if (appliesPierce)
        {
            DealPierceDamage(hitEnemy.transform.position, hitDamage);
            return;
        }
        if (appliesChain)
        {
            DealChainDamage(hitEnemy.transform.position, hitDamage);
            return;
        }
        if (appliesSplash)
        {
            DealSplashDamage(hitEnemy.transform.position, hitDamage);
            return;
        }
        impactDamageApplied = true;
        hitEnemy.TakeDamage(hitDamage);
    }

    private void DealChainDamage(Vector2 impactCenter, float hitDamage)
    {
        if (impactDamageApplied || hitDamage <= 0f)
            return;
        impactDamageApplied = true;

        EnemyHealth[] pool = UnityEngine.Object.FindObjectsByType<EnemyHealth>();
        var hit = new HashSet<EnemyHealth>();
        Vector2 point = impactCenter;
        Vector3 visualOrigin = transform.position;
        float currentDamage = hitDamage;
        float radiusSquared = chainRadius * chainRadius;

        for (int hop = 0; hop < maxChainTargets; hop++)
        {
            EnemyHealth nearest = null;
            float nearestDistance = radiusSquared;
            foreach (EnemyHealth enemy in pool)
            {
                if (enemy == null || !enemy.IsAlive || hit.Contains(enemy))
                    continue;
                Vector2 offset = (Vector2)enemy.transform.position - point;
                float distance = offset.sqrMagnitude;
                // Godot dùng khoảng cách nhỏ hơn radius, không bao gồm biên.
                if (distance < nearestDistance)
                {
                    nearest = enemy;
                    nearestDistance = distance;
                }
            }
            if (nearest == null)
                break;

            hit.Add(nearest);
            point = nearest.transform.position;
            Vector3 visualDestination = nearest.AimPosition;
            DrawChainLink(visualOrigin, visualDestination);
            visualOrigin = visualDestination;
            if (logChainHits)
                Debug.Log($"Đèn Bão - mục tiêu {hop + 1}/{maxChainTargets}: {nearest.name}, sát thương {currentDamage:0.##}", nearest);
            nearest.TakeDamage(currentDamage);
            currentDamage *= chainDamageRetention;
        }
    }

    private void DrawChainLink(Vector3 start, Vector3 end)
    {
        if (chainLinePrefab == null || (end - start).sqrMagnitude < 0.0001f)
            return;
        LineRenderer beam = Instantiate(chainLinePrefab);
        beam.enabled = true;
        beam.useWorldSpace = true;
        beam.loop = false;
        SpriteRenderer projectileVisual = GetComponentInChildren<SpriteRenderer>();
        if (projectileVisual != null)
        {
            beam.sortingLayerID = projectileVisual.sortingLayerID;
            beam.sortingOrder = projectileVisual.sortingOrder + 1;
        }
        beam.positionCount = 5;
        Vector3 direction = end - start;
        Vector3 offset = new Vector3(-direction.y, direction.x, 0f).normalized * 0.06f;
        beam.SetPosition(0, start);
        beam.SetPosition(1, Vector3.Lerp(start, end, 0.25f) + offset);
        beam.SetPosition(2, Vector3.Lerp(start, end, 0.5f) - offset);
        beam.SetPosition(3, Vector3.Lerp(start, end, 0.75f) + offset);
        beam.SetPosition(4, end);
        // Tia là đối tượng độc lập, không bị hủy cùng viên đạn ngay khi trúng.
        Destroy(beam.gameObject, chainFlashDuration);
    }

    private void DealPierceDamage(Vector2 impactPoint, float hitDamage)
    {
        if (impactDamageApplied || hitDamage <= 0f)
            return;
        impactDamageApplied = true;

        Vector2 start = launchPosition;
        Vector2 heading = (impactPoint - start).normalized;
        Vector2 end = start + heading * (launchAttackRange + pierceExtraDistance);
        DrawPierceLine(start, end);

        foreach (EnemyHealth enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>())
        {
            if (enemy == null || !enemy.IsAlive)
                continue;
            Vector2 enemyPosition = enemy.transform.position;
            if (!PierceGeometry.ContainsPoint(enemyPosition.x, enemyPosition.y,
                start.x, start.y, end.x, end.y, pierceHalfWidth))
                continue;
            if (logPierceHits)
                Debug.Log($"Nỏ Gai trúng {enemy.name}: {hitDamage:0.##} sát thương", enemy);
            enemy.TakeDamage(hitDamage);
        }
    }

    private void DrawPierceLine(Vector2 start, Vector2 end)
    {
        if (pierceLinePrefab == null)
            return;
        LineRenderer line = Instantiate(pierceLinePrefab);
        line.enabled = true;
        line.useWorldSpace = true;
        line.loop = false;
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(start.x, start.y, launchPosition.z));
        line.SetPosition(1, new Vector3(end.x, end.y, launchPosition.z));
        SpriteRenderer visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null)
        {
            line.sortingLayerID = visual.sortingLayerID;
            line.sortingOrder = visual.sortingOrder + 1;
        }
        Destroy(line.gameObject, pierceFlashDuration);
    }

    private void DealSplashDamage(Vector2 impactCenter, float hitDamage)
    {
        if (impactDamageApplied || hitDamage <= 0f)
            return;
        impactDamageApplied = true;
        float radiusSquared = splashRadius * splashRadius;
        foreach (EnemyHealth enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>())
        {
            if (enemy == null || !enemy.IsAlive)
                continue;
            Vector2 offset = (Vector2)enemy.transform.position - impactCenter;
            if (offset.sqrMagnitude <= radiusSquared)
            {
                enemy.TakeDamage(hitDamage);
                // Chỉ gắn cháy nếu quái sống sót sau sát thương va chạm.
                if (appliesBurn && enemy.IsAlive)
                    enemy.ApplyBurn(burnDamagePerSecond, burnDuration);
            }
        }
    }
}
