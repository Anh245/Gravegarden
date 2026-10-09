using UnityEngine;

public class BoneLilyTower : MonoBehaviour
{
    [SerializeField] private Animator visualAnimator;
    [SerializeField] private Transform muzzle;
    [SerializeField] private BoneProjectile projectilePrefab;

    [SerializeField] private EnemyHealth target;
    [SerializeField] private TownHallHealth defendedHall;

    [SerializeField, Min(0.01f)] private float attackRange = 3.6f;
    [SerializeField, Min(0.01f)] private float attackinterval = 0.8f;

    [SerializeField, Min(0.01f)] private float damage = 18f;

    private float cooldown;


    private void Awake()
    {
        if (visualAnimator == null ||
            muzzle == null ||
            projectilePrefab == null ||
            target == null ||
            defendedHall == null)
        {
            Debug.LogError(
                "Bone Lily chưa được gán đủ tham chiếu.",
                this
            );

            enabled = false;
        }


    }


    private void Update()
    {
        if (defendedHall.IsDestroyed)
        {
            visualAnimator.speed = 0f;
            return;
        }

        cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);

        if (target == null || !target.IsAlive)
            return;

        float distance = Vector2.Distance(
            transform.position,
            target.transform.position
        );

        if (distance > attackRange || cooldown > 0f)
            return;
        Fire();
        cooldown = attackinterval;
    }

    //Ham tan cong
    private void Fire()
    {
        //Animator phu trach hinh anh dong
        visualAnimator.Play("Base Layer.Attack", 0, 0f);

        //Gameplay phu trach tao dan va sat thuong
        //Tao dan moi
        //Instantiate projectilePrefab tai vi tri muzzle.position, khong xoay
        BoneProjectile projectile = Instantiate(
            projectilePrefab,
            muzzle.position,
            Quaternion.identity
        );


        //Khoi tao projectile voi target, damage, defendedHall
        projectile.Initialize(target, damage, defendedHall);


    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
