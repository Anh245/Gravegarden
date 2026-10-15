using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameResultController : MonoBehaviour
{
    [Header("Hệ thống trận đấu")]
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField] private BuildManager buildManager;

    [Header("Bảng kết quả")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    [Header("Nút chơi lại")]
    [SerializeField] private Button victoryRetryButton;
    [SerializeField] private Button defeatRetryButton;

    [Header("Scene chơi lại")]
    [SerializeField] private string gardenSceneName = "Garden";

    private bool initialized;
    private bool hasEnded;
    private bool isReloading;

    private void Awake()
    {
        // Mỗi trận mới bắt đầu với tốc độ bình thường.
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (defendedHall == null ||
            waveDirector == null ||
            buildManager == null ||
            victoryPanel == null ||
            defeatPanel == null ||
            victoryRetryButton == null ||
            defeatRetryButton == null)
        {
            Debug.LogError(
                "GameResultController chưa được gán đủ tham chiếu.",
                this
            );

            enabled = false;
            return;
        }

        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);

        victoryRetryButton.onClick.AddListener(RestartGame);
        defeatRetryButton.onClick.AddListener(RestartGame);

        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized || hasEnded)
            return;

        // Ưu tiên thất bại nếu nhà chính đã bị phá hủy.
        if (defendedHall.IsDestroyed)
        {
            EndGame(false);
            return;
        }

        if (waveDirector.HasCompletedAllWaves)
        {
            EndGame(true);
        }
    }

    private void EndGame(bool victory)
    {
        if (hasEnded)
            return;

        hasEnded = true;

        buildManager.StopBuilding();

        victoryPanel.SetActive(victory);
        defeatPanel.SetActive(!victory);

        if (victory)
            victoryPanel.transform.SetAsLastSibling();
        else
            defeatPanel.transform.SetAsLastSibling();

        // Dừng thời gian của gameplay và Animator dùng thời gian thường.
        Time.timeScale = 0f;

        Debug.Log(
            victory
                ? "Trận đấu kết thúc: Chiến thắng."
                : "Trận đấu kết thúc: Thất bại.",
            this
        );
    }

    private void RestartGame()
    {
        if (!initialized || !hasEnded || isReloading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(gardenSceneName))
        {
            Debug.LogError(
                $"Không thể tải scene '{gardenSceneName}'. " +
                "Hãy kiểm tra Scene List trong Build Profiles.",
                this
            );

            return;
        }

        // Chặn việc bấm liên tiếp tạo nhiều yêu cầu tải scene.
        isReloading = true;

        victoryRetryButton.interactable = false;
        defeatRetryButton.interactable = false;

        Time.timeScale = 1f;

        SceneManager.LoadSceneAsync(
            gardenSceneName,
            LoadSceneMode.Single
        );
    }

    private void OnDestroy()
    {
        if (victoryRetryButton != null)
        {
            victoryRetryButton.onClick.RemoveListener(
                RestartGame
            );
        }

        if (defeatRetryButton != null)
        {
            defeatRetryButton.onClick.RemoveListener(
                RestartGame
            );
        }

        Time.timeScale = 1f;
    }
}