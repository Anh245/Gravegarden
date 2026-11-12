using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNavigator
{
    private static GameFlowState state = new GameFlowState();
    public static bool IsTransitioning => state.IsTransitioning;
    public static GardenEntryMode ConsumeEntry() => state.ConsumeEntry();
    public static string ConsumeMessage() => state.ConsumeMessage();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        state = new GameFlowState();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => state.Arrived();

    public static bool TryLoad(string sceneName, GardenEntryMode entry, out string error, string menuMessage = "")
    {
        error = "";
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            error = "Không tìm thấy scene '" + sceneName + "'. Kiểm tra Scene List trong Build Profiles.";
            return false;
        }
        if (!state.TryBegin(entry, menuMessage))
        {
            error = "Đang chuyển scene.";
            return false;
        }
        try
        {
            Time.timeScale = 1f;
            if (SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single) == null)
                throw new InvalidOperationException("Không tạo được thao tác tải scene.");
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            state.Cancel(error);
            return false;
        }
    }
}
