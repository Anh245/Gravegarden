using UnityEngine;
using UnityEngine.UI;

public class AudioIconPresenter : MonoBehaviour
{
    [SerializeField] private AudioSettingsUI owner;
    [SerializeField] private Button musicButton,effectsButton;
    [SerializeField] private Image musicIcon,effectsIcon;
    [SerializeField] private Sprite musicOn,musicOff,effectsOn,effectsOff;
    private bool ready;
    private void Start()
    {
        if(owner==null || musicButton==null || effectsButton==null || musicIcon==null || effectsIcon==null
            || musicOn==null || musicOff==null || effectsOn==null || effectsOff==null)
        {Debug.LogError("AudioIconPresenter: thiếu tham chiếu.",this);enabled=false;return;}
        musicButton.onClick.AddListener(owner.ToggleMusic);effectsButton.onClick.AddListener(owner.ToggleEffects);ready=true;
    }
    private void LateUpdate()
    {
        if(!ready)return;
        musicIcon.sprite=owner.MusicEnabled?musicOn:musicOff;
        effectsIcon.sprite=owner.EffectsEnabled?effectsOn:effectsOff;
        bool available=GameAudio.Current!=null && owner.isActiveAndEnabled;
        musicButton.interactable=effectsButton.interactable=available;
        float alpha=GameAudio.Current!=null && (GameAudio.Current.Muted || GameAudio.Current.Master<=0)? .45f:1f;
        musicIcon.color=effectsIcon.color=new Color(1,1,1,alpha);
    }
    private void OnDestroy()
    {
        if(!ready || owner==null)return;
        if(musicButton!=null)musicButton.onClick.RemoveListener(owner.ToggleMusic);
        if(effectsButton!=null)effectsButton.onClick.RemoveListener(owner.ToggleEffects);
    }
}
