using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SupportUpgradeUI : MonoBehaviour
{
    [Header("Hệ thống trận đấu")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private WaveDirector waveDirector;
    [Header("Giao diện")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private BuildingStatRows statRows;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;
    [Header("Các bảng khác")]
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private TowerUpgradeUI towerUpgradeUI;
    [SerializeField] private ProducerUpgradeUI producerUpgradeUI;

    private SupportBuilding selectedSupport;
    [Header("Bán công trình")]
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellButtonText;
    [Header("Di chuyển công trình")]
    [SerializeField] private BuildingMoveController moveController;
    [SerializeField] private Button moveButton;
    public SupportBuilding SelectedSupport => initialized && isActiveAndEnabled && panel!=null && panel.activeInHierarchy ? selectedSupport:null;
    private bool initialized;
    private bool CanInteract => initialized && isActiveAndEnabled
        && defendedHall != null && !defendedHall.IsDestroyed
        && waveDirector != null && waveDirector.CanBuildNow && Time.timeScale > 0f;

    private void OnEnable()
    {
        SupportBuilding.UpgradeRequested += SelectSupport;
        TowerUpgradeClick.TowerClicked += OnTowerSelected;
        ProducerBuilding.UpgradeRequested += OnProducerSelected;
    }

    private void Start()
    {
        ClosePanel();
        if (wallet == null || defendedHall == null || waveDirector == null
            || panel == null || infoText == null || upgradeButton == null
            || closeButton == null || buildPanel == null
            || towerUpgradeUI == null || producerUpgradeUI == null)
        {
            Debug.LogError("SupportUpgradeUI: thiếu tham chiếu trong Inspector.", this);
            enabled = false;
            return;
        }
        if(buildManager==null || sellButton==null || sellButtonText==null)
        {
            Debug.LogError("SupportUpgradeUI: gán Build Manager, Sell Button và Sell Button Text.",this);
            enabled=false;return;
        }
        if(moveController==null || moveButton==null)
        {
            Debug.LogError("SupportUpgradeUI: gán Move Controller và Move Button.",this);
            enabled=false;return;
        }
        moveButton.onClick.AddListener(MoveSelected);
        sellButton.onClick.AddListener(SellSelected);
        upgradeButton.onClick.AddListener(UpgradeSelected);
        closeButton.onClick.AddListener(ClosePanel);
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || !panel.activeSelf)
            return;
        if (!CanInteract || selectedSupport == null || !selectedSupport.CanOperate
            || buildPanel.activeInHierarchy)
        {
            ClosePanel();
            return;
        }
        RefreshPanel();
    }

    private void SelectSupport(SupportBuilding support)
    {
        if (!CanInteract || support == null || !support.CanOperate || buildPanel.activeInHierarchy)
            return;
        towerUpgradeUI.ClosePanel();
        producerUpgradeUI.ClosePanel();
        selectedSupport = support;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        RefreshPanel();
    }

    private void RefreshPanel()
    {
        if (selectedSupport == null)
            return;
        BuildingPlacement placement = selectedSupport.GetComponent<BuildingPlacement>();
        sellButtonText.text=BuildingSale.ButtonText(buildManager,placement,out bool canSell);
        sellButton.interactable=CanInteract && canSell;
        moveButton.interactable=CanInteract && moveController.CanBegin(placement);
        SupportDefinition data = selectedSupport.Definition;
        string content = $"{data.DisplayName}\n"
            + $"Cấp: {selectedSupport.Level}/{data.MaxLevel}\n"
            + $"Tốc độ đánh tháp gần đó: +{selectedSupport.CurrentBonus * 100f:0}%\n"
            + $"Bán kính: {data.Range:0.##}\nChỉ lấy hiệu ứng mạnh nhất.\n";
        if(statRows!=null && statRows.IsConfigured){statRows.ShowSupport(selectedSupport);content="";}

        if (selectedSupport.IsMaxLevel)
        {
            infoText.text = content + "Đã đạt cấp tối đa.";
            upgradeButton.interactable = false;
            return;
        }
        int cost = selectedSupport.NextUpgradeCost;
        bool canUpgrade = CanInteract && selectedSupport.CanUpgradeNow;
        bool enoughMoney = wallet.Banlance >= cost;
        string status = !canUpgrade ? "Hiện chưa thể nâng cấp."
            : !enoughMoney ? $"Còn thiếu {cost - wallet.Banlance} Xu Âm Giới."
            : "Có thể nâng cấp.";
        infoText.text = content
            + $"Cấp tiếp: +{data.GetBonusAtLevel(selectedSupport.Level + 1) * 100f:0}%\n"
            + $"Giá nâng cấp: {cost} Xu Âm Giới\n" + status;
        upgradeButton.interactable = canUpgrade && enoughMoney;
    }

    private void UpgradeSelected()
    {
        if (!CanInteract || selectedSupport == null || buildPanel.activeInHierarchy)
            return;
        selectedSupport.TryUpgrade();
        RefreshPanel();
    }

    private void OnTowerSelected(BoneLilyTower tower) => ClosePanel();
    private void OnProducerSelected(ProducerBuilding producer) => ClosePanel();

    private void MoveSelected()
    {
        if(!CanInteract || selectedSupport==null || buildPanel.activeInHierarchy) return;
        moveController.BeginMove(selectedSupport.GetComponent<BuildingPlacement>());
    }

    private void SellSelected()
    {
        if(!CanInteract || selectedSupport==null || buildPanel.activeInHierarchy) return;
        BuildingPlacement placement=selectedSupport.GetComponent<BuildingPlacement>();
        if(!BuildingSale.TrySell(buildManager,placement,out string error))
            Debug.LogWarning("Bán công trình: "+error,this);
    }

    public void ClosePanel()
    {
        selectedSupport = null;
        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDisable()
    {
        SupportBuilding.UpgradeRequested -= SelectSupport;
        TowerUpgradeClick.TowerClicked -= OnTowerSelected;
        ProducerBuilding.UpgradeRequested -= OnProducerSelected;
        ClosePanel();
    }

    private void OnDestroy()
    {
        if(moveButton!=null) moveButton.onClick.RemoveListener(MoveSelected);
        if(sellButton!=null) sellButton.onClick.RemoveListener(SellSelected);
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(UpgradeSelected);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }
}
