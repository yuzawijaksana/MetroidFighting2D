using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("Control Settings")]
    [SerializeField] public string controlScheme = "None";
    [SerializeField] public bool isControllable;

    [Header("Player Settings")]
    [SerializeField] private GameObject controlledGameObject;
    [SerializeField] public GameObject hurtbox;
    [SerializeField] public GameObject parentCollider; // Reference to the player's parent collider

    [Header("Key Bindings")]
    public Key lightAttackKey = Key.J;
    public Key heavyAttackKey = Key.K;
    public Key dashKey = Key.L;

    [Header("Ground Check Settings")]
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public LayerMask whatIsGround;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float deceleration = 0.99f;
    [SerializeField] private float maxFallSpeed = -15f;

    [Header("Falling Settings")]
    [SerializeField] private float fallingSpeed = 10f;
    [SerializeField] private float fastFallSpeed = -20f; // Maximum falling speed when pressing down

    [Header("Sliding Settings")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallSlidingSpeed = 2f;
    [SerializeField] private LayerMask wallInteractionLayers;
    [SerializeField] private float wallCheckRadius = 0.2f;
    private bool isWallSliding;
    private Vector2 originalColliderSize;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 10;
    [SerializeField] private GameObject jumpSmokePrefab;
    [SerializeField] private float jumpSmokeLifetime = 0.5f;
    [SerializeField] private int maxJumps = 2;
    private int jumpCount;
    private bool isDoubleJumping;

    [Header("Wall Jumping Settings")]
    [SerializeField] private bool isWallJumping;
    [SerializeField] private float wallJumpingDirection;
    [SerializeField] private float wallJumpingTime = 0.2f;
    [SerializeField] private float wallJumpingCounter;
    [SerializeField] private float wallJumpingDuration = 0.4f;
    [SerializeField] private Vector2 wallJumpingPower = new Vector2(1f, 1f);

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private GameObject dashSmokePrefab;
    [SerializeField] private float dashSmokeLifetime = 0.5f;

    [Header("Dash Trail Settings")]
    [SerializeField] private float dashTrailLifetime = 0.5f;
    [SerializeField] private float dashTrailFadeSpeed = 2f;

    private bool isDashing;

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowMotionFactor = 0.5f;
    private bool isSlowMotionActive = false;

    [Header("Platform Settings")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float platformDropDuration = 0.3f;

    // References
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator anim;
    private PlayerAttack playerAttack;
    private ICharacterBehavior characterBehavior;
    private CinemachineCamera virtualCamera;

    // State Variables
    private bool isGrounded;
    public float xAxis;
    public float yAxis { get; private set; } // Add yAxis property
    public bool isFacingRight = true;
    private bool isAttackLocked = false;

    public bool IsAttackLocked => isAttackLocked;

    // Add a new state variable to track if the character is recovering from a hit
    private bool isRecoveringFromHit = false;

    private void Start()
    {
        if (controlledGameObject != null)
        {
            rb = controlledGameObject.GetComponent<Rigidbody2D>();
        }
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
        characterBehavior = GetComponent<ICharacterBehavior>();
        originalColliderSize = boxCollider.size;
    }

    private void Update()
    {
        if (!isControllable) return;

        if (isAttackLocked)
        {
            // Allow natural physics (e.g., falling) while locking player input
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
            return;
        }

        GetInputs();
        HandlePlatformDrop();
        if (!isDashing)
        {
            Move();
            WallSlide();
            Jump();
            WallJump();
            if (!isWallJumping) Flip();
        }
        else
        {
            JumpWhileDashing();
        }
        HandleDash();

        if (!Grounded())
        {
            characterBehavior?.ShrinkColliderForJump();
        }
        else
        {
            characterBehavior?.RestoreCollider();
        }

        if (isWallSliding)
        {
            characterBehavior?.ShrinkColliderForWallSlide();
            anim.SetBool("Jumping", false); // Disable Jumping when sliding
            anim.SetBool("Falling", false); // Disable Falling when sliding
        }
        else if (!Grounded() && rb.linearVelocity.y < 0)
        {
            anim.SetBool("Falling", true);
            anim.SetBool("Jumping", false); // Disable Jumping when falling
        }
        else
        {
            anim.SetBool("Falling", false);
        }

        if (Keyboard.current.rKey.wasPressedThisFrame) ResetPosition();

        isGrounded = Grounded();

        float vertical = Keyboard.current.wKey.isPressed ? 1 : (Keyboard.current.sKey.isPressed ? -1 : 0);
        float horizontal = Keyboard.current.aKey.isPressed ? -1 : (Keyboard.current.dKey.isPressed ? 1 : 0);

        if (controlledGameObject != null && !isWallSliding) // Prevent attacks while wall sliding
        {
            PlayerAttack controlledPlayerAttack = controlledGameObject.GetComponent<PlayerAttack>();
            if (controlledPlayerAttack != null)
            {
                // Use serialized keys for light and heavy attacks
                if (Keyboard.current[lightAttackKey].wasPressedThisFrame)
                {
                    controlledPlayerAttack.HandleAttack(isGrounded, vertical, horizontal, true);
                }
                else if (Keyboard.current[heavyAttackKey].wasPressedThisFrame)
                {
                    controlledPlayerAttack.HandleAttack(isGrounded, vertical, horizontal, false);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isAttackLocked && Keyboard.current.sKey.isPressed) // Only allow fast fall when not attacking
        {
            // Apply controlled falling force when S key is pressed
            rb.AddForce(new Vector2(0, -fallingSpeed), ForceMode2D.Force);

            // Limit the falling speed to the fast fall maximum
            if (rb.linearVelocity.y < fastFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, fastFallSpeed);
            }
        }
        else
        {
            // Reset to normal falling speed when S key is not pressed
            if (rb.linearVelocity.y < maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
            }
        }

        // Only apply deceleration if dashing and not recovering from a hit
        if (isDashing && !isRecoveringFromHit)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * deceleration, rb.linearVelocity.y);
            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop horizontal movement completely
            }
        }
    }

    private void GetInputs()
    {
        if (controlScheme == "None") return;

        if (controlScheme == "Keyboard1" || controlScheme == "Keyboard")
        {
            xAxis = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
            yAxis = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0); // Update yAxis
        }
        else if (controlScheme == "Keyboard2")
        {
            xAxis = (Keyboard.current.rightArrowKey.isPressed ? 1 : 0) - (Keyboard.current.leftArrowKey.isPressed ? 1 : 0);
            yAxis = (Keyboard.current.upArrowKey.isPressed ? 1 : 0) - (Keyboard.current.downArrowKey.isPressed ? 1 : 0); // Update yAxis
        }
        else
        {
            Debug.LogWarning($"Control scheme '{controlScheme}' is not mapped to any input.");
        }
    }

    private void HandleDash()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame && !isDashing && Grounded())
        {
            Dash();
        }
    }

    private void Move()
    {
        if (controlledGameObject == null) return;

        if (xAxis != 0)
        {
            float targetSpeed = walkSpeed * xAxis;
            float acceleration = 10f;

            Rigidbody2D controlledRb = controlledGameObject.GetComponent<Rigidbody2D>();
            if (controlledRb != null)
            {
                controlledRb.linearVelocity = new Vector2(Mathf.Lerp(controlledRb.linearVelocity.x, targetSpeed, Time.deltaTime * acceleration), controlledRb.linearVelocity.y);
            }

            // Enable walking animation only if grounded
            anim.SetBool("Walking", Grounded());
        }
        else
        {
            Rigidbody2D controlledRb = controlledGameObject.GetComponent<Rigidbody2D>();
            if (controlledRb != null)
            {
                controlledRb.linearVelocity = new Vector2(controlledRb.linearVelocity.x * deceleration, controlledRb.linearVelocity.y);
                if (Mathf.Abs(controlledRb.linearVelocity.x) < 0.1f)
                {
                    controlledRb.linearVelocity = new Vector2(0, controlledRb.linearVelocity.y);
                }
            }
            anim.SetBool("Walking", false); // Disable walking animation when no horizontal keys are pressed
        }
    }

    private void Flip()
    {
        if ((xAxis < 0 && isFacingRight) || (xAxis > 0 && !isFacingRight))
        {
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
            isFacingRight = !isFacingRight;
        }
    }

    public bool Grounded()
    {
        float groundCheckRadius = 0.2f;
        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, whatIsGround);
    }

    private bool IsTouchingWall()
    {
        return Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallInteractionLayers);
    }

    private void WallSlide()
    {
        if (IsTouchingWall() && !Grounded())
        {
            isWallSliding = true;
            jumpCount = 0; // Reset the jump count when touching the wall
            characterBehavior?.ShrinkColliderForWallSlide();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));
            anim.SetBool("Sliding", isWallSliding);
        }
        else
        {
            isWallSliding = false;
            characterBehavior?.RestoreCollider();
            anim.SetBool("Sliding", isWallSliding);
        }
    }

    private void Jump()
    {
        if (isAttackLocked) return;

        if (Grounded())
        {
            jumpCount = 0;
            isDoubleJumping = false;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && (Grounded() || jumpCount < maxJumps || isDashing))
        {
            characterBehavior?.ShrinkColliderForJump();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            jumpCount++;

            // Set Jumping animation to true when space is pressed
            anim.SetBool("Jumping", true);

            if (!Grounded() && jumpCount == 2)
            {
                isDoubleJumping = true;
                anim.SetBool("DoubleJumping", true);
            }

            if (isDashing) isDashing = false;
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }

        if (Keyboard.current.sKey.wasPressedThisFrame && !Grounded())
        {
            rb.AddForce(new Vector2(0, -fallingSpeed), ForceMode2D.Impulse);
        }

        // Reset Jumping animation when grounded
        anim.SetBool("Jumping", !Grounded());
        anim.SetBool("DoubleJumping", isDoubleJumping && !Grounded());
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            SmoothWallJump();
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                transform.localScale = new Vector2(wallJumpingDirection, transform.localScale.y);
            }

            jumpCount = 1;
            isDoubleJumping = false;

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private async void SmoothWallJump()
    {
        float jumpDuration = 0.15f;
        float elapsedTime = 0f;

        Vector2 initialPosition = rb.position;
        Vector2 targetPosition = initialPosition + new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);

        while (elapsedTime < jumpDuration)
        {
            rb.position = Vector2.Lerp(initialPosition, targetPosition, elapsedTime / jumpDuration);
            elapsedTime += Time.deltaTime;
            await Task.Yield();
        }

        rb.position = targetPosition;
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void Dash()
    {
        isDashing = true;

        float dashForce = dashSpeed;

        if (Grounded() && rb.linearVelocity.y == 0)
        {
            rb.linearVelocity = new Vector2(isFacingRight ? dashForce : -dashForce, 0);
        }
        else
        {
            rb.linearVelocity = new Vector2(isFacingRight ? dashForce : -dashForce, rb.linearVelocity.y);
        }

        SpawnDashSmoke();
        SpawnMultipleDashTrails(); // Spawn multiple dash trails

        float dashDuration = 0.2f;
        EndDashAfterDuration(dashDuration);
    }

    private async void SpawnMultipleDashTrails()
    {
        int trailCount = 4; // Number of trails
        float trailDelay = 0.05f; // Delay between each trail

        for (int i = 0; i < trailCount; i++)
        {
            float opacity = 1f - (i * 0.25f); // Gradually decrease opacity for each trail
            SpawnDashTrail(opacity);
            await Task.Delay((int)(trailDelay * 1000)); // Convert seconds to milliseconds
        }
    }

    private void SpawnDashTrail(float opacity)
    {
        GameObject trail = new GameObject("DashTrail");
        SpriteRenderer trailRenderer = trail.AddComponent<SpriteRenderer>();
        trailRenderer.sprite = GetComponent<SpriteRenderer>().sprite;
        trailRenderer.color = new Color(1f, 1f, 1f, opacity); // Recolor with varying opacity
        trail.transform.position = transform.position;
        trail.transform.localScale = transform.localScale;

        FadeAndDestroyTrail(trailRenderer);
    }

    private async void FadeAndDestroyTrail(SpriteRenderer trailRenderer)
    {
        float elapsedTime = 0f;
        Color initialColor = trailRenderer.color;

        while (elapsedTime < dashTrailLifetime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(initialColor.a, 0, elapsedTime / dashTrailLifetime);
            trailRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            await Task.Yield();
        }

        Destroy(trailRenderer.gameObject);
    }

    private void SpawnDashSmoke()
    {
        if (dashSmokePrefab != null)
        {
            GameObject smoke = Instantiate(dashSmokePrefab, transform.position, Quaternion.identity);
            Destroy(smoke, dashSmokeLifetime);
        }
    }

    private void JumpWhileDashing()
    {
        if (isDashing && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isDashing = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        }
    }

    private async void EndDashAfterDuration(float duration)
    {
        await Task.Delay((int)(duration * 1000)); // Convert seconds to milliseconds
        isDashing = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.8f, rb.linearVelocity.y);
    }

    private void ToggleSlowMotion()
    {
        if (isSlowMotionActive)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            isSlowMotionActive = false;
        }
        else
        {
            Time.timeScale = slowMotionFactor;
            Time.fixedDeltaTime = Time.timeScale * 0.02f;
            isSlowMotionActive = true;
        }
    }

    private void ResetPosition()
    {
        transform.position = Vector3.zero;
    }

    public void LockAttack(float duration)
    {
        isAttackLocked = true;
        UnlockAttackAfterDuration(duration);
    }

    private async void UnlockAttackAfterDuration(float duration)
    {
        await Task.Delay((int)(duration * 1000)); // Convert seconds to milliseconds
        isAttackLocked = false;
    }

    private async void HandlePlatformDrop()
    {
        if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            await DisablePlatformCollision();
        }
    }

    private async Task DisablePlatformCollision()
    {
        Collider2D platformCollider = Physics2D.OverlapCircle(groundCheckPoint.position, 0.2f, platformLayer);
        if (platformCollider != null)
        {
            Physics2D.IgnoreCollision(boxCollider, platformCollider, true);
            await Task.Delay((int)(platformDropDuration * 1000)); // Convert seconds to milliseconds
            Physics2D.IgnoreCollision(boxCollider, platformCollider, false);
        }
    }

    public void ApplyHitRecovery()
    {
        isRecoveringFromHit = true;

        // Wait until the character touches the ground, then slide
        WaitForGroundAndSlide();
    }

    private async void WaitForGroundAndSlide()
    {
        // Wait until the character is grounded
        while (!Grounded())
        {
            await Task.Yield(); // Wait for the next frame
        }

        // Once grounded, apply the sliding effect
        await SlideToStop();
    }

    private async Task SlideToStop()
    {
        float slideDuration = 0.2f; // Duration of the sliding effect
        float elapsedTime = 0f;

        Vector2 initialVelocity = rb.linearVelocity;

        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;

            // Gradually reduce velocity over time
            rb.linearVelocity = Vector2.Lerp(initialVelocity, Vector2.zero, elapsedTime / slideDuration);

            await Task.Yield();
        }

        // Ensure velocity is fully stopped at the end
        rb.linearVelocity = Vector2.zero;
        isRecoveringFromHit = false;
    }

    public bool IsGroundPoundKeyHeld()
    {
        return Keyboard.current.sKey.isPressed; // Check if the 'S' key is still being held
    }

    /// <summary>
    /// Checks if a specific key is currently being held.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is held, otherwise false.</returns>
    public bool IsKeyHeld(Key key)
    {
        return Keyboard.current[key].isPressed;
    }
}
