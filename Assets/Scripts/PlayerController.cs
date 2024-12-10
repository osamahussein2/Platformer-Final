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

    private float dashTimer;
    private float jumpTimer;

    [Header("Horizontal")]
    public float maxSpeed = 5f;
    public float accelerationTime = 0.25f;
    public float decelerationTime = 0.15f;
    public float dashCoyoteTime = 1.0f;

    public bool canDashAgain;

    [Header("Vertical")]
    public float apexHeight = 3f;
    public float apexTime = 0.5f;
    public float jumpCoyoteTime = 1.0f;

    public bool ladderTriggered;

    public bool canJumpAgain;

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

    [Header("Wall Check")]
    public bool collidedWithLeftWall;
    public bool collidedWithRightWall;
    public float wallForce;

    public void Start()
    {
        body.gravityScale = 0;

        accelerationRate = maxSpeed / accelerationTime;
        decelerationRate = maxSpeed / decelerationTime;

        gravity = -2 * apexHeight / (apexTime * apexTime);
        initialJumpSpeed = 2 * apexHeight / apexTime;

        dashTimer = 0.0f;
        jumpTimer = 0.0f;

        playerSprite.color = Color.white;

        canDashAgain = true;
        canJumpAgain = true;

        ladderTriggered = false;

        collidedWithLeftWall = false;
        collidedWithRightWall = false;

        wallForce = 1.0f;
    }

    public void Update()
    {
        previousState = currentState;

        CheckForGround();

        Vector2 playerInput = new Vector2();
        playerInput.x = Input.GetAxisRaw("Horizontal");

        // If the player didn't reach the ladder yet
        if (!ladderTriggered)
        {
            // Don't move the player vertically
            playerInput.y = 0.0f;

            // Make the player fall
            gravity = -2 * apexHeight / (apexTime * apexTime);
        }

        // If the player reaches the ladder
        else if (ladderTriggered)
        {
            // Move the player vertically
            playerInput.y = Input.GetAxisRaw("Vertical");

            // Disable gravity
            gravity = 0.0f;
        }

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
        AddWallForce();

        if (!isGrounded && !ladderTriggered || !isGrounded && ladderTriggered)
            velocity.y += gravity * Time.deltaTime;

        else if (isGrounded && !ladderTriggered)
        {
            velocity.y = 0;
            canJumpAgain = true;
        }

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
            velocity.x += accelerationRate * wallForce * playerInput.x * Time.deltaTime;
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

        // If the player input is not 0 and we're moving vertically
        if (playerInput.y != 0)
        {
            velocity.y += accelerationRate * playerInput.y * Time.deltaTime;
            velocity.y = Mathf.Clamp(velocity.y, -maxSpeed, maxSpeed);
        }

        // Increment timer to determine if we reached dash coyote time
        dashTimer += Time.deltaTime;

        // Make the player dash by pressing X key when they're walking
        if (Input.GetKeyDown(KeyCode.X) && velocity.x != 0 && canDashAgain)
        {
            dashTimer = 0.0f;

            maxSpeed = 10.0f;

            // Make the player sprite less white when dashing
            playerSprite.color = new Color(0.8f, 0.8f, 0.8f);

            canDashAgain = false;
        }

        // Change the max speed back to default after the time has exceeded coyote time
        if (dashTimer >= dashCoyoteTime)
        {
            canDashAgain = true;

            maxSpeed = 5.0f;

            // Make the player sprite white again when the player isn't dashing anymore
            playerSprite.color = Color.white;
        }
    }

    void AddWallForce()
    {
        // LEFT WALL BOOLEAN

        // Disable wall force by setting the wall force to 1
        if (!collidedWithLeftWall && !collidedWithRightWall)
        {
            wallForce = 1.0f;
        }

        // Enable left wall force by setting the wall force equal to a negative number
        else if (collidedWithLeftWall && !collidedWithRightWall)
        {
            wallForce = -20.0f;
        }

        // RIGHT WALL BOOLEAN

        // Disable wall force by setting the wall force to 1
        if (!collidedWithRightWall && !collidedWithLeftWall)
        {
            wallForce = 1.0f;
        }

        // Enable right wall force by setting the wall force equal to a negative number
        else if (collidedWithRightWall && !collidedWithLeftWall)
        {
            wallForce = -20.0f;
        }
    }

    private void JumpUpdate()
    {
        jumpTimer += Time.deltaTime;

        if (isGrounded && Input.GetButton("Jump"))
        {
            velocity.y = initialJumpSpeed;
            isGrounded = false;

            jumpTimer = 0.0f;

            canJumpAgain = true;
        }

        else if (!isGrounded && Input.GetButton("Jump") && jumpTimer >= jumpCoyoteTime && canJumpAgain)
        {
            velocity.y = initialJumpSpeed;

            jumpTimer = 0.0f;

            canJumpAgain = false;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name == "LeftWallTilemap")
        {
            // If the player hits the left wall, add force to it that pushes the player away from the left wall
            collidedWithLeftWall = true;
        }

        else if (collision.gameObject.name == "RightWallTilemap")
        {
            // If the player hits the right wall, add force to it that pushes the player away from the right wall
            collidedWithRightWall = true;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.name == "LeftWallTilemap")
        {
            // If the player is still colliding with the left wall, add force to it that pushes the player away from the left wall
            collidedWithLeftWall = true;
        }

        else if (collision.gameObject.name == "RightWallTilemap")
        {
            // If the player is still colliding with the right wall, add force to it that pushes the player away from the right wall
            collidedWithRightWall = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.name == "LeftWallTilemap")
        {
            // If the player doesn't hit the left wall anymore, disable wall force by setting collided with left wall boolean to false
            collidedWithLeftWall = false;
        }

        else if (collision.gameObject.name == "RightWallTilemap")
        {
            // If the player doesn't hit the right wall anymore, disable wall force by setting collided with right wall boolean to false
            collidedWithRightWall = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.name == "LadderTilemap")
        {
            // If the player on the ladder, set ladder triggered to true
            ladderTriggered = true;
        }
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (collider.gameObject.name == "LadderTilemap")
        {
            // If the player on the ladder, set ladder triggered to true
            ladderTriggered = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject.name == "LadderTilemap")
        {
            // If the player exits the ladder trigger, set ladder triggered to false
            ladderTriggered = false;
        }
    }
}
