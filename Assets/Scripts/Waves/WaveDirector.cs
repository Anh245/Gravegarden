using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveDirector : MonoBehaviour
{
    private enum WaveState { Ready, Running, ExpansionReady, Finished, Defeat, Error }

    [Header("Dữ liệu nhiều loại quái")]
    [SerializeField] private MixedWaveSetDefinition mixedWaveSet;
    [SerializeField] private GodotEndlessWaves endlessWaves;
    private MixedWaveSetDefinition.SpawnInstruction[] currentPlan;
    private int currentReward;
    private float currentHealthMultiplier = 1f;
    private bool EndlessAvailable => endlessWaves != null && HasExpansionUnlocked;
    [Header("Đếm ngược chuẩn bị")]
    [SerializeField] private TMP_Text preparationText;
    [SerializeField] private GardenSaveController preparationSaveController;
    private readonly PreparationClock preparation = new PreparationClock();
    public double CheckpointPreparationSeconds => preparation.Remaining;
    private bool PreparationBlocked => preparationSaveController != null && preparationSaveController.IsPreparationClockBlocked;
    private bool CanStartPreparedWave => initialized && isActiveAndEnabled
        && (state==WaveState.Ready || (state==WaveState.ExpansionReady && EndlessAvailable))
        && Time.timeScale>0 && !HasDefeatedHall && !TutorialBlocksWave
        && !PreparationBlocked && !SceneNavigator.IsTransitioning;
    [Header("Hướng dẫn tương tác - tùy chọn")]
    [SerializeField] private GardenTutorialController tutorial;
    public bool TutorialAllowsBuilding(BuildingOption option) => tutorial==null || tutorial.Allows(option);
    private bool TutorialBlocksWave => tutorial!=null && tutorial.BlocksWaveStart;
    public event System.Action<ProducerBuilding,int> ProducerHarvested;
    public void NotifyProducerHarvested(ProducerBuilding producer,int amount)
    {
        try { ProducerHarvested?.Invoke(producer,amount); }
        catch(System.Exception error) { Debug.LogException(error,this); }
    }

    [Header("Hệ thống trận đấu")]
    [SerializeField] private EnemyRoute route;
    [Tooltip("Các tuyến thêm, sinh luân phiên cùng Route chính. Chương1 đặt đúng một tuyến phụ.")]
    [SerializeField] private EnemyRoute[] additionalRoutes = new EnemyRoute[0];
    [SerializeField] private Transform enemiesParent;
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private PlayerWallet wallet;
    [Header("Nhiều nhà chính - tùy chọn")]
    [SerializeField] private DistrictDefenseRoutes districtRoutes;
    private DistrictDefenseRoutes.Binding[] runningRoutes;
    [SerializeField] private DistrictExpansionController expansion;
    public bool IsBetweenWaves => initialized && (state == WaveState.Ready || state == WaveState.ExpansionReady);
    public bool HasExpansionUnlocked => expansion != null && expansion.IsExpanded;
    public DistrictExpansionController ExpansionController => expansion;
    public bool HasEndlessConfigured => endlessWaves != null;
    public bool SupportsSingleHallCheckpoint => !HasExpansionUnlocked && (districtRoutes == null
        || (districtRoutes.isActiveAndEnabled && !districtRoutes.HasAdditionalActiveHall(defendedHall)));
    public bool HasDefeatedHall => defendedHall == null || defendedHall.IsDestroyed
        || (districtRoutes != null && districtRoutes.HasDestroyedActiveHall);

    [Header("Giao diện")]
    [SerializeField] private Button startWaveButton;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text remainingEnemiesText;
    [SerializeField] private TMP_Text waveStatusText;
    [Header("Khả năng chịu hiệu ứng")]
    [Tooltip(
    "Hệ số nhận làm chậm: " +
    "1 = nhận toàn bộ; " +
    "0.7 = nhận 70%; " +
    "0 = miễn nhiễm.")]
    [SerializeField, Range(0f, 1f)]
    private float slowEffectMultiplier = 1f;

    public float SlowEffectMultiplier => slowEffectMultiplier;

    private readonly List<EnemyHealth> spawnedEnemies = new();
    private MixedWaveSetDefinition.SpawnInstruction[][] plans;
    private WaveState state = WaveState.Ready;
    private int waveIndex = -1;
    private int nextSpawnIndex;
    private float spawnTimer;
    private bool initialized;
    private string errorMessage;
    private string checkpointSignature;
    public string CheckpointSignature => checkpointSignature;
    public int TotalWaveCount => plans != null ? plans.Length : 0;
    public bool CanSaveCheckpoint => initialized && isActiveAndEnabled && IsBetweenWaves && !HasDefeatedHall
        && (expansion == null || expansion.CheckpointStateConsistent);

    public void RestoreCheckpointProgress(int completed, bool preparationRecorded = false, double preparationSeconds = 0)
    {
        bool valid = completed >= 0 && (HasExpansionUnlocked
            ? (endlessWaves != null ? completed >= 5 && completed < GodotEndlessPolicy.MaxSupportedWave : completed == 5)
            : completed < plans.Length);
        if (!CanSaveCheckpoint || !valid)
            throw new System.InvalidOperationException("Chỉ khôi phục tiến trình hợp lệ ở trạng thái chuẩn bị.");
        foreach (EnemyHealth enemy in spawnedEnemies)
            if (enemy != null)
            {
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
        spawnedEnemies.Clear();
        CompletedWaveCount = completed;
        waveIndex = completed - 1;
        currentPlan = waveIndex >= 0 && waveIndex < plans.Length ? plans[waveIndex] : null;
        currentReward = waveIndex >= 0 ? mixedWaveSet.CompletionRewardAt(waveIndex) : 0;
        currentHealthMultiplier = 1f;
        nextSpawnIndex = currentPlan != null ? currentPlan.Length : 0;
        spawnTimer = 0f;
        state = HasExpansionUnlocked && endlessWaves == null ? WaveState.ExpansionReady : WaveState.Ready;
        preparation.Reset(preparationRecorded ? preparationSeconds : PreparationClock.Duration(completed,HasExpansionUnlocked));
        RefreshUI();
    }

    private string BuildCheckpointSignature()
    {
        var text = new System.Text.StringBuilder();
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        text.Append(plans.Length);
        int rewardIndex=0;
        foreach (var wave in plans)
        {
            text.Append("|wave|");
            foreach (var spawn in wave)
                text.Append(spawn.Prefab.name).Append(':')
                    .Append(spawn.Prefab.GetComponent<EnemyHealth>().Definition.name).Append(':')
                    .Append(spawn.DelayBeforeSpawn.ToString("R", culture)).Append(';');
            int reward=mixedWaveSet.CompletionRewardAt(rewardIndex++);
            if(reward>0)text.Append("|reward|").Append(reward);
        }
        if(districtRoutes == null && additionalRoutes!=null && additionalRoutes.Length>0)
        {
            text.Append("|routes|");
            for(int r=0;r<=additionalRoutes.Length;r++)
            {
                EnemyRoute item=r==0?route:additionalRoutes[r-1];
                text.Append(item.PointCount).Append(':');
                for(int i=0;i<item.PointCount;i++)
                {
                    Vector3 point=item.GetPoint(i);
                    text.Append(point.x.ToString("R",culture)).Append(',').Append(point.y.ToString("R",culture))
                        .Append(',').Append(point.z.ToString("R",culture)).Append(';');
                }
            }
        }
        if (districtRoutes != null)
        {
            text.Append("|district-routing-v1|");
            foreach (var binding in districtRoutes.ConfiguredBindings)
            {
                text.Append(binding.route.name).Append(':').Append(binding.hall.name)
                    .Append(':').Append(binding.hall.MaxHealth).Append(':');
                for (int i = 0; i < binding.route.PointCount; i++)
                {
                    Vector3 point = binding.route.GetPoint(i);
                    text.Append(point.x.ToString("R", culture)).Append(',')
                        .Append(point.y.ToString("R", culture)).Append(',')
                        .Append(point.z.ToString("R", culture)).Append(';');
                }
                text.Append('|');
            }
        }
        if (expansion != null) text.Append("|unlock-after-5-reward-2000-v1|");
        if (endlessWaves != null) text.Append("|godot-endless-v2|").Append(endlessWaves.CheckpointSignature());
        return CheckpointHash.FromText(text.ToString());
    }

    public int CompletedWaveCount { get; private set; }
    public int AvailableWaveNumber => CompletedWaveCount + 1;
    public bool HasCompletedAllWaves => state == WaveState.Finished;
    public bool IsWaveRunning => initialized && state == WaveState.Running;
    public bool CanBuildNow => initialized
        && isActiveAndEnabled
        && !HasDefeatedHall
        && (state == WaveState.Ready || state == WaveState.Running || state == WaveState.ExpansionReady);

    private void Start()
    {
        if (mixedWaveSet == null || (districtRoutes == null && route == null) || enemiesParent == null
            || !enemiesParent.gameObject.activeInHierarchy
            || defendedHall == null || wallet == null
            || startWaveButton == null || waveText == null
            || remainingEnemiesText == null || waveStatusText == null)
        {
            Fail("Thiếu tham chiếu hoặc Enemies Parent đang tắt. Kiểm tra Inspector.");
            return;
        }

        if (!TryCaptureRoutes(out runningRoutes, out string routeError))
        { Fail(routeError); return; }
        if (!mixedWaveSet.TryCreatePlans(out plans, out string error))
        {
            Fail(error);
            return;
        }

        if (expansion != null)
        {
            if (!expansion.TryValidate(out string expansionError)) { Fail(expansionError); return; }
            if (plans.Length != 5) { Fail("Mở rộng hiện yêu cầu đúng 5 đợt chương 1."); return; }
        }
        if (endlessWaves != null)
        {
            if (expansion == null) { Fail("Wave vô tận cần DistrictExpansionController."); return; }
            if (!endlessWaves.TryValidate(out string endlessError)) { Fail(endlessError); return; }
        }
        checkpointSignature = BuildCheckpointSignature();
        CompletedWaveCount = 0;
        startWaveButton.onClick.AddListener(StartNextWave);
        initialized = true;
        preparation.Reset(PreparationClock.Duration(0,false));
        RefreshUI();
    }

    public void StartNextWave()
    {
        if (!CanStartPreparedWave) return;

        int nextWave = waveIndex + 1;
        MixedWaveSetDefinition.SpawnInstruction[] nextPlan;
        int reward;
        float multiplier;
        if (nextWave < plans.Length)
        { nextPlan = plans[nextWave]; reward = mixedWaveSet.CompletionRewardAt(nextWave); multiplier = 1f; }
        else
        {
            if (!EndlessAvailable) return;
            if (!endlessWaves.TryCreatePlan(nextWave + 1, out nextPlan, out multiplier, out reward, out string planError))
            { Fail(planError); return; }
        }

        if (!TryCaptureRoutes(out runningRoutes, out string routeError))
        { Fail(routeError); return; }

        int earlyReward = preparation.EarlyReward;
        if(earlyReward>0 && !wallet.TryAddRewardExact(earlyReward))
        { Fail("Ví không thể nhận đủ thưởng bắt đầu sớm."); return; }
        preparation.Consume();
        if(earlyReward>0)Debug.Log("Thưởng bắt đầu sớm: +"+earlyReward+" xu.",this);

        // Đợt trước đã kết thúc; dọn các đối tượng chết đang bị tắt.
        foreach (EnemyHealth enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
        spawnedEnemies.Clear();

        waveIndex = nextWave;
        currentPlan = nextPlan;
        currentReward = reward;
        currentHealthMultiplier = multiplier;
        nextSpawnIndex = 0;
        spawnTimer = currentPlan[0].DelayBeforeSpawn;
        state = WaveState.Running;
        GameAudio.Play(GameSound.WaveStart);
        RefreshUI();
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (HasDefeatedHall)
        {
            state = WaveState.Defeat;
            RefreshUI();
            return;
        }

        if(preparation.Tick(Time.deltaTime,CanStartPreparedWave))StartNextWave();

        if (state == WaveState.Running && Time.timeScale > 0f)
        {
            if (districtRoutes != null && !districtRoutes.MatchesCurrentBindings(defendedHall, runningRoutes))
            { Fail("Danh sách khu/tuyến đã thay đổi giữa đợt. Chỉ mở khu khi đang chuẩn bị."); return; }
            TickSpawning();
            if (state == WaveState.Running)
                CheckWaveComplete();
        }

        RefreshUI();
    }

    private void TickSpawning()
    {
        // currentPlan chỉ giữ kế hoạch của đợt đang chạy, không tích lũy vô hạn.
        if (nextSpawnIndex >= currentPlan.Length)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        // Tối đa một quái mỗi frame để tránh dồn sinh khi máy giật.
        GameObject prefab = currentPlan[nextSpawnIndex].Prefab;
        if (prefab == null || enemiesParent == null
            || !enemiesParent.gameObject.activeInHierarchy)
        {
            Fail("Prefab quái hoặc Enemies Parent không còn sẵn sàng.");
            return;
        }

        GameObject instance = Instantiate(prefab, enemiesParent);
        EnemyHealth health = instance.GetComponent<EnemyHealth>();
        EnemyRouteFollower movement = instance.GetComponent<EnemyRouteFollower>();
        EnemySiegeAttack siege = instance.GetComponent<EnemySiegeAttack>();

        if (health == null || movement == null || siege == null)
        {
            instance.SetActive(false);
            Destroy(instance);
            Fail("Quái vừa sinh thiếu component bắt buộc.");
            return;
        }

        // Follower dùng route để đặt quái ở đầu tuyến trong Start().
        // Configure phải chạy trước Start() của các component trên quái.
        DistrictDefenseRoutes.Binding binding = runningRoutes[nextSpawnIndex % runningRoutes.Length];
        movement.Configure(binding.route);
        siege.Configure(binding.hall);
        health.Configure(wallet, binding.hall);
        if (!health.ConfigureSpawnHealthMultiplier(currentHealthMultiplier))
        {
            instance.SetActive(false); Destroy(instance);
            Fail("Không áp dụng được hệ số máu cho địch vừa sinh."); return;
        }

        spawnedEnemies.Add(health);
        nextSpawnIndex++;

        if (nextSpawnIndex < currentPlan.Length)
            spawnTimer += currentPlan[nextSpawnIndex].DelayBeforeSpawn;
    }

    private void CheckWaveComplete()
    {
        // Không kết thúc sớm khi các nhóm sau vẫn chưa được sinh.
        if (nextSpawnIndex < currentPlan.Length)
            return;

        foreach (EnemyHealth enemy in spawnedEnemies)
        {
            if (enemy == null)
                continue;

            // EnemyHealth chỉ tắt GameObject sau khi Death đã phát xong.
            if (!enemy.HasDied || enemy.gameObject.activeSelf)
                return;
        }

        int reward=currentReward;
        if(reward>0 && !wallet.TryAddRewardExact(reward))
        {Fail("Ví không nhận được thưởng hoàn thành đợt.");return;}
        CompletedWaveCount = waveIndex + 1;
        if (expansion != null && CompletedWaveCount >= 5 && !expansion.IsExpanded)
        {
            if (!expansion.TryUnlock(out string unlockError)) { Fail(unlockError); return; }
        }
        state = CompletedWaveCount >= plans.Length
            ? (HasExpansionUnlocked ? (EndlessAvailable ? WaveState.Ready : WaveState.ExpansionReady) : WaveState.Finished)
            : WaveState.Ready;
        if (IsBetweenWaves) preparation.Reset(PreparationClock.Duration(CompletedWaveCount,HasExpansionUnlocked));
        if (state == WaveState.Ready) GameAudio.Play(GameSound.WaveClear);
    }

    private int CountLivingEnemies()
    {
        int count = 0;
        foreach (EnemyHealth enemy in spawnedEnemies)
        {
            if (enemy != null && !enemy.HasDied)
                count++;
        }
        return count;
    }

    private void RefreshUI()
    {
        int totalWaves = plans != null ? plans.Length : 0;
        int living = CountLivingEnemies();
        int pending = currentPlan != null ? currentPlan.Length - nextSpawnIndex : 0;

        if (waveText != null)
            waveText.text = EndlessAvailable
                ? (state == WaveState.Ready ? $"Đợt tiếp: {AvailableWaveNumber} • Vô tận" : $"Đợt: {waveIndex + 1} • Vô tận")
                : $"Đợt: {waveIndex + 1}/{totalWaves}";

        if (remainingEnemiesText != null)
            remainingEnemiesText.text =
                $"Còn lại: {living + pending}\n" +
                $"(trên sân: {living}, chưa xuất hiện: {pending})";

        if (startWaveButton != null)
            startWaveButton.interactable = CanStartPreparedWave;

        string preparationLabel = TutorialBlocksWave ? "Hoàn thành hoặc bỏ qua hướng dẫn Mara để bắt đầu chuẩn bị."
            : $"Chuẩn bị: {System.Math.Ceiling(preparation.Remaining):0}s • Bắt đầu ngay: +{preparation.EarlyReward} xu";
        if(preparationText!=null)
        {
            preparationText.gameObject.SetActive(IsBetweenWaves);
            preparationText.text = preparationLabel;
        }
        if(waveStatusText!=null && waveStatusText!=preparationText)
            waveStatusText.gameObject.SetActive(preparationText==null || !IsBetweenWaves);
        if (waveStatusText == null)
            return;

        switch (state)
        {
            case WaveState.Ready:
                waveStatusText.text = preparationText==null ? preparationLabel : (CompletedWaveCount == 0
                    ? "Sẵn sàng bắt đầu."
                    : $"Hoàn thành đợt {CompletedWaveCount}. Sẵn sàng đợt tiếp theo.");
                break;
            case WaveState.Running:
                waveStatusText.text = living + pending == 0
                    ? "Đang chờ animation kết thúc..."
                    : "Đợt tấn công đang diễn ra.";
                break;
            case WaveState.ExpansionReady:
                waveStatusText.text = "Đã mở hai khu mới! +2.000 xu. Hãy bố trí phòng thủ. Wave sau chương 1 chưa được cài ở mốc này.";
                break;
            case WaveState.Finished:
                waveStatusText.text = "Đã hoàn thành tất cả các đợt!";
                break;
            case WaveState.Defeat:
                waveStatusText.text = "Nhà chính đã bị phá hủy.";
                break;
            case WaveState.Error:
                waveStatusText.text = "Lỗi cấu hình wave: " + errorMessage;
                break;
        }
    }

    private bool TryCaptureRoutes(out DistrictDefenseRoutes.Binding[] active, out string error)
    {
        if (districtRoutes != null) return districtRoutes.TryGetActiveBindings(defendedHall, out active, out error);
        active = null; error = "";
        var result = new List<DistrictDefenseRoutes.Binding>();
        var seen = new HashSet<EnemyRoute>();
        int count = 1 + (additionalRoutes == null ? 0 : additionalRoutes.Length);
        for (int i = 0; i < count; i++)
        {
            EnemyRoute item = i == 0 ? route : additionalRoutes[i - 1];
            if (item == null || !item.IsValid || !item.isActiveAndEnabled || !seen.Add(item))
            { error = "Tuyến thiếu điểm, đang tắt hoặc bị gán trùng."; return false; }
            result.Add(new DistrictDefenseRoutes.Binding(item, defendedHall));
        }
        active = result.ToArray();
        return true;
    }

    [ContextMenu("Kiểm tra: chuẩn bị đợt 6 (trận mới)")]
    private void DebugPrepareWave6() => DebugPrepareEndless(6);
    [ContextMenu("Kiểm tra: chuẩn bị đợt 10 có boss (trận mới)")]
    private void DebugPrepareWave10() => DebugPrepareEndless(10);
    private void DebugPrepareEndless(int number)
    {
        if (!Application.isPlaying || !initialized || state != WaveState.Ready || CompletedWaveCount != 0
            || waveIndex != -1 || Time.timeScale <= 0 || endlessWaves == null || expansion == null)
        { Debug.LogWarning("Chỉ thử trên trận mới, chưa bấm bắt đầu đợt và không tạm dừng.", this); return; }
        if (!expansion.TryUnlock(out string error)) { Fail(error); return; }
        CompletedWaveCount = number - 1;
        waveIndex = number - 2;
        currentPlan = null;
        nextSpawnIndex = 0;
        currentReward = 0;
        currentHealthMultiplier = 1f;
        preparation.Reset(PreparationClock.Duration(CompletedWaveCount,true));
        RefreshUI();
        Debug.Log("Phiên kiểm tra: sẵn sàng đợt " + number + ". Chưa cộng thưởng các đợt bị bỏ qua.", this);
    }

    private void Fail(string message)
    {
        state = WaveState.Error;
        initialized = false;
        errorMessage = message;
        Debug.LogError("WaveDirector: " + message, this);
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.RemoveListener(StartNextWave);
    }
}
