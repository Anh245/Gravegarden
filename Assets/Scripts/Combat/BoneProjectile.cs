using UnityEngine;

public class BoneProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float speed = 10.5f;
    [SerializeField, Min(0.1f)] private float maxLifetime = 5f;


    private EnemyHealth target;
    private TownHallHealth defendedHall;
    private float damage;
    private float age;
    private bool initialized;
    private bool resolved;


    public void Initialize(EnemyHealth newTarget, float newDamage, TownHallHealth hall)
    {
        target = newTarget;
        damage = newDamage;
        defendedHall = hall;
        initialized = true;
    }


    private void Update()
    {
        if (resolved)
            return;

        age += Time.deltaTime;

        if (!initialized || age >= maxLifetime || target == null || !target.IsAlive || defendedHall == null || defendedHall.IsDestroyed) {


            Remove();
            return;

        }

        Vector3 destination = target.AimPosition;
        float step = speed * Time.deltaTime;

        if(Vector3.Distance(transform.position, destination) <= step)
        {
            transform.position = destination;
            resolved = true;

            target.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(

            transform.position,
            destination,
            step
        );
    }

    private void Remove()
    {
        resolved = true;
        Destroy(gameObject);
    }
}
