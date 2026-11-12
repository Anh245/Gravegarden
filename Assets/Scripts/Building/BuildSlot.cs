using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public class BuildSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private Transform buildPoint;
    [SerializeField] private SpriteRenderer marker;
    [Header("Ô lưới")]
    [SerializeField] private BuildGrid grid;
    [SerializeField] private Vector2Int cell;
    [SerializeField] private bool buildable = true;
    [SerializeField] private bool requiresExpansion;
    public bool RequiresExpansion => requiresExpansion;
    public bool IsUnlocked => !requiresExpansion || (grid != null && grid.ExpansionUnlocked);
    [SerializeField] private Color availableColor = new Color(0.2f, 0.8f, 1f, 0.25f);
    [SerializeField] private Color blockedColor = new Color(0.4f, 0.4f, 0.4f, 0.15f);

    private bool occupied;
    private bool gridInteractionVisible = true;
    public void SetGridInteraction(bool value)
    {
        gridInteractionVisible = value;
        ClearPreview();
    }
    public BuildGrid Grid => grid;
    public Vector2Int Cell => cell;
    public bool IsBuildable => buildable;
    public bool IsOccupied => occupied;
    public Vector3 BuildPosition => buildPoint != null ? buildPoint.position : transform.position;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left
            || occupied || !buildable || !IsUnlocked || buildManager == null || (grid != null && !grid.IsBuildMode))
            return;
        buildManager.SelectSlot(this);
    }

    public void SetOccupied(bool value)
    {
        occupied = value;
        ClearPreview();
    }

    public void MarkOccupied() => SetOccupied(true);

    public void SetPreview(Color color)
    {
        if (marker == null || !gridInteractionVisible || !IsUnlocked) return;
        marker.enabled = true;
        marker.color = color;
    }

    public void ClearPreview()
    {
        bool visible = gridInteractionVisible && IsUnlocked;
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null) box.enabled = visible && buildable && !occupied;
        if (marker == null) return;
        marker.enabled = visible && !occupied;
        marker.color = buildable ? availableColor : blockedColor;
    }

    // Editor dùng để tạo các ô từ prefab đang có.
    public void ConfigureAuthoring(BuildGrid owner, Vector2Int coordinate, BuildManager manager, float cellSize)
    {
        grid = owner;
        cell = coordinate;
        buildManager = manager;
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
        transform.position = owner.GetCellCenter(coordinate);
        if (buildPoint != null) buildPoint.localPosition = Vector3.zero;
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.offset = Vector2.zero;
        box.size = Vector2.one * cellSize * 0.96f;
        if (marker != null && marker.sprite != null)
        {
            Vector3 size = marker.sprite.bounds.size;
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localRotation = Quaternion.identity;
            if (size.x > 0f && size.y > 0f)
                marker.transform.localScale = new Vector3(cellSize * 0.9f / size.x, cellSize * 0.9f / size.y, 1f);
        }
        ClearPreview();
    }
}
