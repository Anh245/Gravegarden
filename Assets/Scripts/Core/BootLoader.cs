using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private string gardenSceneName = "Garden";
    
    void Start()
    {
        //Kiem tra xem scene co ton tai trong Build Settings hay khong
        if (!Application.CanStreamedLevelBeLoaded(gardenSceneName))
        {
            Debug.LogError($"Khong tim thay scene{gardenSceneName} Hay them scene vao Build Settings", this);

            return;
                
        }

        Debug.Log($"Boot: Dang mo scene {gardenSceneName}...");


        // Load scene

        SceneManager.LoadSceneAsync( gardenSceneName , LoadSceneMode.Single);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
