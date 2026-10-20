using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildManager : MonoBehaviour
{
    [Header("Hệ thống trong scene")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private Transform buildingsParent;

    [Header("Danh sách tháp")]
    [SerializeField] private BoneLilyTower[] towerPrefabs;

    [Header("Giao diện")]
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private TMP_Dropdown buildingDropdown;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button closeButton;

    private BuildSlot selectedSlot;
    private bool initialized;

    private int[] buildCosts;
    private string[] displayNames;

    private void Start()
    {
        if (!ValidateSetup())
        {
            if (buildPanel != null)
                buildPanel.SetActive(false);

            if (buildButton != null)
                buildButton.interactable = false;

            enabled = false;
            return;
        }

        buildCosts = new int[towerPrefabs.Length];
        displayNames = new string[towerPrefabs.Length];

        var options = new List<string>();

        for (int i = 0; i < towerPrefabs.Length; i++)
        {
            ProjectileTowerDefinition data =
                towerPrefabs[i].Definition;

            buildCosts[i] = data.BuildCost;
            displayNames[i] = data.DisplayName;

            options.Add(displayNames[i]);
        }

        buildingDropdown.ClearOptions();
        buildingDropdown.AddOptions(options);
        buildingDropdown.SetValueWithoutNotify(0);
        buildingDropdown.RefreshShownValue();

        buildingDropdown.onValueChanged.AddListener(
            OnSelectionChanged
        );

        buildButton.onClick.AddListener(BuildSelectedSlot);
        closeButton.onClick.AddListener(ClosePanel);

        initialized = true;
        ClosePanel();
    }

    private bool ValidateSetup()
    {
        if (wallet == null ||
            defendedHall == null ||
            buildingsParent == null ||
            buildPanel == null ||
            buildingDropdown == null ||
            infoText == null ||
            buildButton == null ||
            closeButton == null)
        {
            Debug.LogError(
                "BuildManager chưa được gán đủ tham chiếu.",
                this
            );

            return false;
        }

        if (!buildingsParent.gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "Object Buildings đang bị tắt.",
                this
            );

            return false;
        }

        if (towerPrefabs == null || towerPrefabs.Length == 0)
        {
            Debug.LogError(
                "BuildManager chưa có danh sách tháp.",
                this
            );

            return false;
        }

        foreach (BoneLilyTower prefab in towerPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError(
                    "Danh sách tháp có phần tử chưa được gán.",
                    this
                );

                return false;
            }

            if (!prefab.gameObject.activeSelf ||
                !prefab.enabled ||
                prefab.Definition == null ||
                !prefab.Definition.IsValid)
            {
                Debug.LogError(
                    $"Prefab '{prefab.name}' bị tắt " +
                    "hoặc thiếu dữ liệu hợp lệ.",
                    this
                );

                return false;
            }
        }

        return true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (defendedHall.IsDestroyed)
        {
            StopBuilding();
            return;
        }

        if (!buildPanel.activeSelf)
            return;

        if (selectedSlot == null || selectedSlot.IsOccupied)
        {
            ClosePanel();
            return;
        }

        RefreshPanel();
    }

    public void SelectSlot(BuildSlot slot)
    {
        if (!initialized ||
            defendedHall.IsDestroyed ||
            slot == null ||
            slot.IsOccupied)
        {
            return;
        }

        selectedSlot = slot;

        buildPanel.SetActive(true);
        RefreshPanel();
    }

    private void OnSelectionChanged(int index)
    {
        if (!initialized || !buildPanel.activeSelf)
            return;

        RefreshPanel();
    }

    private void RefreshPanel()
    {
        int index = buildingDropdown.value;
        int cost = buildCosts[index];

        bool canAfford = wallet.Banlance >= cost;

        infoText.text =
            $"{displayNames[index]}\n" +
            $"Giá: {cost} tiền\n" +
            $"Bạn có: {wallet.Banlance} tiền\n" +
            (canAfford
                ? "Có thể xây."
                : "Không đủ tiền.");

        buildButton.interactable =
            canAfford &&
            selectedSlot != null &&
            !selectedSlot.IsOccupied &&
            !defendedHall.IsDestroyed;
    }

    private void BuildSelectedSlot()
    {
        if (!initialized ||
            defendedHall.IsDestroyed ||
            selectedSlot == null ||
            selectedSlot.IsOccupied)
        {
            return;
        }

        int index = buildingDropdown.value;

        BoneLilyTower prefab = towerPrefabs[index];
        int cost = buildCosts[index];

        if (!wallet.TrySpend(cost))
        {
            RefreshPanel();
            return;
        }

        BoneLilyTower tower = Instantiate(
            prefab,
            selectedSlot.BuildPosition,
            Quaternion.identity,
            buildingsParent
        );

        tower.Configure(defendedHall);

        selectedSlot.MarkOccupied();

        ClosePanel();
    }

    private void ClosePanel()
    {
        buildingDropdown.Hide();

        selectedSlot = null;
        buildPanel.SetActive(false);
    }

    public void StopBuilding()
    {
        initialized = false;

        if (buildButton != null)
            buildButton.interactable = false;

        if (buildingDropdown != null)
            buildingDropdown.Hide();

        selectedSlot = null;

        if (buildPanel != null)
            buildPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (buildingDropdown != null)
        {
            buildingDropdown.onValueChanged.RemoveListener(
                OnSelectionChanged
            );
        }

        if (buildButton != null)
        {
            buildButton.onClick.RemoveListener(
                BuildSelectedSlot
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                ClosePanel
            );
        }
    }
}