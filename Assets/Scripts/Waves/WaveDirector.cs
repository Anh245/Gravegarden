using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveDirector : MonoBehaviour
{
    private enum WaveState
    {
        Ready,
        Running,
        Finished,
        Defeat
    }

    [Header("Địch và tuyến đường")]
    [SerializeField] private GameObject rotlingPrefab;
    [SerializeField] private EnemyRoute route;
    [SerializeField] private Transform enemiesParent;

    [Header("Hệ thống trong scene")]
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private PlayerWallet wallet;

    [Header("Giao diện")]
    [SerializeField] private Button startWaveButton;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text remainingEnemiesText;
    [SerializeField] private TMP_Text waveStatusText;

    [Header("Các đợt kiểm thử")]
    [SerializeField]
    private int[] enemiesPerWave =
    {
        5, 8, 12
    };

    [SerializeField, Min(0.1f)]
    private float spawnInterval = 2f;

    private readonly List<EnemyHealth> spawnedEnemies =
        new List<EnemyHealth>();

    private WaveState state = WaveState.Ready;

    // -1 nghĩa là chưa bắt đầu đợt nào.
    private int waveIndex = -1;

    private int spawnedCount;
    private float spawnTimer;
    private bool initialized;

    public bool HasCompletedAllWaves =>
    state == WaveState.Finished;

    private void Start()
    {
        if (!ValidateSetup())
        {
            if (startWaveButton != null)
                startWaveButton.interactable = false;

            if (waveStatusText != null)
                waveStatusText.text = "Thiếu cấu hình wave.";

            enabled = false;
            return;
        }

        initialized = true;

        startWaveButton.onClick.AddListener(StartNextWave);

        RefreshUI();
    }

    private bool ValidateSetup()
    {
        if (rotlingPrefab == null ||
            route == null ||
            enemiesParent == null ||
            defendedHall == null ||
            wallet == null ||
            startWaveButton == null ||
            waveText == null ||
            remainingEnemiesText == null ||
            waveStatusText == null)
        {
            Debug.LogError(
                "WaveDirector chưa được gán đủ tham chiếu.",
                this
            );

            return false;
        }

        if (!route.IsValid)
        {
            Debug.LogError(
                "Tuyến đường của wave chưa hợp lệ.",
                this
            );

            return false;
        }

        var health =
            rotlingPrefab.GetComponent<EnemyHealth>();

        var movement =
            rotlingPrefab.GetComponent<EnemyRouteFollower>();

        var siege =
            rotlingPrefab.GetComponent<EnemySiegeAttack>();

        if (health == null ||
            movement == null ||
            siege == null)
        {
            Debug.LogError(
                "Prefab Rotling thiếu script trên object gốc.",
                this
            );

            return false;
        }

        if (!rotlingPrefab.activeSelf ||
            !health.enabled ||
            !movement.enabled ||
            !siege.enabled ||
            !enemiesParent.gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "Prefab, script của Rotling hoặc Enemies đang bị tắt.",
                this
            );

            return false;
        }

        if (enemiesPerWave == null ||
            enemiesPerWave.Length == 0)
        {
            Debug.LogError(
                "Chưa có dữ liệu các đợt.",
                this
            );

            return false;
        }

        foreach (int count in enemiesPerWave)
        {
            if (count <= 0)
            {
                Debug.LogError(
                    "Mỗi đợt phải có ít nhất một địch.",
                    this
                );

                return false;
            }
        }

        if (spawnInterval <= 0f)
        {
            Debug.LogError(
                "Khoảng cách sinh địch phải lớn hơn 0.",
                this
            );

            return false;
        }

        return true;
    }

    public void StartNextWave()
    {
        if (!initialized ||
            state != WaveState.Ready ||
            defendedHall.IsDestroyed)
        {
            return;
        }

        // Dọn các Rotling đã chết của đợt trước.
        foreach (EnemyHealth enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }

        spawnedEnemies.Clear();

        waveIndex++;
        spawnedCount = 0;
        spawnTimer = 0f;

        // Đổi trạng thái ngay để chặn bấm nhiều lần.
        state = WaveState.Running;

        RefreshUI();
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (defendedHall.IsDestroyed)
        {
            state = WaveState.Defeat;
            RefreshUI();
            return;
        }

        if (state == WaveState.Running)
        {
            UpdateSpawning();
            CheckWaveCompletion();
        }

        RefreshUI();
    }

    private void UpdateSpawning()
    {
        int totalToSpawn = enemiesPerWave[waveIndex];

        if (spawnedCount >= totalToSpawn)
            return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f)
            return;

        SpawnRotling();

        spawnTimer = spawnInterval;
    }

    private void SpawnRotling()
    {
        GameObject instance = Instantiate(
            rotlingPrefab,
            route.GetPoint(0),
            Quaternion.identity,
            enemiesParent
        );

        var movement =
            instance.GetComponent<EnemyRouteFollower>();

        var siege =
            instance.GetComponent<EnemySiegeAttack>();

        var health =
            instance.GetComponent<EnemyHealth>();

        // Gán tham chiếu ngay sau Instantiate,
        // trước khi các component chạy Start.
        movement.Configure(route);
        siege.Configure(defendedHall);
        health.Configure(wallet, defendedHall);

        spawnedCount++;

        instance.name =
            $"Rotling_W{waveIndex + 1}_{spawnedCount:00}";

        spawnedEnemies.Add(health);
    }

    private void CheckWaveCompletion()
    {
        // Chưa sinh đủ thì chưa thể hoàn thành.
        if (spawnedCount < enemiesPerWave[waveIndex])
            return;

        foreach (EnemyHealth enemy in spawnedEnemies)
        {
            // Chỉ xác nhận khi địch đã chết thật.
            if (enemy == null || !enemy.HasDied)
                return;

            // Chờ animation chết chạy xong và object bị ẩn.
            if (enemy.gameObject.activeSelf)
                return;
        }

        bool isLastWave =
            waveIndex >= enemiesPerWave.Length - 1;

        state = isLastWave
            ? WaveState.Finished
            : WaveState.Ready;

        Debug.Log(
            $"Hoàn thành đợt {waveIndex + 1}.",
            this
        );
    }

    private int CountLivingEnemies()
    {
        int count = 0;

        foreach (EnemyHealth enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.IsAlive)
                count++;
        }

        return count;
    }

    private void RefreshUI()
    {
        int alive = CountLivingEnemies();

        int pending = waveIndex >= 0
            ? enemiesPerWave[waveIndex] - spawnedCount
            : 0;

        int remaining = alive + pending;

        waveText.text =
            $"Đợt: {waveIndex + 1}/{enemiesPerWave.Length}";

        remainingEnemiesText.text =
            $"Còn lại: {remaining} " +
            $"(trên sân: {alive}, chưa xuất hiện: {pending})";

        startWaveButton.interactable =
            state == WaveState.Ready &&
            !defendedHall.IsDestroyed;

        switch (state)
        {
            case WaveState.Ready:
                waveStatusText.text = waveIndex < 0
                    ? "Sẵn sàng bắt đầu."
                    : $"Hoàn thành đợt {waveIndex + 1}. " +
                      "Bạn có thể bắt đầu đợt tiếp theo.";
                break;

            case WaveState.Running:
                waveStatusText.text = remaining == 0
                    ? "Đang chờ animation chết kết thúc..."
                    : "Đợt đang diễn ra.";
                break;

            case WaveState.Finished:
                waveStatusText.text =
                    "Đã hoàn thành tất cả các đợt kiểm thử!";
                break;

            case WaveState.Defeat:
                waveStatusText.text =
                    "Nhà chính đã bị phá hủy.";
                break;
        }
    }

    private void OnDestroy()
    {
        if (startWaveButton != null)
        {
            startWaveButton.onClick.RemoveListener(
                StartNextWave
            );
        }
    }
}