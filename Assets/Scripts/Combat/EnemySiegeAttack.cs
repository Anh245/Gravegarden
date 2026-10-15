using UnityEngine;

[RequireComponent(typeof(EnemyRouteFollower))]

public class EnemySiegeAttack : MonoBehaviour
{
    [SerializeField] private TownHallHealth target;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.01f)] private float firstHitDelay = 0.65f;
    [SerializeField, Min(0.01f)] private float attackInterval = 1.6f;

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
        if (target == null) {
            Debug.LogError(
                    "Rotling chua duoc gan nha chinh", this

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
