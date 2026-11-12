using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1100)]
public class GameSpeedController : MonoBehaviour
{
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField] private GameResultController gameResult;
    [SerializeField] private GardenSaveController saveController;
    [SerializeField] private Button normalSpeedButton;
    [SerializeField] private Button doubleSpeedButton;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Color selectedColor=Color.white;
    [SerializeField] private Color otherColor=new Color(0.58f,0.58f,0.65f,1f);
    public int SelectedSpeed { get; private set; }=1;
    private bool initialized;

    private bool CanChange=>initialized && isActiveAndEnabled && !SceneNavigator.IsTransitioning
        && waveDirector.CanBuildNow && !gameResult.HasEnded && !saveController.IsOfflineDialogOpen
        && Time.timeScale>0f;

    private void Start()
    {
        if(waveDirector==null || gameResult==null || saveController==null
            || normalSpeedButton==null || doubleSpeedButton==null || speedText==null
            || normalSpeedButton==doubleSpeedButton)
        {
            Debug.LogError("GameSpeedController: gán đủ 6 trường, hai nút phải khác nhau.",this);
            enabled=false;return;
        }
        // Không viết Time.timeScale trong Start/Update: không được tự mở lại một trận đang dừng.
        SelectedSpeed=Time.timeScale>=1.5f?2:1;
        normalSpeedButton.transition=Selectable.Transition.None;
        doubleSpeedButton.transition=Selectable.Transition.None;
        normalSpeedButton.onClick.AddListener(SetNormalSpeed);
        doubleSpeedButton.onClick.AddListener(SetDoubleSpeed);
        initialized=true;Refresh();
    }
    private void Update()=>Refresh();
    public void SetNormalSpeed()=>SetSpeed(1);
    public void SetDoubleSpeed()=>SetSpeed(2);
    private void SetSpeed(int speed)
    {
        if(!CanChange || (speed!=1 && speed!=2)) return;
        bool changed=SelectedSpeed!=speed;
        SelectedSpeed=speed;
        Time.timeScale=speed;
        if(changed) GameAudio.Play(GameSound.Accept,true);
        Refresh();
    }
    private void Refresh()
    {
        if(!initialized) return;
        bool available=CanChange;
        normalSpeedButton.interactable=available;
        doubleSpeedButton.interactable=available;
        if(normalSpeedButton.targetGraphic!=null)
            normalSpeedButton.targetGraphic.color=SelectedSpeed==1?selectedColor:otherColor;
        if(doubleSpeedButton.targetGraphic!=null)
            doubleSpeedButton.targetGraphic.color=SelectedSpeed==2?selectedColor:otherColor;
        speedText.text="Tốc độ: "+SelectedSpeed+"x";
    }
    private void OnEnable(){if(initialized) Refresh();}
    private void OnDisable()
    {
        if(normalSpeedButton!=null) normalSpeedButton.interactable=false;
        if(doubleSpeedButton!=null) doubleSpeedButton.interactable=false;
    }
    private void OnDestroy()
    {
        if(normalSpeedButton!=null) normalSpeedButton.onClick.RemoveListener(SetNormalSpeed);
        if(doubleSpeedButton!=null) doubleSpeedButton.onClick.RemoveListener(SetDoubleSpeed);
        // SceneNavigator/GameResultController quản lý tốc độ khi đổi scene hoặc kết thúc trận.
    }
}
