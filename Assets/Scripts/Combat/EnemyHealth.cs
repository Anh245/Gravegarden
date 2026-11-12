using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Dữ liệu địch")]
    [SerializeField]
    private EnemyDefinition definition;

    public static event System.Action<EnemyHealth,float> DamageReceived;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetFeedbackEvent()=>DamageReceived=null;
    public float MaxHealth=>maxHealth;
    private float maxHealth;
    private int bounty;
    private bool spawnHealthConfigured;
    // Chỉ gọi ngay sau Instantiate, trước khi địch nhận sát thương.
    public bool ConfigureSpawnHealthMultiplier(float multiplier)
    {
        if (!enabled || definition == null || deathStarted || spawnHealthConfigured || CurrentHealth != maxHealth
            || multiplier < 1f || float.IsNaN(multiplier) || float.IsInfinity(multiplier)) return false;
        double value = definition.MaxHealth * (double)multiplier;
        if (value <= 0 || value > float.MaxValue) return false;
        maxHealth = (float)value;
        CurrentHealth = maxHealth;
        spawnHealthConfigured = true;
        return true;
    }

    public EnemyDefinition Definition => definition;

    [Header("Tham chieu trong prefab")]
    [SerializeField] private Transform aimPoint;
    [SerializeField] private Animator visualAnimator;
    [SerializeField] private AnimationClip deathClip;

    [Header("Tham chieu trong scene")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TownHallHealth defendedHall;


    private EnemyRouteFollower movement;
    private EnemySiegeAttack siege;
    private EnemyAnimatorDriver animationDriver;

    private bool deathStarted;

    [Header("Kiểm tra thiêu đốt")]
    [SerializeField] private bool logBurnTicks;
    private readonly BurnState burn = new BurnState();
    public bool IsBurning => burn.IsActive;
    public float CurrentBurnDamagePerSecond => burn.DamagePerSecond;
    public float BurnTimeRemaining => burn.RemainingTime;


    public float CurrentHealth { get; private set; }

    public bool HasDied => deathStarted;

    public bool IsAlive =>
        gameObject.activeInHierarchy && CurrentHealth > 0f;

    public Vector3 AimPosition =>
        aimPoint != null ? aimPoint.position : transform.position;

    private void Awake()
    {
        movement = GetComponent<EnemyRouteFollower>();
        siege = GetComponent<EnemySiegeAttack>();

        animationDriver =
            GetComponentInChildren<EnemyAnimatorDriver>();

        if (definition == null || !definition.IsValid)
        {
            Debug.LogError(
                "EnemyHealth thiếu Definition " +
                "hoặc dữ liệu địch không hợp lệ.",
                this
            );

            enabled = false;
            return;
        }

        maxHealth = definition.MaxHealth;
        bounty = definition.Bounty;

        CurrentHealth = maxHealth;
    }
    public void Configure(PlayerWallet newWallet, TownHallHealth newDefendedHall)
    {
        wallet = newWallet;
        defendedHall = newDefendedHall;
    }

    private void Start()
    {
        if (visualAnimator == null ||
        deathClip == null ||
        wallet == null ||
        defendedHall == null ||
        movement == null ||
        siege == null ||
        animationDriver == null)
        {
            Debug.LogError(
                "EnemyHealth chưa được gán đủ tham chiếu.",
                this
            );

            enabled = false;
            return;
        }

        int deathState =
        Animator.StringToHash("Base Layer.Death");

        if (!visualAnimator.HasState(0, deathState))
        {
            Debug.LogError(
                "Animator của Rotling chưa có state Death.",
                this
            );

            enabled = false;
        }
    }

    public void ApplyBurn(float damagePerSecond, float duration)
    {
        if (!enabled || !IsAlive || deathStarted || defendedHall == null || defendedHall.IsDestroyed)
            return;
        burn.Apply(damagePerSecond, duration);
    }

    private void Update()
    {
        if (!IsAlive || deathStarted || defendedHall == null || defendedHall.IsDestroyed)
        {
            burn.Clear();
            return;
        }
        if (Time.timeScale <= 0f)
            return;

        float tickDamage = burn.Advance(Time.deltaTime);
        if (tickDamage <= 0f)
            return;
        if (logBurnTicks)
            Debug.Log($"{gameObject.name} nhận {tickDamage:0.##} sát thương thiêu đốt.", this);
        TakeDamage(tickDamage);
    }

    private void OnDisable()
    {
        burn.Clear();
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        float healthBefore=CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        Debug.Log(
            $"Rotling còn {CurrentHealth}/{maxHealth} máu.",
            this
        );

        if (CurrentHealth <= 0f)
        {
            //Mốc hiện tại: dừng toàn bộ địch khi chết.
            //gameObject.SetActive(false);

            //Update: Them logic sau khi enemy chet

            BeginDeath();
        }
        float actualDamage=healthBefore-CurrentHealth;
        if(actualDamage>0f && !float.IsNaN(actualDamage) && !float.IsInfinity(actualDamage))
        {
            // Hiệu ứng không được làm gián đoạn xử lý sát thương, tiền thưởng hoặc Death.
            try {DamageReceived?.Invoke(this,actualDamage);}
            catch(System.Exception exception) {Debug.LogException(exception,this);}
        }
    }

    private void BeginDeath()
    {
        if (deathStarted)
            return;

        //Khoa trang thai truoc khi cong tien
        deathStarted = true;
        burn.Clear();

        movement.enabled = false;
        siege.enabled = false;

        //Ngan script doi animation Death -> Walk || Attack
        animationDriver.enabled = false;

        wallet.AddReward(bounty);
        visualAnimator.speed = 1f;
        visualAnimator.Play("Base Layer.Death", 0, 0f);

        StartCoroutine(HideAfterDeath());
    }

    private IEnumerator HideAfterDeath()
    {
        yield return new WaitForSeconds(deathClip.length);

        //Cho phep haon thanh lan cap nhat hinh anh cuoi
        yield return null;

        gameObject.SetActive(false);

    }
}