using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Tilemaps;

public enum ControlScheme
{
    None,
    Keyboard1,
    Keyboard2
}

public class PlayerController : MonoBehaviour
{
    [Header("Control Settings")]
    [SerializeField, Tooltip("Select the control scheme for this player.")]
    public ControlScheme controlScheme = ControlScheme.None;
    [SerializeField] public bool isControllable;

    [Header("Player Settings")]
    [SerializeField] private GameObject controlledGameObject;
    [SerializeField] public GameObject hurtbox;
    [SerializeField] public GameObject parentCollider; // Reference to the player's parent collider

    private GameInputs controls;

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

    [Header("Dash Duration & End Multiplier")]
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashMultiplier = 0.8f;

    private bool isDashing;

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowMotionFactor = 0.5f;
    private bool isSlowMotionActive = false;

    [Header("Platform Settings")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float platformDropDuration = 0.3f;

    [Header("Recovery Settings")]
    [SerializeField] private int maxFreeRecovery = 1; // 1 free recovery
    private int freeRecoveryUsed = 0;

    [Header("Attack Settings")]
    [SerializeField] private bool attackUsesRootMotion = false; // Determines if root motion is used for attacks
    [SerializeField] private float attackDuration = 0.5f; // Default attack duration
    [SerializeField] private float attackSpeed = 5f; // Default attack movement speed

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
    private bool wasFalling = false; // Track if player was falling in previous frame

    public bool IsAttackLocked => isAttackLocked;

    // Add a new state variable to track if the character is recovering from a hit
    private bool isRecoveringFromHit = false;
    
    // Input buffering for controller support
    private float jumpBufferTime = 0.15f;
    private float jumpBufferCounter = 0f;
    private float dashBufferTime = 0.15f;
    private float dashBufferCounter = 0f;

    private void Awake()
    {
        controls = new GameInputs();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

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
        if (!isControllable || controlScheme == ControlScheme.None) return; // Prevent all actions if not controllable or control scheme is None

        // Prevent all input during dialog
        if (StoryDialogTrigger.IsAnyDialogActive)
        {
            // Slow down velocity gradually
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x * 0.9999f, // Slowly decelerate horizontal movement
                    rb.linearVelocity.y
                );
            }
            
            // Stop animations but keep physics active
            if (anim != null)
            {
                anim.SetBool("Walking", false);
                anim.SetBool("Falling", false);
                anim.SetBool("Jumping", false);
                anim.SetBool("Sliding", false);
                anim.SetBool("DoubleJumping", false);
                anim.SetBool("Idle", true);
            }
            wasFalling = false; // Reset falling state during dialog
            return;
        }
        
        // Prevent input during cooldown after dialog
        if (StoryDialogTrigger.InputCooldownTimer > 0) return;

        // Buffer jump input
        if (controls.Player.Jump.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        
        // Buffer dash input
        if (controls.Player.Dash.WasPressedThisFrame())
        {
            dashBufferCounter = dashBufferTime;
        }
        else
        {
            dashBufferCounter -= Time.deltaTime;
        }

        // Prevent all input if attack is locked (including jump, dash, etc.)
        if (isAttackLocked)
        {
            // Allow natural physics (e.g., falling) while locking player input
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);

            // Unlock attack if animation is done
            if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            {
                isAttackLocked = false;
            }
            return;
        }

        GetInputs();
        HandlePlatformDrop();
        if (!isDashing)
        {
            Move();
            WallSlide();
            // Prevent jumping while attacking
            if (!isAttackLocked)
            {
                Jump();
                WallJump();
            }
            if (!isWallJumping) Flip(); // Only flip if not wall jumping
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
            wasFalling = false;
        }
        else if (!Grounded() && rb.linearVelocity.y < 0)
        {
            anim.SetBool("Falling", true);
            anim.SetBool("Jumping", false); // Disable Jumping when falling
            wasFalling = true;
        }
        else if (Grounded() && wasFalling)
        {
            // Just landed after falling - trigger landing animation
            anim.SetTrigger("Landing");
            anim.SetBool("Falling", false);
            wasFalling = false;
        }
        else
        {
            anim.SetBool("Falling", false);
            wasFalling = false;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame) ResetPosition();

        isGrounded = Grounded();

        float vertical = yAxis;
        float horizontal = xAxis;
        
        // Apply threshold for attack direction to prevent accidental neutral attacks on gamepad
        float directionThreshold = 0.3f;
        float attackVertical = Mathf.Abs(vertical) > directionThreshold ? vertical : 0f;
        float attackHorizontal = Mathf.Abs(horizontal) > directionThreshold ? horizontal : 0f;

        if (controlledGameObject != null && !isWallSliding) // Prevent attacks while wall sliding
        {
            PlayerAttack controlledPlayerAttack = controlledGameObject.GetComponent<PlayerAttack>();
            if (controlledPlayerAttack != null)
            {
                // Light Attack
                if (controls.Player.Attack.WasPressedThisFrame() && !isAttackLocked)
                {
                    if (controlledPlayerAttack.HandleAttack(isGrounded, attackVertical, attackHorizontal, true))
                    {
                        isAttackLocked = true;
                    }
                }
                // Heavy Attack / Special Attack
                else if (controls.Player.SpecialAttack.WasPressedThisFrame() && !isAttackLocked)
                {
                    // Perform recovery if jumps are available (example for heavy/special)
                    if (!isGrounded && jumpCount > 0) 
                    {
                        if (controlledPlayerAttack.HandleAttack(isGrounded, attackVertical, attackHorizontal, false))
                        {
                            jumpCount--; // Decrement jump count for recovery
                            isAttackLocked = true;
                        }
                    }
                }
            }
        }
        bool isIdle = 
            Grounded() &&
            Mathf.Abs(rb.linearVelocity.x) < 0.1f &&
            !anim.GetBool("Jumping") &&
            !anim.GetBool("Falling") &&
            !anim.GetBool("Sliding") &&
            !isAttackLocked &&
            !isDashing;

        anim.SetBool("Idle", isIdle);
    }

    private void FixedUpdate()
    {
    if (!isAttackLocked && yAxis < -0.5f) // Only allow fast fall when not attacking
    {
        // Apply controlled falling force when moving down
        rb.AddForce(new Vector2(0, -fallingSpeed), ForceMode2D.Force);

        // Limit the falling speed to the fast fall maximum
        if (rb.linearVelocity.y < fastFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fastFallSpeed);
        }
    }
    else
    {
        // Reset to normal falling speed
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
        if (controlScheme == ControlScheme.None) return;

        // Get movement from Input Actions
        Vector2 movement = controls.Player.Movement.ReadValue<Vector2>();
        xAxis = movement.x;
        yAxis = movement.y;
    }

    private void HandleDash()
    {
        if (dashBufferCounter > 0 && !isDashing && Grounded())
        {
            dashBufferCounter = 0; // Consume the buffered input
            Dash();
        }
    }

    private void Move()
    {
        if (controlledGameObject == null) return;

        Rigidbody2D controlledRb = controlledGameObject.GetComponent<Rigidbody2D>();
        if (controlledRb == null) return;

        float targetSpeed = walkSpeed * xAxis; // Target speed based on input
        float acceleration = Grounded() ? 25f : 15f; // Faster acceleration on the ground
        float deceleration = Grounded() ? 20f : 10f; // Faster deceleration on the ground

        // Gradually accelerate or decelerate based on input
        if (xAxis != 0)
        {
            controlledRb.linearVelocity = new Vector2(
                Mathf.MoveTowards(controlledRb.linearVelocity.x, targetSpeed, acceleration * Time.deltaTime),
                controlledRb.linearVelocity.y
            );
        }
        else
        {
            // Decelerate to stop when no input is provided
            controlledRb.linearVelocity = new Vector2(
                Mathf.MoveTowards(controlledRb.linearVelocity.x, 0, deceleration * Time.deltaTime),
                controlledRb.linearVelocity.y
            );
        }

        // Enable walking animation only if grounded and moving
        anim.SetBool("Walking", Grounded() && Mathf.Abs(controlledRb.linearVelocity.x) > 0.1f);
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
            isWallJumping = false; // <-- Reset wall jump state when touching wall again
            jumpCount = 3; // Reset the jump count when touching the wall
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
            jumpCount = maxJumps; // Reset jump count when grounded
            isDoubleJumping = false;
        }

        bool jumpPressed = jumpBufferCounter > 0;

        if (jumpPressed && jumpCount > 0)
        {
            jumpBufferCounter = 0; // Consume the buffered input
            characterBehavior?.ShrinkColliderForJump();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            jumpCount--; // Decrement jump count

            // Set Jumping animation to true when jump is pressed
            anim.SetBool("Jumping", true);

            if (!Grounded() && jumpCount == 0)
            {
                isDoubleJumping = true;
                anim.SetBool("DoubleJumping", true);
            }

            if (isDashing) isDashing = false;
        }

        // Jump release logic
        bool jumpReleased = controls.Player.Jump.WasReleasedThisFrame();

        if (jumpReleased && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }

        if (yAxis < -0.5f && !Grounded())
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

        bool jumpPressed = jumpBufferCounter > 0;

        if (jumpPressed && wallJumpingCounter > 0f)
        {
            jumpBufferCounter = 0; // Consume the buffered input
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

            // Flip towards the wall if movement input is towards the wall after walljump
            if (xAxis != 0 && Mathf.Sign(xAxis) != Mathf.Sign(wallJumpingDirection))
            {
                isFacingRight = !isFacingRight;
                transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private async void SmoothWallJump()
    {
        float jumpDuration = 0.15f;
        float elapsedTime = 0f;

        Vector2 initialPosition = rb.position;
        Vector2 targetPosition = initialPosition + new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);

        // Preserve upward velocity if it's greater than the target's y velocity
        float currentYVelocity = rb.linearVelocity.y;
        float targetYVelocity = wallJumpingPower.y / jumpDuration;
        float finalYVelocity = Mathf.Max(currentYVelocity, targetYVelocity);

        while (elapsedTime < jumpDuration)
        {
            float t = elapsedTime / jumpDuration;
            Vector2 lerped = Vector2.Lerp(initialPosition, targetPosition, t);
            // Maintain the higher upward velocity if present
            rb.position = new Vector2(lerped.x, lerped.y);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, finalYVelocity);
            elapsedTime += Time.deltaTime;
            await Task.Yield();
        }

        rb.position = targetPosition;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, finalYVelocity);
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
            // Lock vertical velocity during dash
            rb.linearVelocity = new Vector2(isFacingRight ? dashForce : -dashForce, 0);
        }

        SpawnDashSmoke();
        SpawnMultipleDashTrails(); // Spawn multiple dash trails

        DashDuration(dashDuration);
    }

    private async void DashDuration(float duration)
    {
        await Task.Delay((int)(duration * 1000)); // Convert seconds to milliseconds
        isDashing = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * dashMultiplier, rb.linearVelocity.y);
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
        bool jumpPressed = jumpBufferCounter > 0;

        if (isDashing && jumpPressed)
        {
            jumpBufferCounter = 0; // Consume the buffered input
            isDashing = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            anim.SetBool("Jumping", true);
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

        // Ensure the player's velocity is reset after unlocking
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private async void HandlePlatformDrop()
    {
        bool dropPressed = false;
        bool attackInputActive = false;
        if (controlScheme == ControlScheme.Keyboard1)
        {
            dropPressed = Keyboard.current.sKey.wasPressedThisFrame;
            // Check if attack input is active (light or heavy attack)
            attackInputActive = Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.kKey.wasPressedThisFrame;
        }
        else if (controlScheme == ControlScheme.Keyboard2)
        {
            dropPressed = Keyboard.current.downArrowKey.wasPressedThisFrame;
            attackInputActive = Keyboard.current.numpad4Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame;
        }

        // Only allow platform drop if NOT attacking this frame
        if (dropPressed && !attackInputActive)
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
    
    public void HandleRespawning(bool isRespawning)
    {
        if (hurtbox != null) hurtbox.SetActive(!isRespawning);
        if (isRespawning && rb != null) rb.linearVelocity = Vector2.zero;   
    }

    // Sets whether the player is controllable
    public void SetControllable(bool state)
    {
        isControllable = state;
    }

    private void PerformAttack()
    {
        rb.linearVelocity = new Vector2(isFacingRight ? attackSpeed : -attackSpeed, rb.linearVelocity.y);
    }

    private IEnumerator DisableRootMotionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.applyRootMotion = false; // Disable root motion after the attack
    }
}
