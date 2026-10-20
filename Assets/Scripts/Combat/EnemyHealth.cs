using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Dữ liệu địch")]
    [SerializeField]
    private EnemyDefinition definition;

    private float maxHealth;
    private int bounty;

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

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

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
    }

    private void BeginDeath()
    {
        if (deathStarted)
            return;

        //Khoa trang thai truoc khi cong tien
        deathStarted = true;

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