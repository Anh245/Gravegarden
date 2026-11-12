using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider,musicSlider,effectsSlider;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private TMP_Text masterValue,musicValue,effectsValue;
    private bool initialized;
    private float lastMusic = .65f, lastEffects = .75f;
    public bool MusicEnabled => GameAudio.Current!=null && GameAudio.Current.Music>0;
    public bool EffectsEnabled => GameAudio.Current!=null && GameAudio.Current.Effects>0;
    public void ToggleMusic()
    {
        if(!initialized || GameAudio.Current==null)return;
        if(musicSlider.value>0){lastMusic=musicSlider.value;PlayerPrefs.SetFloat("gravegarden.audio.last_music",lastMusic);musicSlider.value=0;}
        else musicSlider.value=Mathf.Clamp(lastMusic,.01f,1f);
    }
    public void ToggleEffects()
    {
        if(!initialized || GameAudio.Current==null)return;
        if(effectsSlider.value>0){lastEffects=effectsSlider.value;PlayerPrefs.SetFloat("gravegarden.audio.last_effects",lastEffects);effectsSlider.value=0;}
        else effectsSlider.value=Mathf.Clamp(lastEffects,.01f,1f);
    }
    private void Start()
    {
        if(masterSlider==null || musicSlider==null || effectsSlider==null || muteToggle==null
            || masterValue==null || musicValue==null || effectsValue==null)
        {Debug.LogError("AudioSettingsUI: gán đủ 3 slider, 3 text và Mute Toggle.",this);enabled=false;return;}
        foreach(Slider slider in new[]{masterSlider,musicSlider,effectsSlider})
        {slider.minValue=0f;slider.maxValue=1f;slider.wholeNumbers=false;}
        masterSlider.onValueChanged.AddListener(OnLevelChanged);
        musicSlider.onValueChanged.AddListener(OnLevelChanged);
        effectsSlider.onValueChanged.AddListener(OnLevelChanged);
        muteToggle.onValueChanged.AddListener(OnMuteChanged);
        lastMusic=Mathf.Clamp(PlayerPrefs.GetFloat("gravegarden.audio.last_music",.65f),.01f,1f);
        lastEffects=Mathf.Clamp(PlayerPrefs.GetFloat("gravegarden.audio.last_effects",.75f),.01f,1f);
        initialized=true;Refresh();
    }
    private void OnEnable() {if(initialized) Refresh();}
    private void Refresh()
    {
        if(GameAudio.Current==null) {Debug.LogError("Scene chưa có GameAudio đã gán Library.",this);return;}
        masterSlider.SetValueWithoutNotify(GameAudio.Current.Master);
        musicSlider.SetValueWithoutNotify(GameAudio.Current.Music);
        effectsSlider.SetValueWithoutNotify(GameAudio.Current.Effects);
        muteToggle.SetIsOnWithoutNotify(GameAudio.Current.Muted);
        if(GameAudio.Current.Music>0)lastMusic=GameAudio.Current.Music;
        if(GameAudio.Current.Effects>0)lastEffects=GameAudio.Current.Effects;
        UpdateLabels();
    }
    private void OnLevelChanged(float value) => Apply();
    private void OnMuteChanged(bool value) => Apply();
    private void Apply()
    {
        if(!initialized || GameAudio.Current==null) return;
        if(musicSlider.value>0){lastMusic=musicSlider.value;PlayerPrefs.SetFloat("gravegarden.audio.last_music",lastMusic);}
        if(effectsSlider.value>0){lastEffects=effectsSlider.value;PlayerPrefs.SetFloat("gravegarden.audio.last_effects",lastEffects);}
        GameAudio.Current.SetLevels(masterSlider.value,musicSlider.value,effectsSlider.value,muteToggle.isOn);
        UpdateLabels();
    }
    private void UpdateLabels()
    {
        masterValue.text=Mathf.RoundToInt(masterSlider.value*100f)+"%";
        musicValue.text=Mathf.RoundToInt(musicSlider.value*100f)+"%";
        effectsValue.text=Mathf.RoundToInt(effectsSlider.value*100f)+"%";
    }
    private void OnDisable() {if(GameAudio.Current!=null) GameAudio.Current.SaveSettings();}
    private void OnDestroy()
    {
        if(!initialized) return;
        masterSlider.onValueChanged.RemoveListener(OnLevelChanged);
        musicSlider.onValueChanged.RemoveListener(OnLevelChanged);
        effectsSlider.onValueChanged.RemoveListener(OnLevelChanged);
        muteToggle.onValueChanged.RemoveListener(OnMuteChanged);
    }
}
