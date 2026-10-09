using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] walkDown;
    [SerializeField] private Sprite[] walkRight;
    [SerializeField] private Sprite[] walkUp;

    [SerializeField, Min(0.1f)] private float framesPerSecond = 4f;
    [SerializeField, Min(1.01f)] private float directionBias = 1.25f;

    [SerializeField] private Sprite[] attack;

    private EnemySiegeAttack siege;
    private bool wasAttacking;
    private float attackTime;

    private SpriteRenderer spriteRenderer;
    private EnemyRouteFollower movement;

    private enum Facing
    {
        Down,
        Side,
        Up
    }

    private Facing facing = Facing.Side;
    private bool facingInitialized;
    private bool facingLeft;
    private float animationTime;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        movement = GetComponentInParent<EnemyRouteFollower>();
        siege = GetComponentInParent<EnemySiegeAttack>();

        if (movement == null ||
            siege == null ||
            !HasFrames(walkDown) ||
            !HasFrames(walkRight) ||
            !HasFrames(walkUp))
        {
            Debug.LogError(
                "Thiếu EnemyRouteFollower hoặc frame animation Rotling.",
                this
            );

            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (siege.IsDefeated)
            return;

        if (siege.IsAttacking)
        {
            if (!wasAttacking)
            {
                attackTime = 0f;
                wasAttacking = true;
            }

            int attackFrameIndex = Mathf.FloorToInt(
                attackTime * attack.Length / siege.AttackInterval
            ) % attack.Length;

            spriteRenderer.sprite = attack[attackFrameIndex];
            spriteRenderer.flipX = false;

            attackTime += Time.deltaTime;
            return;
        }

        wasAttacking = false;


        Vector3 direction = movement.FrameMovement;

        // Tạm giữ frame hiện tại khi địch đứng yên.
        if (direction.sqrMagnitude < 0.0000000001f)
            return;

        UpdateFacing(direction);

        animationTime += Time.deltaTime;

        Sprite[] frames = GetFrames();

        int frameIndex =
            Mathf.FloorToInt(animationTime * framesPerSecond)
            % frames.Length;

        spriteRenderer.sprite = frames[frameIndex];

        // Chỉ lật hình ngang khi dùng bộ sprite đi ngang.
        spriteRenderer.flipX =
            facing == Facing.Side && facingLeft;
    }

    private void UpdateFacing(Vector3 direction)
    {
        float horizontal = Mathf.Abs(direction.x);
        float vertical = Mathf.Abs(direction.y);

        if (!facingInitialized)
        {
            facing = vertical > horizontal * directionBias
                ? (direction.y > 0f ? Facing.Up : Facing.Down)
                : Facing.Side;

            facingInitialized = true;
        }
        else if (vertical > horizontal * directionBias)
        {
            facing = direction.y > 0f ? Facing.Up : Facing.Down;
        }
        else if (horizontal > vertical * directionBias)
        {
            facing = Facing.Side;
        }

        if (horizontal > 0.000001f)
            facingLeft = direction.x < 0f;
    }

    private Sprite[] GetFrames()
    {
        if (facing == Facing.Up)
            return walkUp;

        if (facing == Facing.Down)
            return walkDown;

        return walkRight;
    }

    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
            return false;

        foreach (Sprite frame in frames)
        {
            if (frame == null)
                return false;
        }

        return true;
    }
}