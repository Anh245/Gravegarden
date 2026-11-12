using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(300)]
public class EnemyCombatFeedback : MonoBehaviour
{
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField] private TMP_FontAsset damageFont;
    [SerializeField] private Material barMaterial;
    [SerializeField,Min(0.1f)] private float barWidth=0.75f;
    [SerializeField,Min(0.01f)] private float barHeight=0.07f;
    [SerializeField,Min(0f)] private float verticalOffset=0.12f;
    [SerializeField,Min(0.1f)] private float numberLifetime=0.7f;
    [SerializeField,Min(0.1f)] private float numberFontSize=3f;
    [SerializeField,Range(8,256)] private int maxTrackedEnemies=128;
    [SerializeField,Range(8,64)] private int maxNumbers=32;
    [SerializeField] private string sortingLayerName="Default";
    [SerializeField] private int sortingOrder=80;
    [SerializeField] private Color healthColor=new Color(0.45f,0.9f,0.55f,1f);
    [SerializeField] private Color numberColor=new Color(1f,0.9f,0.65f,1f);
    private sealed class Bar {public GameObject root;public SpriteRenderer background,fill;}
    private sealed class Tracked
    {
        public EnemyHealth enemy;public SpriteRenderer visual;public Bar bar;
        public readonly DamageBurst damage=new DamageBurst();public Vector3 position;
    }
    private sealed class Number {public TextMeshPro text;public Vector3 start;public float age;}
    private readonly Dictionary<EnemyHealth,Tracked> tracked=new Dictionary<EnemyHealth,Tracked>();
    private readonly List<EnemyHealth> remove=new List<EnemyHealth>();
    private readonly Stack<Bar> freeBars=new Stack<Bar>();
    private readonly List<Number> numbers=new List<Number>();
    private Sprite solid;
    private bool ready;
    private int createdBars;
    private int serial;
    private static EnemyCombatFeedback current;
    private static readonly CultureInfo Vietnamese=CultureInfo.GetCultureInfo("vi-VN");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()=>current=null;
    private void Awake()
    {
        if(defendedHall==null || waveDirector==null || damageFont==null || barMaterial==null
            || (current!=null && current!=this))
        {Debug.LogError("EnemyCombatFeedback: gán đủ Hall, Wave, Damage Font, Bar Material; chỉ dùng một controller.",this);enabled=false;return;}
        current=this;
        Texture2D white=Texture2D.whiteTexture;
        solid=Sprite.Create(white,new Rect(0,0,white.width,white.height),new Vector2(0.5f,0.5f),white.width);
        ready=true;
    }
    private void OnEnable(){if(ready) EnemyHealth.DamageReceived+=OnDamage;}
    private void OnDamage(EnemyHealth enemy,float damage)
    {
        if(!ready || enemy==null || enemy.gameObject.scene!=gameObject.scene || damage<=0f
            || defendedHall.IsDestroyed || waveDirector.HasCompletedAllWaves) return;
        if(!tracked.TryGetValue(enemy,out Tracked item))
        {
            if(tracked.Count>=Mathf.Clamp(maxTrackedEnemies,8,256)) return;
            item=new Tracked{enemy=enemy,visual=enemy.GetComponentInChildren<SpriteRenderer>(),bar=GetBar()};
            tracked.Add(enemy,item);
        }
        item.position=GetPosition(item);item.damage.Add(damage);
    }
    private Vector3 GetPosition(Tracked item)
    {
        if(item.enemy==null) return item.position;
        Vector3 position=item.enemy.transform.position;
        position.y=item.visual!=null?item.visual.bounds.max.y+verticalOffset:item.enemy.AimPosition.y+verticalOffset;
        return position;
    }
    private Bar GetBar()
    {
        if(freeBars.Count>0) {Bar reused=freeBars.Pop();reused.root.SetActive(true);return reused;}
        if(createdBars>=Mathf.Clamp(maxTrackedEnemies,8,256)) return null;
        var root=new GameObject("EnemyHealthBar");root.transform.SetParent(transform,false);
        var bar=new Bar{root=root,background=CreateSprite(root,"Track"),fill=CreateSprite(root,"Fill")};
        bar.background.color=new Color(0.08f,0.06f,0.12f,0.95f);
        bar.background.sortingOrder=sortingOrder;bar.fill.sortingOrder=sortingOrder+1;
        createdBars++;return bar;
    }
    private SpriteRenderer CreateSprite(GameObject parent,string name)
    {
        var child=new GameObject(name);child.transform.SetParent(parent.transform,false);
        var sprite=child.AddComponent<SpriteRenderer>();sprite.sprite=solid;sprite.sharedMaterial=barMaterial;
        sprite.sortingLayerName=sortingLayerName;return sprite;
    }
    private void LateUpdate()
    {
        if(!ready) return;
        if(defendedHall.IsDestroyed || waveDirector.HasCompletedAllWaves) {ClearFeedback();return;}
        remove.Clear();
        foreach(var pair in tracked)
        {
            Tracked item=pair.Value;
            if(item.enemy==null || !item.enemy.gameObject.activeInHierarchy)
            {Release(item);remove.Add(pair.Key);continue;}
            item.position=GetPosition(item);
            bool dead=!item.enemy.IsAlive;
            if(item.damage.Advance(Time.deltaTime,dead,out double damage)) SpawnNumber(item.position,damage);
            if(dead) {Release(item);remove.Add(pair.Key);continue;}
            if(item.bar==null) continue;
            float ratio=CombatFeedbackMath.HealthFraction(item.enemy.CurrentHealth,item.enemy.MaxHealth);
            item.bar.root.transform.position=item.position;
            item.bar.background.transform.localScale=new Vector3(barWidth+0.03f,barHeight+0.03f,1f);
            item.bar.fill.transform.localScale=new Vector3(barWidth*ratio,barHeight,1f);
            item.bar.fill.transform.localPosition=new Vector3(-barWidth*(1f-ratio)*0.5f,0f,0f);
            item.bar.fill.color=healthColor;
        }
        foreach(EnemyHealth enemy in remove) tracked.Remove(enemy);
        foreach(Number number in numbers)
        {
            if(!number.text.gameObject.activeSelf) continue;
            number.age+=Time.deltaTime;
            float t=number.age/Mathf.Max(0.1f,numberLifetime);
            if(t>=1f) {number.text.gameObject.SetActive(false);continue;}
            number.text.transform.position=number.start+Vector3.up*(0.6f*t);
            Color color=numberColor;color.a*=1f-t;number.text.color=color;
        }
    }
    private void SpawnNumber(Vector3 position,double damage)
    {
        Number number=null;
        foreach(Number candidate in numbers) if(!candidate.text.gameObject.activeSelf) {number=candidate;break;}
        if(number==null)
        {
            if(numbers.Count>=Mathf.Clamp(maxNumbers,8,64)) return;
            var root=new GameObject("DamageNumber");root.transform.SetParent(transform,false);
            var text=root.AddComponent<TextMeshPro>();text.font=damageFont;text.fontSize=numberFontSize;
            text.alignment=TextAlignmentOptions.Center;text.overflowMode=TextOverflowModes.Overflow;
            text.rectTransform.sizeDelta=new Vector2(4f,1f);
            var renderer=text.GetComponent<MeshRenderer>();renderer.sortingLayerName=sortingLayerName;renderer.sortingOrder=sortingOrder+2;
            number=new Number{text=text};numbers.Add(number);
        }
        number.start=position+new Vector3((serial++%3-1)*0.12f,0.22f,0f);number.age=0f;
        number.text.transform.position=number.start;number.text.color=numberColor;
        number.text.text=damage<0.01d?"<0,01":damage.ToString("0.##",Vietnamese);
        number.text.gameObject.SetActive(true);
    }
    private void Release(Tracked item)
    {
        if(item.bar==null) return;
        item.bar.root.SetActive(false);freeBars.Push(item.bar);item.bar=null;
    }
    private void ClearFeedback()
    {
        foreach(Tracked item in tracked.Values) Release(item);
        tracked.Clear();
        foreach(Number number in numbers) if(number.text!=null) number.text.gameObject.SetActive(false);
    }
    private void OnDisable(){EnemyHealth.DamageReceived-=OnDamage;ClearFeedback();}
    private void OnDestroy(){if(solid!=null) Destroy(solid);if(current==this) current=null;}
}
