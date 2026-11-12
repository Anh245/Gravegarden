using System.Collections.Generic;
using UnityEngine;

public class BuildGrid : MonoBehaviour
{
    [SerializeField, Range(4, 256)] private int columns = 8;
    [SerializeField, Range(4, 256)] private int rows = 8;
    [SerializeField, Min(0.01f)] private float cellSize = 0.64f;
    [Header("Dùng để tạo ô trong Editor")]
    [SerializeField] private BuildSlot slotPrefab;
    [SerializeField] private BuildManager buildManager;

    private readonly Dictionary<Vector2Int, BuildSlot> cells = new Dictionary<Vector2Int, BuildSlot>();
    private readonly List<BuildSlot> previewCells = new List<BuildSlot>();
    private PlacementMap map;
    [SerializeField, HideInInspector] private bool expansionZonesAuthored;
    public bool ExpansionZonesAuthored => expansionZonesAuthored;
    public bool ExpansionUnlocked { get; private set; }
    public void SetExpansionUnlocked(bool unlocked)
    {
        ExpansionUnlocked = unlocked;
        if (IsReady)
        {
            // Checkpoint một khu có thể đã thay map bằng tập ô khu đầu.
            if (unlocked) map.AllowCells(CheckpointAllowedCells());
            RefreshOccupancy();
        }
    }
    private bool CellsUnlocked(Vector2Int anchor, int size)
    {
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            if (!cells.TryGetValue(anchor + new Vector2Int(x, y), out BuildSlot slot)
                || slot == null || !slot.IsUnlocked) return false;
        return true;
    }
    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;
    public BuildSlot SlotPrefab => slotPrefab;
    public BuildManager Manager => buildManager;
    public bool IsReady { get; private set; }
    public bool IsBuildMode { get; private set; } = true;
    public void SetBuildMode(bool value)
    {
        ClearPreview();
        IsBuildMode = value;
        foreach (BuildSlot slot in cells.Values)
            if (slot != null) slot.SetGridInteraction(value);
    }

    public bool HasValidTransform => Quaternion.Angle(transform.rotation, Quaternion.identity) < 0.01f
        && (transform.lossyScale - Vector3.one).sqrMagnitude < 0.0001f;

    private void Awake()
    {
        if (!HasValidTransform || cellSize <= 0f || float.IsNaN(cellSize) || float.IsInfinity(cellSize)
            || columns < 1 || rows < 1)
        {
            Debug.LogError("BuildGrid: kiểm tra kích thước, Rotation=0 và Scale thế giới=1.", this);
            return;
        }
        var allowed = new List<(int x, int y)>();
        foreach (BuildSlot slot in GetComponentsInChildren<BuildSlot>(true))
        {
            if (!slot.gameObject.activeInHierarchy) continue;
            Vector2Int coordinate = slot.Cell;
            if (slot.Grid != this || coordinate.x < 0 || coordinate.y < 0
                || coordinate.x >= columns || coordinate.y >= rows || cells.ContainsKey(coordinate))
            {
                Debug.LogError("BuildGrid: ô sai Grid, tọa độ ngoài vùng hoặc tọa độ bị trùng.", slot);
                return;
            }
            if ((slot.transform.position - GetCellCenter(coordinate)).sqrMagnitude > 0.0001f
                || Quaternion.Angle(slot.transform.rotation, Quaternion.identity) > 0.01f
                || (slot.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
            {
                Debug.LogError("BuildGrid: ô bị lệch vị trí/Scale. Hãy dùng lại công cụ tạo ô trước khi Play.", slot);
                return;
            }
            cells.Add(coordinate, slot);
            if (slot.IsBuildable) allowed.Add((coordinate.x, coordinate.y));
        }
        if (cells.Count == 0)
        {
            Debug.LogError("BuildGrid: chưa có ô. Tạo ô trong Inspector trước khi Play.", this);
            return;
        }
        map = new PlacementMap(columns, rows, allowed);
        IsReady = true;
        RefreshOccupancy();
    }

    public BuildSlot FindCell(Vector2Int anchor)=>cells.TryGetValue(anchor,out BuildSlot slot)?slot:null;
    public bool CanMove(BuildingPlacement placement,Vector2Int anchor,out string reason)
    {
        reason="Công trình không thuộc lưới đang hoạt động.";
        if(!IsReady || placement==null || !placement.IsPlaced || placement.Grid!=this || placement.SaleClaimed) return false;
        if (!CellsUnlocked(anchor, placement.SizeInCells))
        { reason="Vị trí nằm trong khu chưa mở khóa hoặc không có ô xây."; return false; }
        bool valid=map.CanRelocate(placement.ReservationId,anchor.x,anchor.y,placement.SizeInCells,out PlacementFailure failure);
        reason=FailureText(failure);return valid;
    }
    public bool TryMove(BuildingPlacement placement,Vector2Int anchor,out string reason)
    {
        if(!CanMove(placement,anchor,out reason)) return false;
        if(!map.TryRelocate(placement.ReservationId,anchor.x,anchor.y,placement.SizeInCells,out PlacementFailure failure))
        {reason=FailureText(failure);return false;}
        placement.ApplyGridMove(anchor,GetPlacementCenter(anchor,placement.SizeInCells));
        RefreshOccupancy();reason="";return true;
    }

    public Vector3 GetCellCenter(Vector2Int cell)
        => transform.position + new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);

    public Vector3 GetPlacementCenter(Vector2Int anchor, int size)
        => transform.position + new Vector3((anchor.x + size * 0.5f) * cellSize, (anchor.y + size * 0.5f) * cellSize, 0f);

    public bool CanPlace(BuildSlot anchor, int size, out string reason)
    {
        if (!IsReady || anchor == null || anchor.Grid != this
            || !cells.TryGetValue(anchor.Cell, out BuildSlot actual) || actual != anchor)
        {
            reason = "Ô không thuộc lưới xây đang hoạt động.";
            return false;
        }
        bool valid = map.CanPlace(anchor.Cell.x, anchor.Cell.y, size, out PlacementFailure failure);
        if (valid && !CellsUnlocked(anchor.Cell, size))
        { reason = "Công trình chạm vào khu chưa mở khóa."; return false; }
        reason = FailureText(failure);
        return valid;
    }

    public bool TryReserve(BuildSlot anchor, int size, out int reservation, out Vector3 center, out string reason)
    {
        reservation = 0;
        center = Vector3.zero;
        if (!CanPlace(anchor, size, out reason)) return false;
        if (!map.TryReserve(anchor.Cell.x, anchor.Cell.y, size, out reservation, out PlacementFailure failure))
        {
            reason = FailureText(failure);
            return false;
        }
        center = GetPlacementCenter(anchor.Cell, size);
        RefreshOccupancy();
        return true;
    }

    public List<(int x, int y)> CheckpointAllowedCells() => CheckpointAllowedCells(ExpansionUnlocked);
    public List<(int x, int y)> CheckpointAllowedCells(bool expanded)
    {
        var allowed = new List<(int x, int y)>();
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
            if (cells.TryGetValue(new Vector2Int(x, y), out BuildSlot cell)
                && cell != null && cell.IsBuildable && (expanded || !cell.RequiresExpansion) && cell.gameObject.activeInHierarchy)
                allowed.Add((x, y));
        return allowed;
    }

    public PlacementMap CheckpointEmptyMap() => CheckpointEmptyMap(ExpansionUnlocked);
    public PlacementMap CheckpointEmptyMap(bool expanded) => new PlacementMap(columns, rows, CheckpointAllowedCells(expanded));

    public void AdoptCheckpointMap(PlacementMap replacement)
    {
        if (replacement == null) throw new System.ArgumentNullException(nameof(replacement));
        map = replacement;
        RefreshOccupancy();
    }

    public string CheckpointLayoutSignature()
    {
        var text = new System.Text.StringBuilder();
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        text.Append(columns).Append(',').Append(rows).Append('|')
            .Append(cellSize.ToString("R", culture)).Append('|')
            .Append(transform.position.x.ToString("R", culture)).Append(',')
            .Append(transform.position.y.ToString("R", culture)).Append(',')
            .Append(transform.position.z.ToString("R", culture));
        text.Append("|layout-v2|");
        foreach (var cell in CheckpointAllowedCells(true))
        {
            BuildSlot slot=FindCell(new Vector2Int(cell.x,cell.y));
            text.Append('|').Append(cell.x).Append(',').Append(cell.y).Append(':').Append(slot.RequiresExpansion?'1':'0');
        }
        return CheckpointHash.FromText(text.ToString());
    }

    public void ReleaseReservation(int reservation)
    {
        if (map == null || !map.Release(reservation)) return;
        RefreshOccupancy();
    }

    private void RefreshOccupancy()
    {
        ClearPreview();
        foreach (var pair in cells)
            if (pair.Value != null)
                pair.Value.SetOccupied(map.IsOccupied(pair.Key.x, pair.Key.y));
    }

    public void ShowPreview(BuildSlot anchor, int size, bool valid)
    {
        ClearPreview();
        if (!IsBuildMode || anchor == null) return;
        Color color = valid ? new Color(0.2f, 1f, 0.3f, 0.55f) : new Color(1f, 0.2f, 0.2f, 0.55f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            if (cells.TryGetValue(anchor.Cell + new Vector2Int(x, y), out BuildSlot cell) && cell != null)
            {
                cell.SetPreview(color);
                previewCells.Add(cell);
            }
    }

    public void ClearPreview()
    {
        foreach (BuildSlot slot in previewCells) if (slot != null) slot.ClearPreview();
        previewCells.Clear();
    }

    private static string FailureText(PlacementFailure failure)
    {
        switch (failure)
        {
            case PlacementFailure.InvalidReservation: return "Công trình chưa có footprint hợp lệ để di chuyển.";
            case PlacementFailure.InvalidSize: return "Kích thước công trình không hợp lệ hoặc lớn hơn vùng xây.";
            case PlacementFailure.OutsideBounds: return "Công trình vượt ra ngoài lưới.";
            case PlacementFailure.Blocked: return "Có ô bị chặn hoặc không thuộc vùng được phép xây.";
            case PlacementFailure.Occupied: return "Vị trí này chồng lên công trình khác.";
            default: return "";
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (columns < 1 || rows < 1 || cellSize <= 0f) return;
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position;
        for (int x = 0; x <= columns; x++)
            Gizmos.DrawLine(origin + Vector3.right * x * cellSize,
                origin + new Vector3(x * cellSize, rows * cellSize, 0f));
        for (int y = 0; y <= rows; y++)
            Gizmos.DrawLine(origin + Vector3.up * y * cellSize,
                origin + new Vector3(columns * cellSize, y * cellSize, 0f));
    }
}
