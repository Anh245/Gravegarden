using System;
using System.Collections.Generic;
using UnityEngine;

public static class BuildingCheckpointAdapter
{
    private static BuildingOption FindOption(BuildManager manager, string id)
    {
        foreach (BuildingOption option in manager.CheckpointOptions)
            if (option.SaveId == id) return option;
        return null;
    }

    public static SaveValidationContext CreateContext(BuildManager manager, WaveDirector waves,
        TownHallHealth hall, string scenarioId, bool? targetExpanded = null)
    {
        BuildGrid grid = manager.CheckpointGrid;
        var catalog = new Dictionary<string, BuildingSaveSpec>(StringComparer.Ordinal);
        foreach (BuildingOption option in manager.CheckpointOptions)
        {
            if (!option.IsValid || !CheckpointValidator.IsValidId(option.SaveId) || catalog.ContainsKey(option.SaveId))
                throw new InvalidOperationException("Save Id bị trống, trùng hoặc không hợp lệ trong Building Options.");
            var spec = new BuildingSaveSpec { FootprintCells = option.FootprintCells,
                MaxLevel = option.IsProducer ? option.Producer.Definition.MaxLevel
                    : option.IsSupport ? option.Support.Definition.MaxLevel : option.Tower.Definition.MaxLevel,
                IsProducer = option.IsProducer, DisplayName = option.DisplayName };
            if (option.IsProducer)
            {
                spec.Capacities = new int[spec.MaxLevel];
                spec.ProductionRates = new double[spec.MaxLevel];
                for (int i = 0; i < spec.MaxLevel; i++)
                {
                    spec.Capacities[i] = option.Producer.Definition.GetCapacityAtLevel(i + 1);
                    spec.ProductionRates[i] = option.Producer.Definition.GetProductionAtLevel(i + 1);
                    if (spec.Capacities[i] <= 0) throw new InvalidOperationException("Sức chứa producer không hợp lệ.");
                }
            }
            catalog.Add(option.SaveId, spec);
        }
        return new SaveValidationContext { ScenarioId = scenarioId,
            LayoutSignature = grid.CheckpointLayoutSignature(), WaveSignature = waves.CheckpointSignature,
            Columns = grid.Columns, Rows = grid.Rows, TotalWaves = waves.TotalWaveCount,
            HallMaxHealth = hall.MaxHealth,
            SupportsExpansion = waves.ExpansionController != null, SupportsEndless = waves.HasEndlessConfigured,
            HallMaxHealthById = waves.ExpansionController != null ? waves.ExpansionController.CheckpointHallLimits()
                : new Dictionary<string,int> { { GardenCheckpointFormat.Heart, hall.MaxHealth } },
            AllowedCells = grid.CheckpointAllowedCells(targetExpanded ?? waves.HasExpansionUnlocked), Catalog = catalog };
    }

    private static HashSet<GameObject> ActiveActors(BuildManager manager)
    {
        var roots = new HashSet<GameObject>();
        foreach (BoneLilyTower actor in UnityEngine.Object.FindObjectsByType<BoneLilyTower>())
            if (actor.gameObject.scene == manager.gameObject.scene) roots.Add(actor.gameObject);
        foreach (ProducerBuilding actor in UnityEngine.Object.FindObjectsByType<ProducerBuilding>())
            if (actor.gameObject.scene == manager.gameObject.scene) roots.Add(actor.gameObject);
        foreach (SupportBuilding actor in UnityEngine.Object.FindObjectsByType<SupportBuilding>())
            if (actor.gameObject.scene == manager.gameObject.scene) roots.Add(actor.gameObject);
        return roots;
    }

    public static bool TryCapture(BuildManager manager, out BuildingSaveData[] data, out string error)
    {
        data = null;
        error = "";
        var records = new List<BuildingSaveData>();
        foreach (GameObject root in ActiveActors(manager))
        {
            BuildingPlacement placement = root.GetComponent<BuildingPlacement>();
            if (!root.transform.IsChildOf(manager.CheckpointBuildingsParent) || placement == null
                || !placement.IsPlaced || placement.Grid != manager.CheckpointGrid || string.IsNullOrEmpty(placement.BuildingId))
            {
                error = "Có công trình đặt tay/chưa đăng ký ô: " + root.name + ". Hãy xây công trình qua BuildManager.";
                return false;
            }
            BuildingOption option = FindOption(manager, placement.BuildingId);
            if (option == null || placement.SizeInCells != option.FootprintCells
                || (root.transform.position - manager.CheckpointGrid.GetPlacementCenter(placement.Anchor, placement.SizeInCells)).sqrMagnitude > 0.0001f)
            {
                error = "Công trình sai mã, footprint hoặc bị di chuyển thủ công: " + root.name;
                return false;
            }
            BoneLilyTower tower = root.GetComponent<BoneLilyTower>();
            ProducerBuilding producer = root.GetComponent<ProducerBuilding>();
            SupportBuilding support = root.GetComponent<SupportBuilding>();
            if ((tower != null ? 1 : 0) + (producer != null ? 1 : 0) + (support != null ? 1 : 0) != 1)
            {
                error = "Công trình có nhiều loại component trên cùng root: " + root.name;
                return false;
            }
            int level;
            double stored = 0d;
            if (option.IsProducer && producer != null && producer.Definition == option.Producer.Definition && producer.CheckpointReady)
            {
                level = producer.Level;
                stored = producer.StoredAmount;
            }
            else if (option.IsSupport && support != null && support.Definition == option.Support.Definition && support.CheckpointReady)
                level = support.Level;
            else if (!option.IsProducer && !option.IsSupport && tower != null
                && tower.Definition == option.Tower.Definition && tower.CheckpointReady)
                level = tower.Level;
            else
            {
                error = "Công trình chưa khởi tạo xong hoặc sai dữ liệu: " + root.name;
                return false;
            }
            records.Add(new BuildingSaveData { buildingId = option.SaveId,
                cellX = placement.Anchor.x, cellY = placement.Anchor.y,
                footprintCells = placement.SizeInCells, level = level, storedAmount = stored,
                investmentRecorded = placement.InvestmentRecorded, investedAmount = placement.InvestedAmount });
        }
        records.Sort((a, b) => a.cellY != b.cellY ? a.cellY.CompareTo(b.cellY) : a.cellX.CompareTo(b.cellX));
        data = records.ToArray();
        return true;
    }

    public static bool TryRestore(BuildManager manager, GardenSaveData data, out string error, Func<bool> beforeCommit = null)
    {
        error = "";
        // Kiểm tra thế giới hiện tại trước khi thay thế; không xóa các object đặt tay.
        if (!TryCapture(manager, out _, out error)) return false;
        BuildGrid grid = manager.CheckpointGrid;
        PlacementMap replacement = grid.CheckpointEmptyMap(data.expanded);
        var oldPlacements = new List<BuildingPlacement>();
        foreach (GameObject root in ActiveActors(manager)) oldPlacements.Add(root.GetComponent<BuildingPlacement>());
        var staged = new List<BuildingPlacement>();
        var ids = new List<int>();
        var producers = new List<ProducerBuilding>();
        GameObject staging = new GameObject("Checkpoint_Staging");
        staging.SetActive(false);
        staging.transform.SetParent(manager.CheckpointBuildingsParent, false);
        try
        {
            foreach (BuildingSaveData record in data.buildings)
            {
                BuildingOption option = FindOption(manager, record.buildingId);
                if (option == null || !replacement.TryReserve(record.cellX, record.cellY, record.footprintCells, out int id, out _))
                    throw new InvalidOperationException("Không khôi phục được footprint: " + record.buildingId);
                Vector2Int anchor = new Vector2Int(record.cellX, record.cellY);
                Vector3 position = grid.GetPlacementCenter(anchor, record.footprintCells);
                GameObject root;
                if (option.IsProducer)
                {
                    ProducerBuilding actor = UnityEngine.Object.Instantiate(option.Producer, position, Quaternion.identity, staging.transform);
                    root = actor.gameObject;
                    actor.Configure(manager.CheckpointWallet, manager.CheckpointHall, manager.CheckpointWaves);
                    if (!actor.RestoreProgress(record.level, record.storedAmount))
                        throw new InvalidOperationException("Prefab producer thiếu tham chiếu hoặc tiến trình không hợp lệ.");
                    producers.Add(actor);
                }
                else if (option.IsSupport)
                {
                    SupportBuilding actor = UnityEngine.Object.Instantiate(option.Support, position, Quaternion.identity, staging.transform);
                    root = actor.gameObject;
                    actor.Configure(manager.CheckpointWallet, manager.CheckpointHall, manager.CheckpointWaves);
                    if (!actor.RestoreLevel(record.level)) throw new InvalidOperationException("Dữ liệu support không hợp lệ.");
                }
                else
                {
                    BoneLilyTower actor = UnityEngine.Object.Instantiate(option.Tower, position, Quaternion.identity, staging.transform);
                    root = actor.gameObject;
                    actor.Configure(manager.CheckpointHall);
                    if (!actor.RestoreLevel(record.level)) throw new InvalidOperationException("Prefab tháp thiếu tham chiếu.");
                }
                BuildingPlacement placement = root.GetComponent<BuildingPlacement>();
                if (placement == null) placement = root.AddComponent<BuildingPlacement>();
                // Chưa gán reservation: hủy staging khi lỗi sẽ không trả nhầm ô của thế giới cũ.
                staged.Add(placement);
                ids.Add(id);
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Object.Destroy(staging);
            error = "Không thể tạo công trình từ bản lưu: " + exception.Message;
            return false;
        }

        // Ghi nhận thu nhập ngoại tuyến trước khi thay thế thế giới.
        // Nếu ghi file thất bại, hủy các object tạm; không áp dụng tiền vào trận hiện tại.
        if (beforeCommit != null)
        {
            try
            {
                if (!beforeCommit())
                {
                    UnityEngine.Object.Destroy(staging);
                    error = "Không ghi nhận được checkpoint sau khi tính thu nhập ngoại tuyến.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Object.Destroy(staging);
                error = "Không thể ghi checkpoint: " + exception.Message;
                return false;
            }
        }

        // Chỉ đến đây mới thay thế thế giới, sau khi toàn bộ prefab đã được tạo và kiểm tra.
        foreach (BuildingPlacement old in oldPlacements)
        {
            old.gameObject.SetActive(false);
            old.DetachCheckpointReservation();
        }
        grid.AdoptCheckpointMap(replacement);
        for (int i = 0; i < staged.Count; i++)
        {
            BuildingSaveData record = data.buildings[i];
            staged[i].Initialize(grid, ids[i], new Vector2Int(record.cellX, record.cellY), record.footprintCells, record.buildingId);
            staged[i].RestoreInvestment(record.investmentRecorded,record.investedAmount);
            staged[i].transform.SetParent(manager.CheckpointBuildingsParent, true);
        }
        manager.ReplaceCheckpointProducers(producers);
        foreach (BuildingPlacement old in oldPlacements) UnityEngine.Object.Destroy(old.gameObject);
        UnityEngine.Object.Destroy(staging);
        return true;
    }
}
