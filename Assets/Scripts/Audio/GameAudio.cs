using UnityEngine;

// Một instance trong mỗi scene. Khi đổi scene, object cũ được hủy bình thường.
[DefaultExecutionOrder(-1000)]
public class GameAudio : MonoBehaviour
{
    [SerializeField] private AudioLibrary library;
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField] private GameResultController gameResult;
    public static GameAudio Current { get; private set; }
    public float Master { get; private set; }
    public float Music { get; private set; }
    public float Effects { get; private set; }
    public bool Muted { get; private set; }
    private AudioSource calm,battle;
    private sealed class Voice { public AudioSource source; public bool always; public bool paused; public float gain; }
    private readonly Voice[] voices = new Voice[16];
    private readonly SoundCooldown cooldown = new SoundCooldown();
    private float mix;
    private bool gameplayPaused;
    private bool ready;
    private bool dirty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Current = null;
    private void Awake()
    {
        if (Current != null && Current != this)
        { Debug.LogError("Chỉ giữ một GameAudio trong mỗi scene.",this); enabled=false; return; }
        if (library == null) { Debug.LogError("GameAudio: chưa gán Audio Library.",this); enabled=false;return; }
        Current=this;
        Master=AudioMixMath.Unit(PlayerPrefs.GetFloat("gravegarden.audio.master",0.8f));
        Music=AudioMixMath.Unit(PlayerPrefs.GetFloat("gravegarden.audio.music",0.65f));
        Effects=AudioMixMath.Unit(PlayerPrefs.GetFloat("gravegarden.audio.effects",0.75f));
        Muted=PlayerPrefs.GetInt("gravegarden.audio.muted",0)!=0;
        calm=CreateSource("CalmMusic",true);battle=CreateSource("BattleMusic",true);
        calm.clip=library.calmMusic;battle.clip=library.battleMusic;
        for(int i=0;i<voices.Length;i++) voices[i]=new Voice{source=CreateSource("Sfx_"+i,false)};
        ready=true;
        ApplyVolumes();
        if(calm.clip!=null) calm.Play();
        if(battle.clip!=null) battle.Play();
        if(calm.clip==null || battle.clip==null) Debug.LogWarning("Audio Library thiếu nhạc nền.",this);
    }
    private AudioSource CreateSource(string name,bool loop)
    {
        var child=new GameObject(name);child.transform.SetParent(transform,false);
        AudioSource source=child.AddComponent<AudioSource>();
        source.playOnAwake=false;source.loop=loop;source.spatialBlend=0f;
        source.volume=0f;source.priority=loop?32:128;
        return source;
    }
    private void Update()
    {
        if(!ready) return;
        bool paused=Time.timeScale<=0f;
        if(paused!=gameplayPaused)
        {
            gameplayPaused=paused;
            foreach(Voice voice in voices)
            {
                if(voice.always) continue;
                if(paused && voice.source.isPlaying) {voice.source.Pause();voice.paused=true;}
                else if(!paused && voice.paused) {voice.source.UnPause();voice.paused=false;}
            }
        }
        float target=waveDirector!=null && waveDirector.IsWaveRunning && !paused
            && (gameResult==null || !gameResult.HasEnded) ? 1f:0f;
        mix=Mathf.Lerp(mix,target,1f-Mathf.Exp(-Time.unscaledDeltaTime*1.7f));
        ApplyVolumes();
    }
    private void ApplyVolumes()
    {
        if(!ready) return;
        float dim=Time.timeScale<=0f || waveDirector==null ? 0.7f:1f;
        float musicGain=AudioMixMath.Gain(Master,Music,Muted)*dim;
        calm.volume=musicGain*AudioMixMath.Calm(mix)*0.126f;
        battle.volume=musicGain*AudioMixMath.Battle(mix)*0.1f;
        float fxGain=AudioMixMath.Gain(Master,Effects,Muted);
        foreach(Voice voice in voices) voice.source.volume=fxGain*voice.gain;
    }
    public void SetLevels(float master,float music,float effects,bool muted)
    {
        Master=AudioMixMath.Unit(master);Music=AudioMixMath.Unit(music);Effects=AudioMixMath.Unit(effects);Muted=muted;
        PlayerPrefs.SetFloat("gravegarden.audio.master",Master);
        PlayerPrefs.SetFloat("gravegarden.audio.music",Music);
        PlayerPrefs.SetFloat("gravegarden.audio.effects",Effects);
        PlayerPrefs.SetInt("gravegarden.audio.muted",Muted?1:0);
        dirty=true;
        ApplyVolumes();
    }
    public void SaveSettings() { if(dirty) {PlayerPrefs.Save();dirty=false;} }
    public static void Play(GameSound sound,bool whilePaused=false)
    {
        if(Current!=null && Current.isActiveAndEnabled) Current.PlayInternal(sound,whilePaused);
    }
    private void PlayInternal(GameSound sound,bool whilePaused)
    {
        if(!ready || Muted || AudioMixMath.Gain(Master,Effects,false)<=0f || (!whilePaused && Time.timeScale<=0f)) return;
        AudioClip clip=library.Clip(sound);
        if(clip==null) return;
        double gap=sound==GameSound.Shot || sound==GameSound.Impact ? 0.09d:0.035d;
        if(!cooldown.TryEnter(sound,Time.unscaledTimeAsDouble,gap)) return;
        Voice voice=null;
        foreach(Voice item in voices) if(!item.source.isPlaying && !item.paused) {voice=item;break;}
        if(voice==null && whilePaused)
            foreach(Voice item in voices) if(!item.always) {voice=item;break;}
        if(voice==null) return;
        voice.source.Stop();voice.source.clip=clip;voice.always=whilePaused;voice.paused=false;
        voice.gain=sound==GameSound.Shot || sound==GameSound.Impact ? 0.089f
            : sound==GameSound.HeartDamage?0.447f:0.2f;
        voice.source.volume=AudioMixMath.Gain(Master,Effects,Muted)*voice.gain;
        voice.source.Play();
    }
    public static void Finish(bool victory)
    {
        if(Current==null || !Current.ready) return;
        foreach(Voice voice in Current.voices) {voice.source.Stop();voice.paused=false;}
        Play(victory?GameSound.Victory:GameSound.Defeat,true);
    }
    private void OnApplicationPause(bool pause) {if(pause) SaveSettings();}
    private void OnApplicationQuit() => SaveSettings();
    private void OnDisable()
    {
        if(!ready) return;
        calm.Stop();battle.Stop();
        foreach(Voice voice in voices) {voice.source.Stop();voice.paused=false;}
        SaveSettings();
    }
    private void OnEnable()
    {
        if(!ready) return;
        if(calm.clip!=null && !calm.isPlaying) calm.Play();
        if(battle.clip!=null && !battle.isPlaying) battle.Play();
    }
    private void OnDestroy() {SaveSettings();if(Current==this) Current=null;}
}
