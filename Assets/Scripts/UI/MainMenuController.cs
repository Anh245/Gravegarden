using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gardenSceneName = "Garden";
    [SerializeField] private string scenarioId = "garden_v1";
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private Button closeHelpButton;
    [SerializeField] private TMP_Text statusText;
    private bool initialized;
    private bool canContinue;

    private void Awake() => Time.timeScale = 1f;
    private void Start()
    {
        if (newGameButton == null || continueButton == null || helpButton == null || quitButton == null
            || helpPanel == null || closeHelpButton == null || statusText == null
            || !CheckpointValidator.IsValidId(scenarioId))
        {
            Debug.LogError("MainMenuController: gán đủ nút, panel, text và Scenario Id.", this);
            enabled = false;
            return;
        }
        helpPanel.SetActive(false);
        newGameButton.onClick.AddListener(NewGame);
        continueButton.onClick.AddListener(Continue);
        helpButton.onClick.AddListener(ShowHelp);
        closeHelpButton.onClick.AddListener(CloseHelp);
        quitButton.onClick.AddListener(Quit);
        initialized = true;
        RefreshCheckpointInfo();
        string message = SceneNavigator.ConsumeMessage();
        if (!string.IsNullOrWhiteSpace(message)) statusText.text = message;
        UpdateButtons();
    }

    private void RefreshCheckpointInfo()
    {
        string path = Path.Combine(Application.persistentDataPath, GardenCheckpointFormat.FileName);
        canContinue = false;
        if (!CheckpointFileStore.TryRead(path, out string json, out string error))
        {
            statusText.text = error + " Chọn Chơi mới để bắt đầu.";
            return;
        }
        try
        {
            canContinue = MenuCheckpointInfo.IsReadable(JsonUtility.FromJson<GardenSaveData>(json), scenarioId, out string message);
            statusText.text = message;
        }
        catch (System.Exception) { statusText.text = "Không đọc được JSON của checkpoint."; }
    }
    private void Update() => UpdateButtons();
    private void UpdateButtons()
    {
        if (!initialized) return;
        bool available = !SceneNavigator.IsTransitioning;
        newGameButton.interactable = available;
        continueButton.interactable = available && canContinue;
        helpButton.interactable = available;
        quitButton.interactable = available;
    }
    private void NewGame() => EnterGarden(GardenEntryMode.NewGame);
    private void Continue()
    {
        if (canContinue) EnterGarden(GardenEntryMode.ContinueCheckpoint);
    }
    private void EnterGarden(GardenEntryMode entry)
    {
        if (!initialized || SceneNavigator.IsTransitioning) return;
        if (!SceneNavigator.TryLoad(gardenSceneName, entry, out string error)) statusText.text = error;
        UpdateButtons();
    }
    private void ShowHelp() { helpPanel.SetActive(true); helpPanel.transform.SetAsLastSibling(); }
    private void CloseHelp() => helpPanel.SetActive(false);
    private void Quit()
    {
        if (Application.isEditor) statusText.text = "Nút Thoát sẽ đóng ứng dụng trong bản build. Trong Editor, dùng nút Stop.";
        else Application.Quit();
    }
    private void OnDestroy()
    {
        if (newGameButton != null) newGameButton.onClick.RemoveListener(NewGame);
        if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        if (helpButton != null) helpButton.onClick.RemoveListener(ShowHelp);
        if (closeHelpButton != null) closeHelpButton.onClick.RemoveListener(CloseHelp);
        if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
    }
}
