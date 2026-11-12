using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1100)]
public class GardenTutorialController : MonoBehaviour
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private BuildModeController buildMode;
    [SerializeField] private WaveDirector waves;
    [SerializeField] private GardenSaveController save;
    [SerializeField] private string orchardId = "bone_orchard";
    [SerializeField] private string lilyId = "bone_lily";
    private BuildingOption orchardOption;
    private BuildingOption lilyOption;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text body;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private Button skipButton;
    private readonly GardenTutorialProgress progress = new GardenTutorialProgress();
    public GardenTutorialProgress.Step CurrentStep => progress.Current;
    private bool initialized;
    private bool subscribed;
    public bool BlocksWaveStart => initialized && isActiveAndEnabled && progress.BlocksWave;
    public bool Allows(BuildingOption option) => !BlocksWaveStart || (option!=null && progress.Allows(option.SaveId==orchardId,option.SaveId==lilyId));

    private void Start()
    {
        if(buildManager!=null && buildManager.CheckpointOptions!=null)
            foreach(var option in buildManager.CheckpointOptions)
            {
                if(option.SaveId==orchardId)orchardOption=option;
                if(option.SaveId==lilyId)lilyOption=option;
            }
        if(buildManager==null || buildMode==null || waves==null || save==null || orchardOption==null || lilyOption==null
            || !orchardOption.IsProducer || lilyOption.IsProducer || lilyOption.IsSupport || panel==null || title==null || body==null
            || actionButton==null || actionText==null || skipButton==null || !buildManager.CheckpointReady || !save.CheckpointReady)
        {Debug.LogError("GardenTutorialController: thiếu tham chiếu hoặc hệ thống trận chưa sẵn sàng.",this);if(panel!=null)panel.SetActive(false);enabled=false;return;}
        initialized=true;
        if(save.DidRestoreCheckpoint || waves.CompletedWaveCount>0 || waves.HasExpansionUnlocked)
        {progress.Skip();panel.SetActive(false);return;}
        buildManager.BuildingPlaced+=OnPlaced;
        waves.ProducerHarvested+=OnHarvested;
        actionButton.onClick.AddListener(Action);
        skipButton.onClick.AddListener(Skip);
        subscribed=true;Refresh();
    }
    private void Update()
    {
        if(!initialized)return;
        if(save.DidRestoreCheckpoint || waves.CompletedWaveCount>0 || waves.HasExpansionUnlocked || waves.HasDefeatedHall || SceneNavigator.IsTransitioning)
        {progress.Skip();panel.SetActive(false);return;}
        bool visible=progress.BlocksWave && Time.timeScale>0 && !save.IsOfflineDialogOpen;
        if(panel.activeSelf!=visible)panel.SetActive(visible);
        actionButton.interactable=visible && progress.Current!=GardenTutorialProgress.Step.Harvest;
    }
    private void Action()
    {
        if(!BlocksWaveStart || Time.timeScale<=0 || save.IsOfflineDialogOpen)return;
        if(progress.Current==GardenTutorialProgress.Step.Welcome)progress.Start();
        else if(progress.Current==GardenTutorialProgress.Step.Ready)progress.Confirm();
        if(progress.Current==GardenTutorialProgress.Step.Orchard)Select(orchardOption);
        else if(progress.Current==GardenTutorialProgress.Step.Lily)Select(lilyOption);
        Refresh();
    }
    private void Select(BuildingOption option)
    {
        if(!buildMode.IsBuildMode)buildMode.ToggleBuildMode();
        for(int i=0;i<buildManager.CheckpointOptions.Count;i++)
            if(buildManager.CheckpointOptions[i]==option){buildManager.TrySelectShopOption(i);return;}
    }
    private void OnPlaced(BuildingPlacement placement)
    {
        if(!BlocksWaveStart || placement==null)return;
        progress.Placed(placement.BuildingId==orchardOption.SaveId,placement.BuildingId==lilyOption.SaveId);
        Refresh();
    }
    private void OnHarvested(ProducerBuilding producer,int amount)
    {
        if(!BlocksWaveStart || producer==null)return;
        var placement=producer.GetComponent<BuildingPlacement>();
        if(placement==null || placement.Grid!=buildManager.CheckpointGrid || placement.BuildingId!=orchardOption.SaveId)return;
        progress.Harvested(amount);Refresh();
    }
    private void Skip(){if(Time.timeScale<=0)return;progress.Skip();Refresh();}
    private void Refresh()
    {
        panel.SetActive(progress.BlocksWave);
        switch(progress.Current)
        {
            case GardenTutorialProgress.Step.Welcome:
                title.text="Mara • Chào mừng đến khu vườn";
                body.text="Ta sẽ cùng chuẩn bị trước đợt đầu: xây Vườn Xương, đặt một tháp Hoa Xương và thu hoạch. Bạn có thể Bỏ qua để chơi tự do.";
                actionText.text="Bắt đầu hướng dẫn";break;
            case GardenTutorialProgress.Step.Orchard:
                title.text="Mara • 1/4: Xây Vườn Xương";
                body.text="Bấm Chọn Vườn Xương, chọn một ô hợp lệ rồi bấm Xây. Công trình này tạo tiền vào kho riêng. Hãy chừa vị trí gần đường cho tháp.";
                actionText.text="Chọn Vườn Xương";break;
            case GardenTutorialProgress.Step.Lily:
                title.text="Mara • 2/4: Xây "+lilyOption.DisplayName;
                body.text="Chọn "+lilyOption.DisplayName+", đặt gần tuyến đường và bấm Xây. Tháp tự chọn địch trong tầm để bắn. Giờ ta đã có nguồn tiền và một lớp phòng thủ.";
                actionText.text="Chọn "+lilyOption.DisplayName;break;
            case GardenTutorialProgress.Step.Harvest:
                title.text="Mara • 3/4: Thu hoạch lần đầu";
                body.text="Chờ Vườn Xương có ít nhất 1 xu rồi bấm chuột trái lên công trình để thu vào ví. Có thể bấm Xong/B để ẩn các ô xây trước khi thu.";
                actionText.text="Đang chờ thu hoạch";break;
            case GardenTutorialProgress.Step.Ready:
                title.text="Mara • 4/4: Sẵn sàng bảo vệ";
                body.text="Bạn đã biết xây và thu hoạch. Bấm Hoàn tất để bắt đầu 20 giây chuẩn bị. Có thể bấm Bắt đầu đợt sớm để nhận thêm xu, hoặc đợi hết giờ. Xem lại sổ tay trong Pause → Hướng dẫn.";
                actionText.text="Hoàn tất hướng dẫn";break;
        }
        actionButton.interactable=progress.Current!=GardenTutorialProgress.Step.Harvest;
    }
    private void OnDisable(){if(panel!=null)panel.SetActive(false);}
    private void OnDestroy()
    {
        if(!subscribed)return;
        if(buildManager!=null)buildManager.BuildingPlaced-=OnPlaced;
        if(waves!=null)waves.ProducerHarvested-=OnHarvested;
        if(actionButton!=null)actionButton.onClick.RemoveListener(Action);
        if(skipButton!=null)skipButton.onClick.RemoveListener(Skip);
    }
}
