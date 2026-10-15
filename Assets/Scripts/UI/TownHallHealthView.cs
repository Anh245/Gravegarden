using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class TownHallHealthView : MonoBehaviour
{
    [SerializeField] private TownHallHealth health;
    [SerializeField] private Transform anchor;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas canvas;

    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text valueText;


    [SerializeField] private float verticalOffset = -60f;

    private void Awake()
    {
        if (health == null || anchor == null ||
            worldCamera == null || canvas == null ||
            healthBar == null || valueText == null
            )
        {
            Debug.LogError(
                "TownHallHealthView chưa được gán đủ tham chiếu.",
                this
            );

            enabled = false;
            return;
        }

        healthBar.minValue = 0;
        healthBar.maxValue = health.MaxHealth;
        healthBar.wholeNumbers = true;
        
    }

    private void LateUpdate()
    {
        healthBar.SetValueWithoutNotify(health.CurrentHealth);

        valueText.text =
            $"{health.CurrentHealth} / {health.MaxHealth}";

        Vector3 screenPoint =
            worldCamera.WorldToScreenPoint(anchor.position);

        healthBar.gameObject.SetActive(screenPoint.z > 0f);

        if (screenPoint.z > 0f)
        {
            screenPoint.y += verticalOffset * canvas.scaleFactor;
            screenPoint.z = 0f;

            healthBar.transform.position = screenPoint;
        }

       
    }
}