using UnityEngine;
public class BootLoader : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    private void Start()
    {
        if (!SceneNavigator.TryLoad(mainMenuSceneName, GardenEntryMode.None, out string error))
            Debug.LogError(error, this);
    }
}
