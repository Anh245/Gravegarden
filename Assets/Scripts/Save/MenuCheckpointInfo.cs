using System;

public static class MenuCheckpointInfo
{
    // Menu chưa có lưới và catalog của Garden; kiểm tra tương thích đầy đủ khi vào Garden.
    public static bool IsReadable(GardenSaveData data, string scenarioId, out string message)
    {
        message = "";
        if (!GardenCheckpointFormat.ValidateHeader(data, out message)) return false;
        if (data.scenarioId != scenarioId || data.buildings == null || data.buildings.Length > 65536)
        { message="Bản lưu không thuộc kịch bản hoặc danh sách công trình không hợp lệ."; return false; }
        foreach (BuildingSaveData record in data.buildings)
            if (record == null || !CheckpointValidator.IsValidId(record.buildingId)
                || !BuildingSaleMath.ValidInvestment(record.investmentRecorded,record.investedAmount)
                || record.level < 1 || record.cellX < 0 || record.cellY < 0 || record.cellX >= 256 || record.cellY >= 256
                || (record.footprintCells < 1 || record.footprintCells > 256)
                || record.storedAmount < 0d || double.IsNaN(record.storedAmount) || double.IsInfinity(record.storedAmount))
            {
                message = "Bản lưu chứa dữ liệu công trình không hợp lệ.";
                return false;
            }
        message = $"Bản lưu: đã hoàn thành {data.completedWaves} đợt, {data.buildings.Length} công trình.";
        return true;
    }
}
