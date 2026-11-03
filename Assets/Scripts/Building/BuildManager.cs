using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildManager : MonoBehaviour
{
    [Header("Hệ thống trong scene")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField] private Transform buildingsParent;

    [Header("Danh sách công trình")]
    [SerializeField] private BuildingOption[] buildingOptions;

    [Header("Giao diện")]
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private TMP_Dropdown buildingDropdown;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button closeButton;

    private readonly List<ProducerBuilding> builtProducers =
        new List<ProducerBuilding>();

    private BuildSlot selectedSlot;
    private bool initialized;

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

        var names = new List<string>();

        foreach (BuildingOption option in buildingOptions)
        {
            names.Add(option.DisplayName);
        }

        buildingDropdown.ClearOptions();
        buildingDropdown.AddOptions(names);
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
            waveDirector == null ||
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

        if (buildingOptions == null ||
            buildingOptions.Length == 0)
        {
            Debug.LogError(
                "Danh sách công trình đang trống.",
                this
            );

            return false;
        }

        for (int i = 0; i < buildingOptions.Length; i++)
        {
            BuildingOption option = buildingOptions[i];

            if (option == null || !option.IsValid)
            {
                Debug.LogError(
                    $"Building Options, phần tử {i} không hợp lệ. " +
                    "Chỉ gán Tower hoặc Producer, " +
                    "và kiểm tra dữ liệu của prefab.",
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

        if (!waveDirector.CanBuildNow ||
            selectedSlot == null ||
            selectedSlot.IsOccupied)
        {
            ClosePanel();
            return;
        }

        RefreshPanel();
    }

    public void SelectSlot(BuildSlot slot)
    {
        if (!initialized ||
            !waveDirector.CanBuildNow ||
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
        if (initialized && buildPanel.activeSelf)
            RefreshPanel();
    }

    private bool IsUnlocked(BuildingOption option)
    {
        return waveDirector.AvailableWaveNumber
            >= option.UnlockWave;
    }

    private int CountOwnedProducers(
        ProducerDefinition definition)
    {
        int count = 0;

        foreach (ProducerBuilding producer in builtProducers)
        {
            if (producer != null &&
                producer.gameObject.activeInHierarchy &&
                producer.Definition == definition)
            {
                count++;
            }
        }

        return count;
    }

    private int GetPurchaseCost(BuildingOption option)
    {
        if (!option.IsProducer)
            return option.BaseCost;

        int copies = CountOwnedProducers(
            option.Producer.Definition
        );

        double price =
            option.BaseCost * Math.Pow(1.18, copies);

        return (int)Math.Round(
            price,
            MidpointRounding.AwayFromZero
        );
    }

    private void RefreshPanel()
    {
        BuildingOption option =
            buildingOptions[buildingDropdown.value];

        int cost = GetPurchaseCost(option);

        bool unlocked = IsUnlocked(option);
        bool canAfford = wallet.Banlance >= cost;
        bool canBuildNow = waveDirector.CanBuildNow;

        string status;

        if (!canBuildNow)
        {
            status = "Hiện không thể xây công trình.";
        }
        else if (!unlocked)
        {
            status =
                $"Chưa mở khóa. Mở từ đợt {option.UnlockWave}.";
        }
        else if (!canAfford)
        {
            status = "Không đủ tiền.";
        }
        else
        {
            status = "Có thể xây.";
        }

        infoText.text =
            $"{option.DisplayName}\n" +
            $"Giá: {cost} tiền\n" +
            $"Bạn có: {wallet.Banlance} tiền\n" +
            status;

        buildButton.interactable =
            canBuildNow &&
            unlocked &&
            canAfford &&
            selectedSlot != null &&
            !selectedSlot.IsOccupied;
    }

    private void BuildSelectedSlot()
    {
        if (!initialized ||
            !waveDirector.CanBuildNow ||
            selectedSlot == null ||
            selectedSlot.IsOccupied)
        {
            return;
        }

        BuildingOption option =
            buildingOptions[buildingDropdown.value];

        if (!IsUnlocked(option))
        {
            RefreshPanel();
            return;
        }

        // Tính lại giá ngay lúc xác nhận mua.
        int cost = GetPurchaseCost(option);

        if (!wallet.TrySpend(cost))
        {
            RefreshPanel();
            return;
        }

        Vector3 position = selectedSlot.BuildPosition;

        if (option.IsProducer)
        {
            ProducerBuilding producer = Instantiate(
                option.Producer,
                position,
                Quaternion.identity,
                buildingsParent
            );

            producer.Configure(
                wallet,
                defendedHall,
                waveDirector
            );

            builtProducers.Add(producer);
        }
        else
        {
            BoneLilyTower tower = Instantiate(
                option.Tower,
                position,
                Quaternion.identity,
                buildingsParent
            );

            tower.Configure(defendedHall);
        }

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