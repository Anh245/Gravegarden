using System;
using System.Collections.Generic;

public sealed class OfflineEarning
{
    public string BuildingId;
    public string DisplayName;
    public double Amount;
}

public sealed class OfflineProductionReport
{
    public double ElapsedSeconds;
    public double TotalEarned;
    public bool WasCapped;
    public bool ClockWentBackwards;
    public OfflineEarning[] Earnings;
    public bool ShouldShowPanel => ElapsedSeconds >= 120d && TotalEarned > 0d;
}

public static class OfflineProduction
{
    public const double MaxOfflineSeconds = 8d * 60d * 60d;

    public static long CheckpointTimestamp(long nowUtcTicks, long lastProcessedUtcTicks)
        => Math.Max(nowUtcTicks, lastProcessedUtcTicks);

    public static bool TryCalculate(GardenSaveData source, SaveValidationContext context, long nowUtcTicks,
        out GardenSaveData updated, out OfflineProductionReport report, out string error)
    {
        updated = null;
        report = null;
        if (!CheckpointValidator.Validate(source, context, out error)) return false;
        if (nowUtcTicks <= 0 || nowUtcTicks > DateTime.MaxValue.Ticks)
        {
            error = "Thời gian hiện tại không hợp lệ.";
            return false;
        }

        double rawSeconds = Math.Max(0d, (nowUtcTicks - source.savedUtcTicks) / (double)TimeSpan.TicksPerSecond);
        double elapsed = Math.Min(rawSeconds, MaxOfflineSeconds);
        var earnings = new Dictionary<string, OfflineEarning>(StringComparer.Ordinal);
        var records = new BuildingSaveData[source.buildings.Length];
        double total = 0d;
        for (int i = 0; i < source.buildings.Length; i++)
        {
            BuildingSaveData original = source.buildings[i];
            var record = new BuildingSaveData { buildingId = original.buildingId,
                cellX = original.cellX, cellY = original.cellY, footprintCells = original.footprintCells,
                level = original.level, storedAmount = original.storedAmount,
                investmentRecorded = original.investmentRecorded, investedAmount = original.investedAmount };
            records[i] = record;
            BuildingSaveSpec spec = context.Catalog[record.buildingId];
            if (!spec.IsProducer) continue;
            if (spec.ProductionRates == null || spec.ProductionRates.Length < spec.MaxLevel)
            {
                error = "Thiếu dữ liệu tốc độ sản xuất: " + record.buildingId;
                return false;
            }
            double rate = spec.ProductionRates[record.level - 1];
            if (rate < 0d || double.IsNaN(rate) || double.IsInfinity(rate))
            {
                error = "Tốc độ sản xuất không hợp lệ: " + record.buildingId;
                return false;
            }
            double after = Math.Min(spec.Capacities[record.level - 1], record.storedAmount + rate * elapsed);
            double gained = after - record.storedAmount;
            record.storedAmount = after;
            total += gained;
            if (!earnings.TryGetValue(record.buildingId, out OfflineEarning earning))
            {
                earning = new OfflineEarning { BuildingId = record.buildingId,
                    DisplayName = string.IsNullOrWhiteSpace(spec.DisplayName) ? record.buildingId : spec.DisplayName };
                earnings.Add(record.buildingId, earning);
            }
            earning.Amount += gained;
        }
        updated = new GardenSaveData { schemaVersion = source.schemaVersion, scenarioId = source.scenarioId,
            layoutSignature = source.layoutSignature, waveSignature = source.waveSignature,
            savedUtcTicks = CheckpointTimestamp(nowUtcTicks, source.savedUtcTicks),
            balance = source.balance, hallHealth = source.hallHealth, hallMaxHealth = source.hallMaxHealth,
            completedWaves = source.completedWaves, expanded = source.expanded,
            preparationRecorded = source.preparationRecorded, preparationSeconds = source.preparationSeconds,
            halls = GardenCheckpointFormat.CloneHalls(source.halls), buildings = records };
        if (!CheckpointValidator.Validate(updated, context, out error)) { updated = null; return false; }
        report = new OfflineProductionReport { ElapsedSeconds = elapsed, TotalEarned = total,
            WasCapped = rawSeconds > MaxOfflineSeconds,
            ClockWentBackwards = nowUtcTicks < source.savedUtcTicks,
            Earnings = new List<OfflineEarning>(earnings.Values).ToArray() };
        return true;
    }
}
