using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildModeController : MonoBehaviour
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private BuildGrid buildGrid;
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField] private GameResultController gameResult;
    [SerializeField] private GardenSaveController saveController;
    [SerializeField] private Button toggleBuildButton;
    [SerializeField] private TMP_Text buttonLabel;
    public bool IsBuildMode { get; private set; }
    private bool initialized;
    private bool CanBuild => initialized && !SceneNavigator.IsTransitioning
        && waveDirector.CanBuildNow && !gameResult.HasEnded && Time.timeScale > 0f
        && !saveController.IsOfflineDialogOpen;

    private void Start()
    {
        if (buildGrid != null) buildGrid.SetBuildMode(false);
        if (buildManager == null || buildGrid == null || waveDirector == null || gameResult == null
            || saveController == null || toggleBuildButton == null || buttonLabel == null)
        {
            Debug.LogError("BuildModeController: thiếu tham chiếu.", this);
            enabled = false;
            return;
        }
        toggleBuildButton.onClick.AddListener(ToggleBuildMode);
        initialized = true;
        UpdateLabel();
    }
    private void Update()
    {
        if (!initialized) return;
        if(buildManager.IsMovingBuilding) {toggleBuildButton.interactable=false;return;}
        if (IsBuildMode && !CanBuild) ExitBuildMode();
        if (CanBuild && Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame) ToggleBuildMode();
        toggleBuildButton.interactable = CanBuild;
    }
    public void ToggleBuildMode()
    {
        if(buildManager!=null && buildManager.IsMovingBuilding) return;
        if (!CanBuild) return;
        IsBuildMode = !IsBuildMode;
        if (!IsBuildMode) buildManager.CloseForCheckpoint();
        buildGrid.SetBuildMode(IsBuildMode);
        UpdateLabel();
    }
    public void ExitBuildMode()
    {
        if(buildManager!=null) buildManager.CancelBuildingMove();
        IsBuildMode = false;
        if (buildManager != null) buildManager.CloseForCheckpoint();
        if (buildGrid != null) buildGrid.SetBuildMode(false);
        UpdateLabel();
    }
    private void UpdateLabel()
    {
        if (buttonLabel != null) buttonLabel.text = IsBuildMode ? "Xong" : "Xây dựng";
    }
    private void OnDestroy()
    {
        if (toggleBuildButton != null) toggleBuildButton.onClick.RemoveListener(ToggleBuildMode);
    }
}
