using UnityEngine;

public class EnemyRouteFollower : MonoBehaviour
{
    [SerializeField] private EnemyRoute route;
    private float moveSpeed;

    [Tooltip("Khoảng cách xuất phát dọc theo tuyến. Dùng để kiểm thử.")]
    [SerializeField, Min(0f)] private float startingDistance = 0f;

    private int targetIndex;
    private bool reachedEnd;

    private float totalLength;
    private float distanceTravelled;

    public Vector3 FrameMovement { get; private set; }
    public bool HasReachedEnd => reachedEnd;
    public bool IsReady { get; private set; }

    public float Progress =>
        totalLength > 0f
            ? Mathf.Clamp01(distanceTravelled / totalLength)
            : 0f;

    private void Start()
    {

        EnemyHealth health = GetComponent<EnemyHealth>();

        EnemyDefinition data =
            health != null ? health.Definition : null;

        if (data == null || !data.IsValid)
        {
            Debug.LogError(
                "EnemyRouteFollower không tìm thấy dữ liệu địch hợp lệ.",
                this
            );

            enabled = false;
            return;
        }

        moveSpeed = data.MoveSpeed;

        if (route == null || !route.IsValid)
        {
            Debug.LogError("Địch chưa có tuyến hợp lệ.", this);
            enabled = false;
            return;
        }

        totalLength = 0f;

        for (int i = 1; i < route.PointCount; i++)
        {
            totalLength += Vector3.Distance(
                route.GetPoint(i - 1),
                route.GetPoint(i)
            );
        }

        if (totalLength <= 0.0001f)
        {
            Debug.LogError("Tuyến đường có chiều dài bằng 0.", this);
            enabled = false;
            return;
        }

        transform.position = route.GetPoint(0);
        targetIndex = 1;
        distanceTravelled = 0f;
        reachedEnd = false;
        FrameMovement = Vector3.zero;

        // Đặt địch tại một khoảng cách xác định trên tuyến.
        Advance(Mathf.Clamp(startingDistance, 0f, totalLength));

        IsReady = true;
    }

    private void Update()
    {
        FrameMovement = Vector3.zero;

        if (!IsReady || reachedEnd)
            return;

        Vector3 beforeMove = transform.position;

        Advance(moveSpeed * Time.deltaTime);

        FrameMovement = transform.position - beforeMove;
    }

    private void Advance(float remainingDistance)
    {
        while (remainingDistance > 0f &&
               targetIndex < route.PointCount)
        {
            Vector3 destination = route.GetPoint(targetIndex);

            float segmentRemaining = Vector3.Distance(
                transform.position,
                destination
            );

            if (segmentRemaining <= remainingDistance)
            {
                transform.position = destination;
                distanceTravelled += segmentRemaining;
                remainingDistance -= segmentRemaining;
                targetIndex++;
            }
            else
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destination,
                    remainingDistance
                );

                distanceTravelled += remainingDistance;
                remainingDistance = 0f;
            }
        }

        if (!reachedEnd && targetIndex >= route.PointCount)
        {
            reachedEnd = true;
            distanceTravelled = totalLength;

            Debug.Log("Rotling đã đến đích.", this);
        }
    }
    public void Configure(EnemyRoute newRoute)
    {
        route = newRoute;
        startingDistance = 0f;
    }
}