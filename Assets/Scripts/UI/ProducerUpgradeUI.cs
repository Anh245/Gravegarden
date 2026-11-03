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
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;

    [Header("Các bảng khác")]
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private TowerUpgradeUI towerUpgradeUI;

    private ProducerBuilding selectedProducer;
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

        ProducerDefinition definition =
            selectedProducer.Definition;

        string content =
            $"{definition.DisplayName}\n" +
            $"Cấp: {selectedProducer.Level}/{definition.MaxLevel}\n" +
            $"Sản xuất: " +
            $"{selectedProducer.CurrentProductionPerSecond:0.##} xu/giây\n" +
            $"Trong kho: {selectedProducer.CollectableAmount}/" +
            $"{selectedProducer.CurrentCapacity}\n";

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

    public void ClosePanel()
    {
        selectedProducer = null;

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDestroy()
    {
        ProducerBuilding.UpgradeRequested -= SelectProducer;
        TowerUpgradeClick.TowerClicked -= OnTowerSelected;

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(UpgradeSelected);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }
}