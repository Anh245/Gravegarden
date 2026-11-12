using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public class BuildingMoveController : MonoBehaviour
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private BuildModeController buildMode;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GameObject movePanel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button cancelButton;
    private static BuildingMoveController current;
    private BuildingPlacement source;
    private SpriteRenderer ghost;
    private Vector3 visualOffset;
    private bool rangePreviewVisible;
    private bool rangePreviewValid;
    private Vector2Int rangePreviewAnchor;
    public bool TryGetRangePreview(out Vector3 center,out float radius,out bool valid)
    {
        center=Vector3.zero;radius=0f;valid=false;
        if(!IsMoving || !rangePreviewVisible || source.Grid==null) return false;
        var tower=source.GetComponent<BoneLilyTower>();
        var support=source.GetComponent<SupportBuilding>();
        if(tower!=null && tower.CheckpointReady) radius=tower.CurrentEffectiveRange;
        else if(support!=null && support.CheckpointReady) radius=support.CurrentEffectiveRange;
        else return false;
        center=source.Grid.GetPlacementCenter(rangePreviewAnchor,source.SizeInCells);
        valid=rangePreviewValid;return true;
    }
    private bool initialized;
    private int beganFrame;
    public bool IsMoving => source!=null;
    private readonly List<RaycastResult> hits=new List<RaycastResult>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()=>current=null;
    public static bool BlocksWorldClick(GameObject target)=>current!=null && target!=null
        && target.scene==current.gameObject.scene && current.buildManager!=null
        && current.buildManager.MoveInputBlocked;

    private void Start()
    {
        if(movePanel!=null) movePanel.SetActive(false);
        if(buildManager==null || buildMode==null || worldCamera==null || movePanel==null
            || statusText==null || cancelButton==null || (current!=null && current!=this)
            || !buildManager.RegisterMoveController(this))
        {
            Debug.LogError("BuildingMoveController: thiếu tham chiếu hoặc có controller trùng.",this);
            enabled=false;return;
        }
        current=this;cancelButton.onClick.AddListener(CancelMove);initialized=true;
    }
    private bool CanOperate => initialized && isActiveAndEnabled && buildManager.CheckpointReady
        && !SceneNavigator.IsTransitioning && Time.timeScale>0f
        && buildManager.CheckpointHall!=null && !buildManager.CheckpointHall.IsDestroyed
        && buildManager.CheckpointWaves!=null && buildManager.CheckpointWaves.CanBuildNow;

    private bool ValidSource(BuildingPlacement placement)
    {
        if(!CanOperate || placement==null || !placement.isActiveAndEnabled || placement.SaleClaimed
            || !placement.IsPlaced || placement.Grid!=buildManager.CheckpointGrid
            || placement.gameObject.scene!=gameObject.scene
            || !placement.transform.IsChildOf(buildManager.CheckpointBuildingsParent)) return false;
        var tower=placement.GetComponent<BoneLilyTower>();
        var producer=placement.GetComponent<ProducerBuilding>();
        var support=placement.GetComponent<SupportBuilding>();
        return tower!=null && tower.CheckpointReady || producer!=null && producer.CheckpointReady
            || support!=null && support.CheckpointReady;
    }
    public bool CanBegin(BuildingPlacement placement)=>!IsMoving && ValidSource(placement);
    public void BeginMove(BuildingPlacement placement)
    {
        if(!CanBegin(placement)) return;
        buildMode.ExitBuildMode();
        buildManager.CloseForCheckpoint();
        CloseUpgradePanels();
        source=placement;beganFrame=Time.frameCount;
        buildManager.BlockMoveClickForFrame();
        BuildGrid grid=source.Grid;grid.SetBuildMode(true);
        SpriteRenderer visual=source.GetComponentInChildren<SpriteRenderer>();
        if(visual!=null)
        {
            var preview=new GameObject("BuildingMovePreview");
            ghost=preview.AddComponent<SpriteRenderer>();
            ghost.sprite=visual.sprite;ghost.sortingLayerID=visual.sortingLayerID;
            ghost.sortingOrder=visual.sortingOrder+1;
            ghost.flipX=visual.flipX;ghost.flipY=visual.flipY;
            ghost.transform.rotation=visual.transform.rotation;
            ghost.transform.localScale=visual.transform.lossyScale;
            visualOffset=visual.transform.position-source.transform.position;
            preview.SetActive(false);
        }
        statusText.text="Chọn vị trí mới bằng chuột trái. Esc hoặc Hủy để giữ vị trí cũ. Di chuyển miễn phí.";
        movePanel.SetActive(true);movePanel.transform.SetAsLastSibling();
    }
    private void Update()
    {
        if(!initialized) return;
        if(source==null)
        {
            // Unity object đã bị Destroy có thể còn phần preview cần dọn.
            if(ghost!=null || movePanel.activeSelf) CancelMove();
            return;
        }
        if(!ValidSource(source)) {CancelMove();return;}
        Mouse mouse=Mouse.current;
        if(mouse==null) return;
        Vector2 pointer=mouse.position.ReadValue();
        BuildGrid grid=source.Grid;
        if(!worldCamera.pixelRect.Contains(pointer) || IsOverUI(pointer)) {HidePreview(grid);return;}
        Ray ray=worldCamera.ScreenPointToRay(pointer);
        Plane plane=new Plane(Vector3.forward,new Vector3(0f,0f,grid.transform.position.z));
        if(!plane.Raycast(ray,out float distance)) {HidePreview(grid);return;}
        Vector3 world=ray.GetPoint(distance);
        Vector3 local=world-grid.transform.position;
        Vector2Int anchor=new Vector2Int(Mathf.FloorToInt(local.x/grid.CellSize),Mathf.FloorToInt(local.y/grid.CellSize));
        bool valid=grid.CanMove(source,anchor,out string reason);
        rangePreviewVisible=true;rangePreviewValid=valid;rangePreviewAnchor=anchor;
        BuildSlot slot=grid.FindCell(anchor);
        grid.ShowPreview(slot,source.SizeInCells,valid);
        if(ghost!=null)
        {
            ghost.gameObject.SetActive(true);
            ghost.transform.position=grid.GetPlacementCenter(anchor,source.SizeInCells)+visualOffset;
            ghost.color=valid?new Color(0.3f,1f,0.4f,0.55f):new Color(1f,0.25f,0.25f,0.55f);
        }
        statusText.text=valid?"Chuột trái để đặt. Esc hoặc Hủy để giữ nguyên. Di chuyển miễn phí.":reason;
        if(Time.frameCount<=beganFrame || !mouse.leftButton.wasPressedThisFrame) return;
        buildManager.BlockMoveClickForFrame();
        if(!valid) {GameAudio.Play(GameSound.Reject);return;}
        if(!grid.TryMove(source,anchor,out reason)) {statusText.text=reason;return;}
        GameAudio.Play(GameSound.Placement);
        CancelMove();
    }
    private bool IsOverUI(Vector2 point)
    {
        if(EventSystem.current==null) return false;
        var data=new PointerEventData(EventSystem.current){position=point};
        hits.Clear();EventSystem.current.RaycastAll(data,hits);
        foreach(var hit in hits) if(hit.module is GraphicRaycaster) return true;
        return false;
    }
    private void HidePreview(BuildGrid grid)
    {
        rangePreviewVisible=false;
        grid.ClearPreview();if(ghost!=null) ghost.gameObject.SetActive(false);
    }
    public void CancelMove()
    {
        rangePreviewVisible=false;
        bool hadMove=source!=null || ghost!=null || (movePanel!=null && movePanel.activeSelf);
        source=null;
        if(ghost!=null) {ghost.gameObject.SetActive(false);Destroy(ghost.gameObject);ghost=null;}
        if(movePanel!=null) movePanel.SetActive(false);
        if(hadMove && buildManager!=null)
        {
            buildManager.BlockMoveClickForFrame();
            if(buildManager.CheckpointGrid!=null) buildManager.CheckpointGrid.SetBuildMode(false);
        }
    }
    private void CloseUpgradePanels()
    {
        foreach(TowerUpgradeUI ui in Object.FindObjectsByType<TowerUpgradeUI>())
            if(ui.gameObject.scene==gameObject.scene) ui.ClosePanel();
        foreach(ProducerUpgradeUI ui in Object.FindObjectsByType<ProducerUpgradeUI>())
            if(ui.gameObject.scene==gameObject.scene) ui.ClosePanel();
        foreach(SupportUpgradeUI ui in Object.FindObjectsByType<SupportUpgradeUI>())
            if(ui.gameObject.scene==gameObject.scene) ui.ClosePanel();
    }
    private void OnDisable()=>CancelMove();
    private void OnDestroy()
    {
        if(cancelButton!=null) cancelButton.onClick.RemoveListener(CancelMove);
        if(current==this) current=null;
    }
}
