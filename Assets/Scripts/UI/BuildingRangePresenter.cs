using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(200)]
public class BuildingRangePresenter : MonoBehaviour
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private BuildingMoveController moveController;
    [SerializeField] private TowerUpgradeUI towerUI;
    [SerializeField] private SupportUpgradeUI supportUI;
    [SerializeField] private Material ringMaterial;
    [SerializeField,Range(32,192)] private int segments=96;
    [SerializeField,Min(0.005f)] private float lineWidth=0.045f;
    [SerializeField] private string sortingLayerName="Default";
    [SerializeField] private int sortingOrder=50;
    [SerializeField] private Color towerColor=new Color(0.35f,0.85f,1f,0.85f);
    [SerializeField] private Color supportColor=new Color(0.8f,0.5f,1f,0.85f);
    [SerializeField] private Color validColor=new Color(0.3f,1f,0.45f,0.85f);
    [SerializeField] private Color invalidColor=new Color(1f,0.3f,0.3f,0.85f);
    private LineRenderer ring;
    private Vector3[] points;

    private void Awake()
    {
        if(buildManager==null || moveController==null || towerUI==null || supportUI==null || ringMaterial==null)
        {Debug.LogError("BuildingRangePresenter: gán đủ hệ thống và Ring Material.",this);enabled=false;return;}
        var child=new GameObject("RangeRing");child.transform.SetParent(transform,false);
        ring=child.AddComponent<LineRenderer>();
        ring.sharedMaterial=ringMaterial;ring.loop=true;ring.useWorldSpace=true;
        ring.alignment=LineAlignment.View;ring.widthMultiplier=lineWidth;
        ring.widthCurve=AnimationCurve.Constant(0f,1f,1f);
        ring.shadowCastingMode=ShadowCastingMode.Off;ring.receiveShadows=false;
        ring.sortingLayerName=sortingLayerName;ring.sortingOrder=sortingOrder;
        ring.enabled=false;
        points=new Vector3[Mathf.Clamp(segments,32,192)];ring.positionCount=points.Length;
    }
    private void LateUpdate()
    {
        if(ring==null) return;
        if(Time.timeScale<=0f || SceneNavigator.IsTransitioning || !buildManager.CheckpointReady
            || buildManager.CheckpointHall==null || buildManager.CheckpointHall.IsDestroyed
            || !buildManager.CheckpointWaves.CanBuildNow)
        {Hide();return;}
        if(buildManager.IsMovingBuilding)
        {
            if(moveController.TryGetRangePreview(out Vector3 center,out float radius,out bool valid))
                Show(center,radius,valid?validColor:invalidColor);
            else Hide();
            return;
        }
        if(buildManager.IsBuildPanelOpen)
        {
            if(buildManager.TryGetRangePreview(out Vector3 center,out float radius,out bool valid))
                Show(center,radius,valid?validColor:invalidColor);
            else Hide();
            return;
        }
        BoneLilyTower tower=towerUI.SelectedTower;
        if(tower!=null && tower.CheckpointReady)
        {Show(tower.transform.position,tower.CurrentEffectiveRange,towerColor);return;}
        SupportBuilding support=supportUI.SelectedSupport;
        if(support!=null && support.CheckpointReady)
        {Show(support.transform.position,support.CurrentEffectiveRange,supportColor);return;}
        Hide();
    }
    private void Show(Vector3 center,float radius,Color color)
    {
        if(radius<=0f || float.IsNaN(radius) || float.IsInfinity(radius)) {Hide();return;}
        // World coordinates: kích thước không phụ thuộc scale của object quản lý.
        for(int i=0;i<points.Length;i++)
        {
            float angle=i*2f*Mathf.PI/points.Length;
            points[i]=center+new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0f);
        }
        ring.SetPositions(points);ring.startColor=color;ring.endColor=color;
        ring.widthMultiplier=lineWidth;ring.enabled=true;
    }
    private void Hide(){if(ring!=null) ring.enabled=false;}
    private void OnDisable()=>Hide();
}
