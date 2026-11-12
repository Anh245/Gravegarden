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

    [Header("Về menu - tùy chọn")]
    [SerializeField] private Button victoryMenuButton;
    [SerializeField] private Button defeatMenuButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    public bool HasEnded => hasEnded;
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

        if (victoryMenuButton != null) victoryMenuButton.onClick.AddListener(ReturnToMenu);
        if (defeatMenuButton != null) defeatMenuButton.onClick.AddListener(ReturnToMenu);
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized || hasEnded)
            return;

        // Ưu tiên thất bại nếu nhà chính đã bị phá hủy.
        if (defendedHall.IsDestroyed || waveDirector.HasDefeatedHall)
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
        GameAudio.Finish(victory);

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

        Navigate(gardenSceneName, GardenEntryMode.NewGame);
    }

    private void ReturnToMenu()
    {
        if (!initialized || !hasEnded || isReloading) return;
        Navigate(mainMenuSceneName, GardenEntryMode.None);
    }
    private void Navigate(string scene, GardenEntryMode mode)
    {
        if (!SceneNavigator.TryLoad(scene, mode, out string error))
        { Debug.LogError(error, this); return; }
        isReloading = true;
        victoryRetryButton.interactable = false;
        defeatRetryButton.interactable = false;
        if (victoryMenuButton != null) victoryMenuButton.interactable = false;
        if (defeatMenuButton != null) defeatMenuButton.interactable = false;
    }

    private void OnDestroy()
    {
        if (victoryMenuButton != null) victoryMenuButton.onClick.RemoveListener(ReturnToMenu);
        if (defeatMenuButton != null) defeatMenuButton.onClick.RemoveListener(ReturnToMenu);
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