using System;
using UnityEngine;

public class TownHallHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 20;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => CurrentHealth <= 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;

    }

    public void TakeDamage(int amount)
    {
        if(IsDestroyed || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        Debug.Log($"Nha chinh bi tan cong! Mau hien tai: {CurrentHealth}/{maxHealth}", this);

        if(IsDestroyed)
        {
            Debug.Log("Nha chinh bi pha huy!", this);
            // Thêm logic khi nhà chính bị phá hủy, ví dụ: kết thúc trò chơi
        }
    }
    
}
