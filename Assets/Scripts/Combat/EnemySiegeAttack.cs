using UnityEngine;

[RequireComponent(typeof(EnemyRouteFollower))]

public class EnemySiegeAttack : MonoBehaviour
{
    [SerializeField] private TownHallHealth target;

    private int damage;
    private float firstHitDelay;
    private float attackInterval;
    //Set up animation for attack



    private EnemyRouteFollower movement;

    private bool attackStarted;
    private float hitTimer;

    public bool IsDefeated => target != null && target.IsDestroyed;


    public bool IsAttacking => isActiveAndEnabled && attackStarted && target != null && !target.IsDestroyed;

    public float AttackInterval => attackInterval;


    private void Awake()
    {
        movement = GetComponent<EnemyRouteFollower>();
       


       
    }

    public void Configure(TownHallHealth newTarget)
    {
        target = newTarget;
    }

    private void Start()
    {
        EnemyHealth health = GetComponent<EnemyHealth>();
        EnemyDefinition data = health ! != null ? health.Definition : null;


        if (data == null || !data.IsValid)
        {
            Debug.LogError(
                "EnemySiegeAttack không tìm thấy dữ liệu địch hợp lệ.",
                this
            );

            enabled = false;
            return;
        }

        damage = data.SiegeDamage;
        firstHitDelay = data.FirstHitDelay;
        attackInterval = data.AttackInterval;

        if (target == null)
        {
            Debug.LogError(
                "Rotling chưa được gán nhà chính.",
                this
            );

            enabled = false;
        }
    }

    private void Update()
    {
        if (target == null)
            return;
        if (target.IsDestroyed)
        {
            //Dung di chuyen khi nha chinh bi pha huy
            movement.enabled = false;
            return;
        }

        if (!movement.HasReachedEnd || !movement.IsReady)
            return;

        if (!attackStarted)
        {
            attackStarted = true;
            hitTimer = firstHitDelay;

            //Dem tu frame tiep theo

            return;
        }

        hitTimer -= Time.deltaTime;

        while( hitTimer <= 0f && !target.IsDestroyed)
        {
            target.TakeDamage(damage);
            hitTimer += attackInterval;
        }
    }
}
