// Bộ đếm thiêu đốt độc lập với Unity, có thể kiểm thử bằng .NET.
public sealed class BurnState
{
    public const float TickInterval = 0.5f;
    private double elapsed;
    private double expiresAt;
    private double nextTickAt;
    private float damagePerSecond;

    public bool IsActive => damagePerSecond > 0f && elapsed < expiresAt;
    public float DamagePerSecond => IsActive ? damagePerSecond : 0f;
    public float RemainingTime => IsActive ? (float)(expiresAt - elapsed) : 0f;

    public void Apply(float newDamagePerSecond, float duration)
    {
        if (newDamagePerSecond <= 0f || duration <= 0f
            || float.IsNaN(newDamagePerSecond) || float.IsInfinity(newDamagePerSecond)
            || float.IsNaN(duration) || float.IsInfinity(duration))
            return;

        if (!IsActive)
        {
            Clear();
            damagePerSecond = newDamagePerSecond;
            expiresAt = duration;
            return;
        }

        damagePerSecond = System.Math.Max(damagePerSecond, newDamagePerSecond);
        expiresAt = System.Math.Max(expiresAt, elapsed + duration);
        // Không đặt lại nhịp tick: nhiều đạn trong cùng frame không tạo thêm tick.
    }

    public float Advance(float deltaTime)
    {
        if (!IsActive || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            return 0f;

        double end = System.Math.Min(elapsed + deltaTime, expiresAt);
        double damage = 0d;
        // Tick đầu ở đầu hiệu ứng; loại trừ tick tại đúng thời điểm hết hạn.
        // 3 giây: các mốc 0, 0.5, 1, 1.5, 2, 2.5 => 6 tick.
        while (nextTickAt < end)
        {
            damage += damagePerSecond * TickInterval;
            nextTickAt += TickInterval;
        }
        elapsed = end;
        if (elapsed >= expiresAt)
            Clear();
        return (float)damage;
    }

    public void Clear()
    {
        elapsed = 0d;
        expiresAt = 0d;
        nextTickAt = 0d;
        damagePerSecond = 0f;
    }
}
