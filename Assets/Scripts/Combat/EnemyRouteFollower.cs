using Unity.VisualScripting;
using UnityEngine;

public class EnemyRouteFollower : MonoBehaviour
{
    [SerializeField] private EnemyRoute route;
    [SerializeField,Min(0.01f)] private float moveSpeed = 0.64f;


    private int targetIndex;
    private bool reachedEnd;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(route == null || !route.IsValid)
        {
            Debug.LogError(
                "Chưa gán EnemyRoute hợp lệ cho EnemyRouteFollower.",
                this
            );

            enabled = false;
            return;
        }

        transform.position = route.GetPoint(0);
        targetIndex = 1;
        reachedEnd = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (reachedEnd)
        {
            return;
        } 
        
        float remainingDistance = moveSpeed * Time.deltaTime;
        // Di chuyển đến điểm tiếp theo (cho phép di chuyển nhiều hơn 1 điểm trong 1 frame nếu cần)

        //Khong mat quang duong con lai

        while (remainingDistance > 0f && targetIndex < route.PointCount) {
            
            Vector3 target = route.GetPoint(targetIndex);
            float distance = Vector3.Distance(transform.position, target);

            if(distance <= remainingDistance)
            {
                // Di chuyển đến điểm tiếp theo
                transform.position = target;
                remainingDistance -= distance;
                targetIndex++;
            }
            else
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    remainingDistance
                );

                remainingDistance = 0f;
            }
        }
    }
}
