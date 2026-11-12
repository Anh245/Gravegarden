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
    [SerializeField] private BuildGrid buildGrid;

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

    public event Action<BuildingPlacement> BuildingPlaced;
    private BuildSlot selectedSlot;
    private bool initialized;
    private BuildingMoveController moveController;
    private int lastMoveInputFrame=-1;
    public bool IsMovingBuilding=>moveController!=null && moveController.IsMoving;
    public bool MoveInputBlocked=>IsMovingBuilding || lastMoveInputFrame==Time.frameCount;
    public bool RegisterMoveController(BuildingMoveController controller)
    {
        if(moveController!=null && moveController!=controller) return false;
        moveController=controller;return true;
    }
    public void BlockMoveClickForFrame()=>lastMoveInputFrame=Time.frameCount;
    public void CancelBuildingMove() {if(moveController!=null) moveController.CancelMove();}
    public bool CheckpointReady => initialized && isActiveAndEnabled;
    public TMP_Dropdown LegacyDropdown=>buildingDropdown;
    public int SelectedShopIndex=>buildingDropdown!=null?buildingDropdown.value:0;
    public int LastPlacedOptionIndex { get; private set; }=-1;
    public float LastPlacementTime { get; private set; }=-100f;
    public bool HasSelectedBuildSlot=>IsBuildPanelOpen && selectedSlot!=null && !selectedSlot.IsOccupied;
    public bool CanUseShop=>CheckpointReady && !MoveInputBlocked && !SceneNavigator.IsTransitioning
        && Time.timeScale>0f && buildGrid.IsBuildMode && waveDirector.CanBuildNow && !defendedHall.IsDestroyed;
    public bool TryGetShopOffer(int index,out int price,out bool unlocked,out int owned)
    {
        price=owned=0;unlocked=false;
        if(!CheckpointReady || index<0 || index>=buildingOptions.Length)return false;
        BuildingOption option=buildingOptions[index];
        if(option==null || !option.IsValid)return false;
        price=GetPurchaseCost(option);unlocked=IsUnlocked(option);
        foreach(BuildingPlacement placement in buildingsParent.GetComponentsInChildren<BuildingPlacement>())
            if(placement.IsPlaced && placement.Grid==buildGrid && placement.BuildingId==option.SaveId && !placement.SaleClaimed)owned++;
        return true;
    }
    public bool TrySelectShopOption(int index)
    {
        if(!CanUseShop || index<0 || index>=buildingOptions.Length || !IsUnlocked(buildingOptions[index]))return false;
        buildingDropdown.SetValueWithoutNotify(index);buildingDropdown.RefreshShownValue();
        if(IsBuildPanelOpen)RefreshPanel();
        return true;
    }
    public bool IsBuildPanelOpen => initialized && buildPanel!=null && buildPanel.activeInHierarchy;
    public bool TryGetRangePreview(out Vector3 center,out float radius,out bool valid)
    {
        center=Vector3.zero;radius=0f;valid=false;
        if(!IsBuildPanelOpen || selectedSlot==null || selectedSlot.Grid!=buildGrid
            || buildingDropdown.value<0 || buildingDropdown.value>=buildingOptions.Length) return false;
        BuildingOption option=buildingOptions[buildingDropdown.value];
        if(option==null || !option.IsValid || option.IsProducer) return false;
        center=buildGrid.GetPlacementCenter(selectedSlot.Cell,option.FootprintCells);
        float baseRange=option.IsSupport?option.Support.Definition.Range:option.Tower.Definition.Range;
        radius=baseRange+option.FootprintCells*buildGrid.CellSize*0.5f;
        valid=buildGrid.CanPlace(selectedSlot,option.FootprintCells,out _);
        return true;
    }
    public BuildGrid CheckpointGrid => buildGrid;
    public Transform CheckpointBuildingsParent => buildingsParent;
    public IReadOnlyList<BuildingOption> CheckpointOptions => buildingOptions;
    public PlayerWallet CheckpointWallet => wallet;
    public TownHallHealth CheckpointHall => defendedHall;
    public WaveDirector CheckpointWaves => waveDirector;
    public void CloseForCheckpoint() { CancelBuildingMove(); ClosePanel(); }
    public void ReplaceCheckpointProducers(List<ProducerBuilding> producers)
    {
        builtProducers.Clear();
        builtProducers.AddRange(producers);
    }

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
            buildGrid == null || !buildGrid.IsReady ||
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

        var saveIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < buildingOptions.Length; i++)
        {
            BuildingOption option = buildingOptions[i];
            if (option != null && (!CheckpointValidator.IsValidId(option.SaveId) || !saveIds.Add(option.SaveId)))
            {
                Debug.LogError($"Building Options phần tử {i}: Save Id trống, bị trùng hoặc sai định dạng.", this);
                return false;
            }


            if (option == null || !option.IsValid)
            {
                Debug.LogError(
                    $"Building Options, phần tử {i} không hợp lệ. " +
                    "Chỉ gán một loại: Tower, Producer hoặc Support, " +
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
        if(MoveInputBlocked) return;
        if (!initialized ||
            !waveDirector.CanBuildNow || Time.timeScale <= 0f || !buildGrid.IsBuildMode ||
            slot == null || slot.Grid != buildGrid ||
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
        return waveDirector.TutorialAllowsBuilding(option) && waveDirector.AvailableWaveNumber
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
        int copies=option.IsProducer?CountOwnedProducers(option.Producer.Definition):0;
        return ShopCardPolicy.PurchasePrice(option.BaseCost,option.IsProducer,copies);
    }

    private void RefreshPanel()
    {
        BuildingOption option = buildingOptions[buildingDropdown.value];
        int cost = GetPurchaseCost(option);
        bool unlocked = IsUnlocked(option);
        bool canAfford = wallet.Banlance >= cost;
        bool canBuildNow = waveDirector.CanBuildNow && Time.timeScale > 0f;
        bool fits = buildGrid.CanPlace(selectedSlot, option.FootprintCells, out string placementReason);
        buildGrid.ShowPreview(selectedSlot, option.FootprintCells, fits);
        string status = !waveDirector.TutorialAllowsBuilding(option) ? "Hãy làm theo bước hướng dẫn hoặc chọn Bỏ qua."
            : !canBuildNow ? "Hiện không thể xây công trình."
            : !unlocked ? $"Chưa mở khóa. Mở từ đợt {option.UnlockWave}."
            : !fits ? placementReason
            : !canAfford ? "Không đủ tiền."
            : "Có thể xây.";
        infoText.text = $"{option.DisplayName}\nGiá: {cost} tiền\n"
            + $"Bạn có: {wallet.Banlance} tiền\n"
            + $"Chiếm: {option.FootprintCells} × {option.FootprintCells} ô nhỏ\n" + status;
        buildButton.interactable = canBuildNow && unlocked && canAfford && fits;
    }

    private void BuildSelectedSlot()
    {
        if(MoveInputBlocked) return;
        if (!initialized || !waveDirector.CanBuildNow || Time.timeScale <= 0f
            || selectedSlot == null || selectedSlot.IsOccupied)
            return;
        BuildingOption option = buildingOptions[buildingDropdown.value];
        if (!IsUnlocked(option) || !buildGrid.CanPlace(selectedSlot, option.FootprintCells, out _))
        {
            RefreshPanel();
            return;
        }
        int cost = GetPurchaseCost(option);
        if (wallet.Banlance < cost)
        {
            RefreshPanel();
            return;
        }
        Vector2Int anchor = selectedSlot.Cell;
        int size = option.FootprintCells;
        if (!buildGrid.TryReserve(selectedSlot, size, out int reservation, out Vector3 position, out _))
        {
            RefreshPanel();
            return;
        }
        if (!wallet.TrySpend(cost))
        {
            buildGrid.ReleaseReservation(reservation);
            RefreshPanel();
            return;
        }

        GameObject instance = null;
        ProducerBuilding newProducer = null;
        try
        {
            if (option.IsProducer)
            {
                newProducer = Instantiate(option.Producer, position, Quaternion.identity, buildingsParent);
                instance = newProducer.gameObject;
                newProducer.Configure(wallet, defendedHall, waveDirector);
            }
            else if (option.IsSupport)
            {
                SupportBuilding support = Instantiate(option.Support, position, Quaternion.identity, buildingsParent);
                instance = support.gameObject;
                support.Configure(wallet, defendedHall, waveDirector);
            }
            else
            {
                BoneLilyTower tower = Instantiate(option.Tower, position, Quaternion.identity, buildingsParent);
                instance = tower.gameObject;
                tower.Configure(defendedHall);
            }
            BuildingPlacement placement = instance.GetComponent<BuildingPlacement>();
            if (placement == null) placement = instance.AddComponent<BuildingPlacement>();
            placement.Initialize(buildGrid, reservation, anchor, size, option.SaveId, cost);
            if (newProducer != null) builtProducers.Add(newProducer);
            GameAudio.Play(GameSound.Placement);
            LastPlacedOptionIndex=buildingDropdown.value;
            LastPlacementTime=Time.unscaledTime;
            ClosePanel();
            try { BuildingPlaced?.Invoke(placement); }
            catch(Exception notificationError) { Debug.LogException(notificationError,this); }
        }
        catch (Exception exception)
        {
            if (instance != null) Destroy(instance);
            buildGrid.ReleaseReservation(reservation);
            wallet.AddReward(cost);
            Debug.LogException(exception, this);
            RefreshPanel();
        }
    }

    private void ClosePanel()
    {
        if (buildGrid != null) buildGrid.ClearPreview();
        buildingDropdown.Hide();
        selectedSlot = null;
        buildPanel.SetActive(false);
    }

    public void StopBuilding()
    {
        CancelBuildingMove();
        if (buildGrid != null) buildGrid.ClearPreview();
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