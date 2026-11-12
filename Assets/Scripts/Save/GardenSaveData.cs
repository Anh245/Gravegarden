using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

[Serializable]
public sealed class GardenSaveData
{
    public int schemaVersion;
    public string scenarioId;
    public string layoutSignature;
    public string waveSignature;
    public long savedUtcTicks;
    public int balance;
    public int hallHealth;
    public int hallMaxHealth;
    public int completedWaves;
    public bool expanded;
    public bool preparationRecorded;
    public double preparationSeconds;
    public HallSaveData[] halls;
    public BuildingSaveData[] buildings;
}

[Serializable]
public sealed class HallSaveData
{
    public string hallId;
    public int health;
    public int maxHealth;
}

[Serializable]
public sealed class BuildingSaveData
{
    public string buildingId;
    public int cellX;
    public int cellY;
    public int footprintCells;
    public int level;
    public double storedAmount;
    public bool investmentRecorded;
    public long investedAmount;
}

public sealed class BuildingSaveSpec
{
    public int FootprintCells;
    public int MaxLevel;
    public bool IsProducer;
    public int[] Capacities;
    public double[] ProductionRates;
    public string DisplayName;
}

public sealed class SaveValidationContext
{
    public string ScenarioId;
    public string LayoutSignature;
    public string WaveSignature;
    public int Columns;
    public int Rows;
    public int TotalWaves;
    public int HallMaxHealth;
    public bool SupportsExpansion;
    public bool SupportsEndless;
    public Dictionary<string, int> HallMaxHealthById;
    public List<(int x, int y)> AllowedCells;
    public Dictionary<string, BuildingSaveSpec> Catalog;
}

public static class CheckpointHash
{
    public static string FromText(string text)
    {
        using (SHA256 hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
    }
}
