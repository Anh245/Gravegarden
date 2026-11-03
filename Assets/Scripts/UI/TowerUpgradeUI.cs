using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerUpgradeUI : MonoBehaviour
{
    [Header("Hệ thống trận đấu")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private WaveDirector waveDirector;

    [Header("Giao diện nâng cấp")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;

    [Header("Bảng xây dựng hiện có")]
    [SerializeField] private GameObject buildPanel;

    private BoneLilyTower selectedTower;
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
            || buildPanel == null)
        {
            Debug.LogError(
                "TowerUpgradeUI: thiếu tham chiếu trong Inspector.",
                this);

            enabled = false;
            return;
        }

        upgradeButton.onClick.AddListener(UpgradeSelected);
        closeButton.onClick.AddListener(ClosePanel);
        TowerUpgradeClick.TowerClicked += SelectTower;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized || !panel.activeSelf)
            return;

        if (!CanInteract
            || selectedTower == null
            || !selectedTower.isActiveAndEnabled
            || buildPanel.activeInHierarchy)
        {
            ClosePanel();
            return;
        }

        RefreshPanel();
    }

    private void SelectTower(BoneLilyTower tower)
    {
        if (!CanInteract
            || tower == null
            || tower.Definition == null
            || !tower.isActiveAndEnabled)
        {
            return;
        }

        // Đóng bảng xây dựng trước khi chọn tháp.
        if (buildPanel.activeInHierarchy)
            return;

        selectedTower = tower;

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        RefreshPanel();
    }

    private void RefreshPanel()
    {
        if (selectedTower == null)
            return;

        ProjectileTowerDefinition definition = selectedTower.Definition;

        string content =
            $"{definition.DisplayName}\n" +
            $"Cấp: {selectedTower.Level}/{definition.MaxLevel}\n" +
            $"Sát thương: {selectedTower.CurrentDamage}\n";

        if (selectedTower.IsMaxLevel)
        {
            infoText.text = content + "Đã đạt cấp tối đa.";
            upgradeButton.interactable = false;
            return;
        }

        int cost = selectedTower.NextUpgradeCost;

        int nextDamage = definition.GetDamageAtLevel(
            selectedTower.Level + 1);

        bool enoughMoney = wallet.Banlance >= cost;
        bool canUpgrade = selectedTower.CanUpgradeNow(waveDirector);

        string status;

        if (!canUpgrade)
            status = "Hiện chưa thể nâng cấp.";
        else if (!enoughMoney)
            status = $"Còn thiếu {cost - wallet.Banlance} Xu Âm Giới.";
        else
            status = "Có thể nâng cấp.";

        infoText.text =
            content +
            $"Sát thương cấp tiếp: {nextDamage}\n" +
            $"Giá nâng cấp: {cost} Xu Âm Giới\n" +
            status;

        upgradeButton.interactable =
            CanInteract && canUpgrade && enoughMoney;
    }

    private void UpgradeSelected()
    {
        if (!CanInteract
            || selectedTower == null
            || buildPanel.activeInHierarchy)
        {
            return;
        }

        selectedTower.TryUpgrade(wallet, waveDirector);
        RefreshPanel();
    }

    public void ClosePanel()
    {
        selectedTower = null;

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDestroy()
    {
        TowerUpgradeClick.TowerClicked -= SelectTower;

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(UpgradeSelected);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }
}