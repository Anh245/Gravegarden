using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public class BuildSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private Transform buildPoint;
    [SerializeField] private SpriteRenderer marker;

    private bool occupied;

    public bool IsOccupied => occupied;

    public Vector3 BuildPosition =>
        buildPoint != null
            ? buildPoint.position
            : transform.position;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (occupied || buildManager == null)
            return;

        buildManager.SelectSlot(this);
    }

    public void MarkOccupied()
    {
        occupied = true;

        if (marker != null)
            marker.enabled = false;

        GetComponent<BoxCollider2D>().enabled = false;
    }
}