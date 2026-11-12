using UnityEngine;

// Mốc bố trí, chưa phải nhà chính có máu hay mục tiêu chiến đấu.
public class ExpansionHallMarker : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, .5f);
        Gizmos.DrawLine(transform.position + Vector3.left, transform.position + Vector3.right);
        Gizmos.DrawLine(transform.position + Vector3.down, transform.position + Vector3.up);
    }
}
