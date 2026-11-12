using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
public class GardenSaveController : MonoBehaviour
{
    [Header("Hệ thống trong scene")]
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private WaveDirector waveDirector;
    [Header("Nhận diện kịch bản")]
    [SerializeField] private string scenarioId = "garden_v1";
    [Header("Giao diện")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Thông báo ngoại tuyến - tùy chọn, nếu dùng phải gán đủ 3 trường")]
    [SerializeField] private GameObject offlinePanel;
    [SerializeField] private TMP_Text offlineInfoText;
    [SerializeField] private Button offlineCloseButton;

    [SerializeField] private string mainMenuSceneName = "MainMenu";
    public bool DidRestoreCheckpoint { get; private set; }
    public bool CheckpointReady => initialized && isActiveAndEnabled;
    public bool IsOfflineDialogOpen => offlinePanel != null && offlinePanel.activeInHierarchy;
    public string LastMessage { get; private set; } = "";
    private long lastProcessedUtcTicks;
    private bool initialized;
    private bool busy;
    private int checkpointFrame = -10;
    public bool IsPreparationClockBlocked => busy || IsOfflineDialogOpen || Time.frameCount<=checkpointFrame+1;
    private bool hasFile;
    public string SavePath => Path.Combine(Application.persistentDataPath, GardenCheckpointFormat.FileName);
    private bool CanUse => initialized && !busy && isActiveAndEnabled && !SceneNavigator.IsTransitioning
        && buildManager.CheckpointReady && !buildManager.IsMovingBuilding && waveDirector.CanSaveCheckpoint
        && !defendedHall.IsDestroyed && Time.timeScale > 0f
        && (offlinePanel == null || !offlinePanel.activeInHierarchy);

    private void Start()
    {
        if (buildManager == null || wallet == null || defendedHall == null || waveDirector == null
            || saveButton == null || loadButton == null || statusText == null
            || !CheckpointValidator.IsValidId(scenarioId))
        {
            Debug.LogError("GardenSaveController: thiếu tham chiếu hoặc Scenario Id không hợp lệ.", this);
            enabled = false;
            return;
        }
        if (!buildManager.CheckpointReady || waveDirector.TotalWaveCount == 0)
        {
            Debug.LogError("GardenSaveController: BuildManager/WaveDirector chưa sẵn sàng. Kiểm tra Save Id và cấu hình.", this);
            enabled = false;
            return;
        }
        if (offlinePanel != null) offlinePanel.SetActive(false);
        bool anyOfflineUI = offlinePanel != null || offlineInfoText != null || offlineCloseButton != null;
        if (anyOfflineUI && (offlinePanel == null || offlineInfoText == null || offlineCloseButton == null))
        {
            Debug.LogError("GardenSaveController: gán đủ Offline Panel, Info Text và Close Button hoặc để trống cả ba.", this);
            enabled = false;
            return;
        }
        if (offlineCloseButton != null) offlineCloseButton.onClick.AddListener(CloseOfflinePanel);
        saveButton.onClick.AddListener(SaveCheckpoint);
        loadButton.onClick.AddListener(LoadCheckpoint);
        hasFile = File.Exists(SavePath);
        initialized = true;
        Show("Lưu phiên bản2: hỗ trợ ba nhà chính; chỉ lưu/tải giữa hai đợt.");
        UpdateButtons();
        GardenEntryMode entry = SceneNavigator.ConsumeEntry();
        if (entry == GardenEntryMode.ContinueCheckpoint && !TryLoadCheckpoint())
        {
            string reason = "Không thể tiếp tục: " + LastMessage;
            if (!SceneNavigator.TryLoad(mainMenuSceneName, GardenEntryMode.None, out string error, reason))
                Debug.LogError(reason + " " + error, this);
        }
    }

    private void Update() => UpdateButtons();
    private void UpdateButtons()
    {
        if (saveButton != null) saveButton.interactable = CanUse;
        if (loadButton != null) loadButton.interactable = CanUse && hasFile;
    }

    private bool NoLivingEnemies()
    {
        foreach (EnemyHealth enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>())
            if (enemy.gameObject.scene == gameObject.scene && enemy.IsAlive) return false;
        return true;
    }

    public void SaveCheckpoint() => TrySaveCheckpoint();

    public bool TrySaveCheckpoint()
    {
        if (!CanUse || !NoLivingEnemies()) { Show("Chỉ lưu được giữa hai đợt, khi không còn quái sống."); return false; }
        busy = true;
        checkpointFrame = Time.frameCount;
        try
        {
            if (!BuildingCheckpointAdapter.TryCapture(buildManager, out BuildingSaveData[] buildings, out string error))
            { Show(error); return false; }
            SaveValidationContext context = BuildingCheckpointAdapter.CreateContext(buildManager, waveDirector, defendedHall, scenarioId);
            var data = new GardenSaveData { schemaVersion = GardenCheckpointFormat.Version, scenarioId = scenarioId,
                layoutSignature = context.LayoutSignature, waveSignature = context.WaveSignature,
                savedUtcTicks = OfflineProduction.CheckpointTimestamp(DateTime.UtcNow.Ticks, lastProcessedUtcTicks), balance = wallet.Banlance,
                hallHealth = defendedHall.CurrentHealth, hallMaxHealth = defendedHall.MaxHealth,
                completedWaves = waveDirector.CompletedWaveCount, expanded = waveDirector.HasExpansionUnlocked,
                preparationRecorded = true, preparationSeconds = waveDirector.CheckpointPreparationSeconds,
                halls = waveDirector.ExpansionController != null ? waveDirector.ExpansionController.CaptureCheckpointHalls()
                    : new[] { new HallSaveData { hallId=GardenCheckpointFormat.Heart, health=defendedHall.CurrentHealth, maxHealth=defendedHall.MaxHealth } },
                buildings = buildings };
            if (!CheckpointValidator.Validate(data, context, out error)) { Show(error); return false; }
            string json = JsonUtility.ToJson(data, true);
            if (!CheckpointFileStore.TryWrite(SavePath, json, out error)) { Show(error); return false; }
            lastProcessedUtcTicks = data.savedUtcTicks;
            hasFile = true;
            Show($"Đã lưu: {buildings.Length} công trình, hoàn thành {data.completedWaves} đợt.");
            Debug.Log("Bản lưu: " + SavePath, this);
            return true;
        }
        catch (Exception exception) { Show("Lưu thất bại: " + exception.Message); return false; }
        finally { busy = false; UpdateButtons(); }
    }

    public void LoadCheckpoint() => TryLoadCheckpoint();

    public bool TryLoadCheckpoint()
    {
        if (!CanUse || !NoLivingEnemies()) { Show("Chỉ tải được giữa hai đợt, khi không còn quái sống."); return false; }
        busy = true;
        checkpointFrame = Time.frameCount;
        float previousTimeScale = Time.timeScale;
        try
        {
            if (!CheckpointFileStore.TryRead(SavePath, out string json, out string error)) { Show(error); return false; }
            GardenSaveData data;
            try { data = JsonUtility.FromJson<GardenSaveData>(json); }
            catch (Exception) { Show("File lưu không phải JSON hợp lệ. Giữ nguyên trận hiện tại."); return false; }
            SaveValidationContext context = BuildingCheckpointAdapter.CreateContext(buildManager, waveDirector, defendedHall, scenarioId, data != null && data.expanded);
            if (!CheckpointValidator.Validate(data, context, out error)) { Show(error); return false; }
            if (!OfflineProduction.TryCalculate(data, context, DateTime.UtcNow.Ticks,
                out GardenSaveData credited, out OfflineProductionReport report, out error))
            { Show(error); return false; }
            data = credited;
            Time.timeScale = 0f;
            foreach (BuildModeController mode in UnityEngine.Object.FindObjectsByType<BuildModeController>())
                if (mode.gameObject.scene == gameObject.scene) mode.ExitBuildMode();
            buildManager.CloseForCheckpoint();
            foreach (TowerUpgradeUI ui in UnityEngine.Object.FindObjectsByType<TowerUpgradeUI>()) ui.ClosePanel();
            foreach (ProducerUpgradeUI ui in UnityEngine.Object.FindObjectsByType<ProducerUpgradeUI>()) ui.ClosePanel();
            foreach (SupportUpgradeUI ui in UnityEngine.Object.FindObjectsByType<SupportUpgradeUI>()) ui.ClosePanel();
            string persistError = "";
            DistrictExpansionController expansion = waveDirector.ExpansionController;
            bool previousExpanded = expansion != null && expansion.IsExpanded;
            HallSaveData[] previousHalls = expansion != null ? expansion.CaptureCheckpointHalls() : null;
            bool PersistCredit()
            {
                string creditedJson = JsonUtility.ToJson(data, true);
                if (expansion != null && !expansion.TryApplyCheckpointState(data.expanded, data.halls, out persistError)) return false;
                if (!CheckpointFileStore.TryWrite(SavePath, creditedJson, out persistError))
                {
                    if (expansion != null && !expansion.TryApplyCheckpointState(previousExpanded, previousHalls, out string rollbackError))
                        Debug.LogError("Không khôi phục được trạng thái khu sau lỗi ghi file: " + rollbackError, this);
                    return false;
                }
                lastProcessedUtcTicks = data.savedUtcTicks;
                return true;
            }
            if (!BuildingCheckpointAdapter.TryRestore(buildManager, data, out error, PersistCredit))
            { Show(string.IsNullOrEmpty(persistError) ? error : persistError); return false; }
            wallet.RestoreCheckpointBalance(data.balance);
            defendedHall.RestoreCheckpointHealth(data.hallHealth);
            waveDirector.RestoreCheckpointProgress(data.completedWaves, data.preparationRecorded, data.preparationSeconds);
            DidRestoreCheckpoint = true;
            foreach (BoneProjectile projectile in UnityEngine.Object.FindObjectsByType<BoneProjectile>())
                if (projectile.gameObject.scene == gameObject.scene)
                { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
            Show($"Đã tải: {data.buildings.Length} công trình. Sẵn sàng đợt {data.completedWaves + 1}.\n"
                + $"Ngoại tuyến: {report.ElapsedSeconds:0.#} giây, +{report.TotalEarned:0.##} xu vào kho."
                + (report.ClockWentBackwards ? " Đồng hồ đi lùi: không cộng thời gian âm." : ""));
            ShowOfflineReport(report);
            return true;
        }
        catch (Exception exception) { Show("Tải thất bại: " + exception.Message); return false; }
        finally { Time.timeScale = previousTimeScale; busy = false; UpdateButtons(); }
    }

    private void ShowOfflineReport(OfflineProductionReport report)
    {
        if (!report.ShouldShowPanel || offlinePanel == null || offlineInfoText == null || offlineCloseButton == null)
            return;
        TimeSpan span = TimeSpan.FromSeconds(report.ElapsedSeconds);
        string duration = span.TotalHours >= 1d
            ? $"{(int)span.TotalHours} giờ {span.Minutes} phút"
            : $"{span.Minutes} phút {span.Seconds} giây";
        var text = new System.Text.StringBuilder();
        text.AppendLine("Sản xuất ngoại tuyến");
        text.AppendLine("Thời gian tính: " + duration + (report.WasCapped ? " (giới hạn 8 giờ)" : ""));
        foreach (OfflineEarning earning in report.Earnings)
            if (earning.Amount > 0d) text.AppendLine($"{earning.DisplayName}: +{earning.Amount:0.##} xu");
        text.AppendLine($"Tổng đưa vào kho: {report.TotalEarned:0.##} xu");
        text.Append("Bấm công trình sản xuất để thu hoạch vào ví.");
        offlineInfoText.text = text.ToString();
        offlinePanel.SetActive(true);
        offlinePanel.transform.SetAsLastSibling();
    }

    public void CloseOfflinePanel()
    {
        if (offlinePanel != null) offlinePanel.SetActive(false);
    }

    private void Show(string message)
    {
        LastMessage = message;
        if (statusText != null) statusText.text = message;
        Debug.Log("Lưu/Tải: " + message, this);
    }

    [ContextMenu("In đường dẫn lưu")]
    private void PrintSavePath() => Debug.Log(SavePath, this);

    private void OnDestroy()
    {
        if (offlineCloseButton != null) offlineCloseButton.onClick.RemoveListener(CloseOfflinePanel);
        if (saveButton != null) saveButton.onClick.RemoveListener(SaveCheckpoint);
        if (loadButton != null) loadButton.onClick.RemoveListener(LoadCheckpoint);
    }
}
