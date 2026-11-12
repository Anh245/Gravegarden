using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProducerUpgradeUI : MonoBehaviour
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

    private ProducerBuilding selectedProducer;
    [Header("Bán công trình")]
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellButtonText;
    [Header("Di chuyển công trình")]
    [SerializeField] private BuildingMoveController moveController;
    [SerializeField] private Button moveButton;
    private bool initialized;

    private bool CanInteract =>
        initialized
        && defendedHall != null
        && !defendedHall.IsDestroyed
        && waveDirector != null
        && waveDirector.CanBuildNow
        && Time.timeScale > 0f;

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        if (wallet == null
            || defendedHall == null
            || waveDirector == null
            || panel == null
            || infoText == null
            || upgradeButton == null
            || closeButton == null
            || buildPanel == null
            || towerUpgradeUI == null)
        {
            Debug.LogError(
                "ProducerUpgradeUI: thiếu tham chiếu trong Inspector.",
                this);

            enabled = false;
            return;
        }

        if(buildManager==null || sellButton==null || sellButtonText==null)
        {
            Debug.LogError("ProducerUpgradeUI: gán Build Manager, Sell Button và Sell Button Text.",this);
            enabled=false;return;
        }
        if(moveController==null || moveButton==null)
        {
            Debug.LogError("ProducerUpgradeUI: gán Move Controller và Move Button.",this);
            enabled=false;return;
        }
        moveButton.onClick.AddListener(MoveSelected);
        sellButton.onClick.AddListener(SellSelected);
        upgradeButton.onClick.AddListener(UpgradeSelected);
        closeButton.onClick.AddListener(ClosePanel);

        ProducerBuilding.UpgradeRequested += SelectProducer;
        TowerUpgradeClick.TowerClicked += OnTowerSelected;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized || !panel.activeSelf)
            return;

        if (!CanInteract
            || selectedProducer == null
            || !selectedProducer.isActiveAndEnabled
            || buildPanel.activeInHierarchy)
        {
            ClosePanel();
            return;
        }

        RefreshPanel();
    }

    private void SelectProducer(ProducerBuilding producer)
    {
        if (!CanInteract
            || producer == null
            || !producer.isActiveAndEnabled
            || producer.Definition == null
            || buildPanel.activeInHierarchy)
        {
            return;
        }

        towerUpgradeUI.ClosePanel();

        selectedProducer = producer;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        RefreshPanel();
    }

    private void OnTowerSelected(BoneLilyTower tower)
    {
        ClosePanel();
    }

    private void RefreshPanel()
    {
        if (selectedProducer == null)
            return;

        BuildingPlacement placement = selectedProducer.GetComponent<BuildingPlacement>();
        sellButtonText.text=BuildingSale.ButtonText(buildManager,placement,out bool canSell);
        sellButton.interactable=CanInteract && canSell;
        moveButton.interactable=CanInteract && moveController.CanBegin(placement);
        ProducerDefinition definition =
            selectedProducer.Definition;

        string content =
            $"{definition.DisplayName}\n" +
            $"Cấp: {selectedProducer.Level}/{definition.MaxLevel}\n" +
            $"Sản xuất: " +
            $"{selectedProducer.CurrentProductionPerSecond:0.##} xu/giây\n" +
            $"Trong kho: {selectedProducer.CollectableAmount}/" +
            $"{selectedProducer.CurrentCapacity}\n";

        if(statRows!=null && statRows.IsConfigured){statRows.ShowProducer(selectedProducer);content="";}

        if (selectedProducer.IsMaxLevel)
        {
            infoText.text = content + "Đã đạt cấp tối đa.";
            upgradeButton.interactable = false;
            return;
        }

        int nextLevel = selectedProducer.Level + 1;
        int cost = selectedProducer.NextUpgradeCost;

        float nextProduction =
            definition.GetProductionAtLevel(nextLevel);

        int nextCapacity =
            definition.GetCapacityAtLevel(nextLevel);

        bool enoughMoney = wallet.Banlance >= cost;
        bool canUpgrade = selectedProducer.CanUpgradeNow;

        string status;

        if (!canUpgrade)
            status = "Hiện chưa thể nâng cấp.";
        else if (!enoughMoney)
            status = $"Còn thiếu {cost - wallet.Banlance} Xu Âm Giới.";
        else
            status = "Có thể nâng cấp.";

        infoText.text =
            content +
            $"Cấp tiếp: {nextProduction:0.##} xu/giây\n" +
            $"Sức chứa cấp tiếp: {nextCapacity}\n" +
            $"Giá nâng cấp: {cost} Xu Âm Giới\n" +
            status;

        upgradeButton.interactable =
            CanInteract && canUpgrade && enoughMoney;
    }

    private void UpgradeSelected()
    {
        if (!CanInteract
            || selectedProducer == null
            || buildPanel.activeInHierarchy)
        {
            return;
        }

        selectedProducer.TryUpgrade();
        RefreshPanel();
    }

    private void MoveSelected()
    {
        if(!CanInteract || selectedProducer==null || buildPanel.activeInHierarchy) return;
        moveController.BeginMove(selectedProducer.GetComponent<BuildingPlacement>());
    }

    private void SellSelected()
    {
        if(!CanInteract || selectedProducer==null || buildPanel.activeInHierarchy) return;
        BuildingPlacement placement=selectedProducer.GetComponent<BuildingPlacement>();
        if(!BuildingSale.TrySell(buildManager,placement,out string error))
            Debug.LogWarning("Bán công trình: "+error,this);
    }

    public void ClosePanel()
    {
        selectedProducer = null;

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if(moveButton!=null) moveButton.onClick.RemoveListener(MoveSelected);
        if(sellButton!=null) sellButton.onClick.RemoveListener(SellSelected);
        ProducerBuilding.UpgradeRequested -= SelectProducer;
        TowerUpgradeClick.TowerClicked -= OnTowerSelected;

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(UpgradeSelected);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }
}