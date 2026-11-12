using System;

public static class CheckpointValidator
{
    public static bool IsValidId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        foreach (char c in id)
            if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_') return false;
        return true;
    }

    private static bool ValidProgress(GardenSaveData data, SaveValidationContext context)
    {
        if(context.SupportsExpansion)
        {
            if(context.TotalWaves!=5 || data.halls.Length!=3 || data.expanded!=(data.completedWaves>=5))return false;
            if(!data.expanded)return data.completedWaves<5;
            return context.SupportsEndless || data.completedWaves==5;
        }
        return !data.expanded && data.halls.Length==1 && data.completedWaves<context.TotalWaves;
    }

    public static bool Validate(GardenSaveData data, SaveValidationContext context, out string error)
    {
        error = "";
        if (!GardenCheckpointFormat.ValidateHeader(data, out error)) return false;
        if (context == null)
            error = "Thiếu cấu hình xác thực bản lưu.";
        else if (string.IsNullOrWhiteSpace(data.scenarioId) || data.scenarioId != context.ScenarioId)
            error = "Bản lưu thuộc kịch bản khác.";
        else if (data.layoutSignature != context.LayoutSignature || string.IsNullOrEmpty(data.layoutSignature))
            error = "Lưới/vùng được phép xây đã thay đổi so với bản lưu.";
        else if (data.waveSignature != context.WaveSignature || string.IsNullOrEmpty(data.waveSignature))
            error = "Dữ liệu wave đã thay đổi so với bản lưu.";
        else if (data.savedUtcTicks <= 0 || data.savedUtcTicks > DateTime.MaxValue.Ticks)
            error = "Thời điểm lưu không hợp lệ.";
        else if (data.balance < 0 || data.hallMaxHealth != context.HallMaxHealth
            || data.hallHealth <= 0 || data.hallHealth > context.HallMaxHealth)
            error = "Tiền hoặc máu nhà chính không hợp lệ.";
        else if (!ValidProgress(data, context))
            error = "Tiến trình wave hoặc trạng thái mở khu không tương thích.";
        else if (data.buildings == null || data.buildings.Length > context.Columns * context.Rows)
            error = "Danh sách công trình không hợp lệ.";
        if (error.Length != 0) return false;

        if(context.HallMaxHealthById==null || context.HallMaxHealthById.Count!=data.halls.Length)
        {error="Danh sách nhà chính đã thay đổi.";return false;}
        foreach(var hall in data.halls)
            if(!context.HallMaxHealthById.TryGetValue(hall.hallId,out int maximum) || maximum!=hall.maxHealth)
            {error="Max Health của nhà chính đã thay đổi: "+hall.hallId;return false;}
        var map = new PlacementMap(context.Columns, context.Rows, context.AllowedCells);
        foreach (BuildingSaveData building in data.buildings)
        {
            if (building == null || !IsValidId(building.buildingId)
                || !context.Catalog.TryGetValue(building.buildingId, out BuildingSaveSpec spec))
            {
                error = "Bản lưu chứa mã công trình không có trong Building Options.";
                return false;
            }
            if (!BuildingSaleMath.ValidInvestment(building.investmentRecorded,building.investedAmount))
            {
                error = "Tiền đầu tư không hợp lệ: " + building.buildingId;
                return false;
            }
            if (building.footprintCells != spec.FootprintCells || building.level < 1 || building.level > spec.MaxLevel)
            {
                error = "Footprint hoặc cấp công trình không hợp lệ: " + building.buildingId;
                return false;
            }
            if (double.IsNaN(building.storedAmount) || double.IsInfinity(building.storedAmount)
                || building.storedAmount < 0d
                || (!spec.IsProducer && building.storedAmount != 0d)
                || (spec.IsProducer && building.storedAmount > spec.Capacities[building.level - 1]))
            {
                error = "Tiền trong kho không hợp lệ: " + building.buildingId;
                return false;
            }
            if (!map.TryReserve(building.cellX, building.cellY, building.footprintCells, out _, out _))
            {
                error = "Công trình chồng ô, chạm vùng cấm hoặc vượt lưới: " + building.buildingId;
                return false;
            }
        }
        return true;
    }
}
