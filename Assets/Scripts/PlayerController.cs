using UnityEngine;

public enum PlayerDirection
{
    left, right
}

public enum PlayerState
{
    idle, walking, jumping, dead
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private SpriteRenderer playerSprite;
    private PlayerDirection currentDirection = PlayerDirection.right;
    public PlayerState currentState = PlayerState.idle;
    public PlayerState previousState = PlayerState.idle;

    private float timer;

    [Header("Horizontal")]
    public float maxSpeed = 5f;
    public float accelerationTime = 0.25f;
    public float decelerationTime = 0.15f;
    public float dashCoyoteTime = 1.0f;

    public bool canDashAgain;

    [Header("Vertical")]
    public float apexHeight = 3f;
    public float apexTime = 0.5f;

    [Header("Ground Checking")]
    public float groundCheckOffset = 0.5f;
    public Vector2 groundCheckSize = new(0.4f, 0.1f);
    public LayerMask groundCheckMask;

    private float accelerationRate;
    private float decelerationRate;

    private float gravity;
    private float initialJumpSpeed;

    private bool isGrounded = false;
    public bool isDead = false;

    private Vector2 velocity;

    public void Start()
    {
        body.gravityScale = 0;

        accelerationRate = maxSpeed / accelerationTime;
        decelerationRate = maxSpeed / decelerationTime;

        gravity = -2 * apexHeight / (apexTime * apexTime);
        initialJumpSpeed = 2 * apexHeight / apexTime;

        timer = 0.0f;

        playerSprite.color = Color.white;

        canDashAgain = true;
    }

    public void Update()
    {
        previousState = currentState;

        CheckForGround();

        Vector2 playerInput = new Vector2();
        playerInput.x = Input.GetAxisRaw("Horizontal");

        if (isDead)
        {
            currentState = PlayerState.dead;
        }

        switch(currentState)
        {
            case PlayerState.dead:
                // do nothing - we ded.
                break;
            case PlayerState.idle:
                if (!isGrounded) currentState = PlayerState.jumping;
                else if (velocity.x != 0) currentState = PlayerState.walking;
                break;
            case PlayerState.walking:
                if (!isGrounded) currentState = PlayerState.jumping;
                else if (velocity.x == 0) currentState = PlayerState.idle;
                break;
            case PlayerState.jumping:
                if (isGrounded)
                {
                    if (velocity.x != 0) currentState = PlayerState.walking;
                    else currentState = PlayerState.idle;
                }
                break;
        }

        MovementUpdate(playerInput);
        JumpUpdate();

        if (!isGrounded)
            velocity.y += gravity * Time.deltaTime;
        else
            velocity.y = 0;

        body.velocity = velocity;
    }

    private void MovementUpdate(Vector2 playerInput)
    {
        if (playerInput.x < 0)
            currentDirection = PlayerDirection.left;
        else if (playerInput.x > 0)
            currentDirection = PlayerDirection.right;

        if (playerInput.x != 0)
        {
            velocity.x += accelerationRate * playerInput.x * Time.deltaTime;
            velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
        }
        else
        {
            if (velocity.x > 0)
            {
                velocity.x -= decelerationRate * Time.deltaTime;
                velocity.x = Mathf.Max(velocity.x, 0);
            }
            else if (velocity.x < 0)
            {
                velocity.x += decelerationRate * Time.deltaTime;
                velocity.x = Mathf.Min(velocity.x, 0);
            }
        }

        // Increment timer to determine if we reached dash coyote time
        timer += Time.deltaTime;

        // Make the player dash by pressing X key when they're walking
        if (Input.GetKeyDown(KeyCode.X) && velocity.x != 0 && canDashAgain)
        {
            timer = 0.0f;

            maxSpeed = 10.0f;

            // Make the player sprite less white when dashing
            playerSprite.color = new Color(0.8f, 0.8f, 0.8f);

            canDashAgain = false;
        }

        // Make the player dash again when they're walking after timer has exceeded coyote time
        else if (Input.GetKeyDown(KeyCode.X) && timer >= dashCoyoteTime && velocity.x != 0 && canDashAgain)
        {
            timer = 0.0f;

            maxSpeed = 10.0f;

            // Make the player sprite less white when dashing
            playerSprite.color = new Color(0.8f, 0.8f, 0.8f);
        }

        // Change the max speed back to default after the time has exceeded coyote time
        if (timer >= dashCoyoteTime)
        {
            canDashAgain = true;

            maxSpeed = 5.0f;

            // Make the player sprite white again when the player isn't dashing anymore
            playerSprite.color = Color.white;
        }
    }

    private void JumpUpdate()
    {
        if (isGrounded && Input.GetButton("Jump"))
        {
            velocity.y = initialJumpSpeed;
            isGrounded = false;
        }
    }

    private void CheckForGround()
    {
        isGrounded = Physics2D.OverlapBox(
            transform.position + Vector3.down * groundCheckOffset,
            groundCheckSize,
            0,
            groundCheckMask);
    }

    public void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position + Vector3.down * groundCheckOffset, groundCheckSize);
    }

    public bool IsWalking()
    {
        return velocity.x != 0;
    }
    public bool IsGrounded()
    {
        return isGrounded;
    }

    public PlayerDirection GetFacingDirection()
    {
        return currentDirection;
    }
}
