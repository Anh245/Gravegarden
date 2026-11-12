using UnityEngine;

// Chỉ các nền đang bật mới mở rộng giới hạn camera.
public class WorldMapBounds : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] grounds;
    public bool TryGetBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        if (grounds == null) return false;
        foreach (SpriteRenderer item in grounds)
        {
            if (item == null || item.sprite == null || !item.enabled || !item.gameObject.activeInHierarchy) continue;
            if (!found) { bounds = item.bounds; found = true; }
            else bounds.Encapsulate(item.bounds);
        }
        return found && bounds.size.x > 0 && bounds.size.y > 0;
    }
}
