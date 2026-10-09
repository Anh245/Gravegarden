using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]

public class EnemyAnimatorDriver : MonoBehaviour
{
    
    [SerializeField, Min(0.01f)] private float directionBias = 1.25f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private EnemyRouteFollower movement;
    private EnemySiegeAttack siege;

    private string currentState;
    private string walkState = "WalkSide";
    private bool facingInitialized;
    private bool facingLeft;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        movement = GetComponentInParent<EnemyRouteFollower>();
        siege = GetComponentInParent<EnemySiegeAttack>();
        if (movement == null || siege == null)
        {
            Debug.LogError(
                "Thiếu EnemyRouteFollower hoặc EnemySiegeAttack - Rotling thiếu component di chuyển hoặc tấn công",
                this
            );
            enabled = false;
        }

    }

    private void LateUpdate()
    {
        if (siege.IsDefeated)
        {
            animator.speed = 0f;
            return;
        }

        animator.speed = 1f;
        if (siege.IsAttacking)
        {
            spriteRenderer.flipX = false;
            PlayState("Attack");
            return;
        }


        Vector3 direction = movement.FrameMovement;

        if(direction.sqrMagnitude < 0.0000000001f)
        {
            PlayState("Idle");
            return;
        }

        float horizontal = Mathf.Abs(direction.x);
        float vertical = Mathf.Abs(direction.y);

        if (!facingInitialized)
        {
            walkState = vertical > horizontal * directionBias ? (direction.y > 0f ? "WalkUp" : "WalkDown") : "WalkSide";
            facingInitialized = true;
        }
        else if (vertical > horizontal * directionBias)
        {
            walkState = direction.y > 0f ? "WalkUp" : "WalkDown";

        }
        else if (horizontal > vertical * directionBias)
        {
            walkState = "WalkSide";
        }


        if (horizontal > 0.000001f)
            facingLeft = direction.x < 0f;

        spriteRenderer.flipX =
            walkState == "WalkSide" && facingLeft;

        PlayState(walkState);
    }


    private void PlayState(string stateName)
    {
        if (currentState == stateName)
            return;

        float startTime = 0f;

        // Giữ nhịp bước khi chuyển giữa các hướng đi.
        if (!string.IsNullOrEmpty(currentState) &&
            currentState.StartsWith("Walk") &&
            stateName.StartsWith("Walk"))
        {
            startTime =
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
        }

        currentState = stateName;

        animator.Play(
            "Base Layer." + stateName,
            0,
            startTime
        );
    }
}
