using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class GardenPauseController : MonoBehaviour
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private BuildModeController buildMode;
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField] private GardenSaveController saveController;
    [SerializeField] private GameResultController gameResult;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveAndMenuButton;
    [SerializeField] private Button menuWithoutSaveButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Header("Hướng dẫn - tùy chọn")]
    [SerializeField] private Button helpButton;
    [SerializeField] private HelpGuidePanel helpGuide;
    private bool initialized;
    private bool paused;
    private float resumeTimeScale = 1f;
    private bool CanPause => initialized && !paused && !SceneNavigator.IsTransitioning
        && !gameResult.HasEnded && waveDirector.CanBuildNow && Time.timeScale > 0f
        && !saveController.IsOfflineDialogOpen;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (buildManager == null || buildMode == null || waveDirector == null || saveController == null
            || gameResult == null || pausePanel == null || pauseButton == null || resumeButton == null
            || saveAndMenuButton == null || menuWithoutSaveButton == null || statusText == null)
        {
            Debug.LogError("GardenPauseController: thiếu tham chiếu.", this);
            enabled = false;
            return;
        }
        if(helpButton!=null && helpGuide!=null)helpButton.onClick.AddListener(ShowHelp);
        else if(helpButton!=null || helpGuide!=null)Debug.LogWarning("Gán đủ Help Button và Help Guide để dùng hướng dẫn trong Pause.",this);
        if(helpGuide!=null)helpGuide.Close();
        pauseButton.onClick.AddListener(Pause);
        resumeButton.onClick.AddListener(Resume);
        saveAndMenuButton.onClick.AddListener(SaveAndMenu);
        menuWithoutSaveButton.onClick.AddListener(MenuWithoutSave);
        initialized = true;
    }
    private void Update()
    {
        if (!initialized) return;
        pauseButton.interactable = CanPause;
        if (paused && gameResult.HasEnded)
        {
            if(helpGuide!=null)helpGuide.Close();
            paused = false;
            pausePanel.SetActive(false);
            return;
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if(helpGuide!=null && helpGuide.IsOpen){helpGuide.Close();return;}
            if (saveController.IsOfflineDialogOpen) { saveController.CloseOfflinePanel(); return; }
            if(buildManager.IsMovingBuilding) {buildManager.CancelBuildingMove();return;}
            if (paused) Resume();
            else if (buildMode.IsBuildMode) buildMode.ExitBuildMode();
            else Pause();
        }
        if (paused)
        {
            saveAndMenuButton.interactable = waveDirector.CanSaveCheckpoint && saveController.CheckpointReady;
        }
    }
    private void ShowHelp()
    {
        if(paused && !gameResult.HasEnded && helpGuide!=null)helpGuide.Open();
    }
    public void Pause()
    {
        if (!CanPause) return;
        buildMode.ExitBuildMode();
        buildManager.CloseForCheckpoint();
        foreach (TowerUpgradeUI ui in UnityEngine.Object.FindObjectsByType<TowerUpgradeUI>()) ui.ClosePanel();
        foreach (ProducerUpgradeUI ui in UnityEngine.Object.FindObjectsByType<ProducerUpgradeUI>()) ui.ClosePanel();
        foreach (SupportUpgradeUI ui in UnityEngine.Object.FindObjectsByType<SupportUpgradeUI>()) ui.ClosePanel();
        if(helpGuide!=null)helpGuide.Close();
        resumeTimeScale = Time.timeScale;
        paused = true;
        statusText.text = waveDirector.CanSaveCheckpoint
            ? "Lưu và về menu để giữ trạng thái hiện tại. Về menu không lưu sẽ dùng checkpoint cũ khi Tiếp tục."
            : "Đợt đang chạy: chưa thể lưu giữa trận. Về menu không lưu sẽ dùng checkpoint gần nhất.";
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();
    }
    public void Resume()
    {
        if (!paused) return;
        if(helpGuide!=null)helpGuide.Close();
        paused = false;
        pausePanel.SetActive(false);
        if (!gameResult.HasEnded) Time.timeScale = resumeTimeScale;
    }
    private void SaveAndMenu()
    {
        if (!paused || gameResult.HasEnded || !waveDirector.CanSaveCheckpoint || SceneNavigator.IsTransitioning) return;
        Time.timeScale = resumeTimeScale;
        bool saved = saveController.TrySaveCheckpoint();
        if (!saved)
        {
            Time.timeScale = 0f;
            statusText.text = "Không lưu được: " + saveController.LastMessage;
            return;
        }
        GoToMenu();
    }
    private void MenuWithoutSave()
    {
        if (!paused || SceneNavigator.IsTransitioning) return;
        GoToMenu();
    }
    private void GoToMenu()
    {
        if (SceneNavigator.TryLoad(mainMenuSceneName, GardenEntryMode.None, out string error))
            paused = false;
        else { Time.timeScale = 0f; statusText.text = error; }
    }
    private void OnDisable()
    {
        if(helpGuide!=null)helpGuide.Close();
        if (paused)
        {
            paused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameResult == null || !gameResult.HasEnded) Time.timeScale = resumeTimeScale;
        }
    }
    private void OnDestroy()
    {
        if(helpButton!=null)helpButton.onClick.RemoveListener(ShowHelp);
        if (pauseButton != null) pauseButton.onClick.RemoveListener(Pause);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (saveAndMenuButton != null) saveAndMenuButton.onClick.RemoveListener(SaveAndMenu);
        if (menuWithoutSaveButton != null) menuWithoutSaveButton.onClick.RemoveListener(MenuWithoutSave);
    }
}
