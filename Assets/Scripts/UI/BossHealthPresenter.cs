using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthPresenter : MonoBehaviour
{
    [SerializeField] private EnemyDefinition bossDefinition;
    [SerializeField] private WaveDirector waves;
    [SerializeField] private GameObject panel;
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text label;
    private EnemyHealth target;
    private int alive;
    private float nextScan;
    private void Update()
    {
        if(waves==null || panel==null || fill==null || label==null || bossDefinition==null)return;
        if(!waves.IsWaveRunning || waves.HasDefeatedHall){panel.SetActive(false);target=null;return;}
        if(Time.unscaledTime>=nextScan || target==null || !target.IsAlive)
        {
            nextScan=Time.unscaledTime+.2f;alive=0;
            if(target!=null && !target.IsAlive)target=null;
            foreach(var enemy in Object.FindObjectsByType<EnemyHealth>())
                if(enemy.gameObject.scene==gameObject.scene && enemy.IsAlive && enemy.Definition==bossDefinition)
                {alive++;if(target==null)target=enemy;}
        }
        bool visible=target!=null && target.IsAlive;panel.SetActive(visible);
        if(!visible)return;
        fill.fillAmount=target.MaxHealth>0?Mathf.Clamp01(target.CurrentHealth/target.MaxHealth):0;
        label.text=bossDefinition.DisplayName+" • Còn "+alive+" boss  |  "+System.Math.Ceiling(target.CurrentHealth).ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+" / "+System.Math.Ceiling(target.MaxHealth).ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
    }
}
