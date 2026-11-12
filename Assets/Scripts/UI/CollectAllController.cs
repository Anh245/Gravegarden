using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1200)]
public class CollectAllController : MonoBehaviour
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private GardenSaveController saveController;
    [SerializeField] private Button collectAllButton;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text statusText;
    private bool initialized;
    private float nextRefresh;
    private readonly List<ProducerBuilding> producers=new List<ProducerBuilding>();
    private readonly List<double> amounts=new List<double>();
    private static readonly CultureInfo Vietnamese=CultureInfo.GetCultureInfo("vi-VN");
    private bool CanCollect=>initialized && isActiveAndEnabled && buildManager.CheckpointReady
        && !buildManager.MoveInputBlocked && !SceneNavigator.IsTransitioning && Time.timeScale>0f
        && !buildManager.CheckpointHall.IsDestroyed && buildManager.CheckpointWaves.CanBuildNow
        && !saveController.IsOfflineDialogOpen;

    private void Start()
    {
        if(buildManager==null || !buildManager.CheckpointReady || saveController==null || collectAllButton==null || buttonText==null
            || collectAllButton.gameObject==gameObject)
        {
            Debug.LogError("CollectAllController: gán Build Manager, Save Controller, Button, Button Text; controller phải nằm ngoài nút.",this);
            enabled=false;return;
        }
        collectAllButton.onClick.AddListener(CollectAll);initialized=true;
        if(statusText!=null) statusText.text="";
        Refresh();
    }
    private void Update()
    {
        if(!initialized) return;
        if(Time.unscaledTime>=nextRefresh) {nextRefresh=Time.unscaledTime+0.15f;Refresh();}
        collectAllButton.interactable=collectAllButton.interactable && CanCollect;
    }
    private void Snapshot()
    {
        producers.Clear();amounts.Clear();
        if(!buildManager.CheckpointReady || buildManager.CheckpointBuildingsParent==null) return;
        foreach(ProducerBuilding producer in buildManager.CheckpointBuildingsParent.GetComponentsInChildren<ProducerBuilding>())
        {
            BuildingPlacement placement=producer.GetComponent<BuildingPlacement>();
            if(placement==null || !placement.IsPlaced || placement.SaleClaimed || placement.Grid!=buildManager.CheckpointGrid
                || producer.gameObject.scene!=gameObject.scene
                || !producer.BelongsTo(buildManager.CheckpointWallet,buildManager.CheckpointHall,buildManager.CheckpointWaves)) continue;
            producers.Add(producer);amounts.Add(producer.StoredAmount);
        }
    }
    private void Refresh()
    {
        if(!initialized) return;
        Snapshot();
        // Giống Godot: nút xuất hiện khi đã có ít nhất hai producer.
        collectAllButton.gameObject.SetActive(producers.Count>=2);
        bool valid=HarvestMath.TryTotal(amounts,buildManager.CheckpointWallet.Banlance,out int total,out _,out _);
        collectAllButton.interactable=CanCollect && valid && total>0 && producers.Count>=2;
        buttonText.text=!valid?"Thu hoạch tất cả\nChưa thể nhận vào ví"
            :total>0?"Thu hoạch tất cả\n+"+total.ToString("N0",Vietnamese)+" xu":"Thu hoạch tất cả\nChưa có xu để thu";
    }
    public void CollectAll()
    {
        if(!CanCollect) return;
        Snapshot();
        if(producers.Count<2) {Refresh();return;}
        if(!HarvestMath.TryTotal(amounts,buildManager.CheckpointWallet.Banlance,out int expected,out _,out string error))
        {Show(error);Refresh();return;}
        if(expected<=0) {Show("Kho chưa có xu nguyên để thu.");Refresh();return;}
        int total=0,count=0;
        foreach(ProducerBuilding producer in producers)
        {
            if(!producer.BelongsTo(buildManager.CheckpointWallet,buildManager.CheckpointHall,buildManager.CheckpointWaves)) continue;
            if(producer.TryCollect(out int collected,false)) {total+=collected;count++;}
        }
        if(total>0) {GameAudio.Play(GameSound.Collect);Show("Đã thu "+total.ToString("N0",Vietnamese)+" xu từ "+count+" công trình.");}
        Refresh();
    }
    private void Show(string text){if(statusText!=null)statusText.text=text;Debug.Log("Thu hoạch tất cả: "+text,this);}
    private void OnEnable(){if(initialized)Refresh();}
    private void OnDisable(){if(collectAllButton!=null)collectAllButton.interactable=false;}
    private void OnDestroy(){if(collectAllButton!=null)collectAllButton.onClick.RemoveListener(CollectAll);}
}
