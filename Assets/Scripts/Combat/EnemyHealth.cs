using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 60f;
    [SerializeField] private Transform aimPoint;

    public float CurrentHealth { get; private set; }

    public bool IsAlive =>
        gameObject.activeInHierarchy && CurrentHealth > 0f;

    public Vector3 AimPosition =>
        aimPoint != null ? aimPoint.position : transform.position;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        Debug.Log(
            $"Rotling còn {CurrentHealth}/{maxHealth} máu.",
            this
        );

        if (CurrentHealth <= 0f)
        {
            // Mốc hiện tại: dừng toàn bộ địch khi chết.
            gameObject.SetActive(false);
        }
    }
}