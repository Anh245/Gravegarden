using Unity.VisualScripting;
using UnityEngine;

public class EnemyRoute : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    public int PointCount => points == null ? 0: points.Length;

    public bool IsValid
    {
        get
        {
            if(PointCount < 2)
                return false;
            foreach(Transform point in points)
            {
                if (point == null)
                    return false;
            }

            return true;
        }
    }

    public Vector3 GetPoint(int index)
    {
        return points[index].position;
    }

    private void OnDrawGizmos()
    {
        if (points == null)
            return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            if(points[i] == null)
            {
                continue;
            }
            Gizmos.DrawSphere(points[i].position, 0.08f);

            if( i + 1 < points.Length && points[i + 1] != null)
            {
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
        }
    }
   
}
