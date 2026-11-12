using UnityEngine;
[CreateAssetMenu(menuName="GraveGarden/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    public AudioClip calmMusic;
    public AudioClip battleMusic;
    public AudioClip placement, upgrade, coin, towerShot, boneImpact, heartDamage;
    public AudioClip uiAccept, uiReject, victory, defeat;
    public AudioClip Clip(GameSound sound)
    {
        switch(sound)
        {
            case GameSound.Placement:case GameSound.Production:return placement;
            case GameSound.Upgrade:case GameSound.WaveStart:return upgrade;
            case GameSound.Collect:return coin;
            case GameSound.Shot:return towerShot;
            case GameSound.Impact:return boneImpact;
            case GameSound.HeartDamage:return heartDamage;
            case GameSound.Accept:return uiAccept;
            case GameSound.Reject:return uiReject;
            case GameSound.Victory:case GameSound.WaveClear:return victory;
            case GameSound.Defeat:return defeat;
            default:return null;
        }
    }
}
