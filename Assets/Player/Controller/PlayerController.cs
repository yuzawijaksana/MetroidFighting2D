using System.Collections;
using System.Linq;
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

    [Header("Key Bindings")]
    public KeyCode lightAttackKey = KeyCode.J;
    public KeyCode heavyAttackKey = KeyCode.K;
    public KeyCode dashKey = KeyCode.L;

    [Header("Ground Check Settings")]
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public LayerMask whatIsGround;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float deceleration = 0.99f;
    [SerializeField] private float fallingSpeed = 10f;
    [SerializeField] private float maxFallSpeed = -15f;

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

    [Header("Device Settings")]
    [SerializeField] private InputDevice assignedKeyboard;

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
    public bool isFacingRight = true;
    private bool isAttackLocked = false;

    public bool IsAttackLocked => isAttackLocked;

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

        // Assign a specific keyboard based on the control scheme
        if (assignedKeyboard == null && controlScheme != "None")
        {
            assignedKeyboard = InputSystem.devices.FirstOrDefault(device => device.name == controlScheme);
            if (assignedKeyboard != null)
            {
                Debug.Log($"Assigned {assignedKeyboard.name} to {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"No keyboard found for control scheme '{controlScheme}'.");
            }
        }
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

        if ((assignedKeyboard as Keyboard)?.rKey.wasPressedThisFrame == true) ResetPosition();

        isGrounded = Grounded();

        float vertical = (assignedKeyboard as Keyboard)?.wKey.isPressed == true ? 1 : ((assignedKeyboard as Keyboard)?.sKey.isPressed == true ? -1 : 0);
        float horizontal = (assignedKeyboard as Keyboard)?.aKey.isPressed == true ? -1 : ((assignedKeyboard as Keyboard)?.dKey.isPressed == true ? 1 : 0);

        if (controlledGameObject != null)
        {
            PlayerAttack controlledPlayerAttack = controlledGameObject.GetComponent<PlayerAttack>();
            if (controlledPlayerAttack != null)
            {
                if ((assignedKeyboard as Keyboard)?.jKey.wasPressedThisFrame == true)
                {
                    controlledPlayerAttack.HandleAttack(isGrounded, vertical, horizontal, true);
                }
                else if ((assignedKeyboard as Keyboard)?.kKey.wasPressedThisFrame == true)
                {
                    controlledPlayerAttack.HandleAttack(isGrounded, vertical, horizontal, false);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if ((assignedKeyboard as Keyboard)?.sKey.isPressed == true)
        {
            // Apply controlled falling force when S key is pressed
            rb.AddForce(new Vector2(0, -fallingSpeed), ForceMode2D.Force);
        }
        else
        {
            // Reset to normal falling speed when S key is not pressed
            if (rb.linearVelocity.y < maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
            }
        }

        // Reduce horizontal momentum when no movement keys are pressed or when not grounded
        if (((assignedKeyboard as Keyboard)?.aKey.isPressed != true && (assignedKeyboard as Keyboard)?.dKey.isPressed != true) || !Grounded())
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
        if (controlScheme == "None" || assignedKeyboard == null) return;

        var keyboard = assignedKeyboard as Keyboard;

        if (controlScheme == "Keyboard1")
        {
            xAxis = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
        }
        else if (controlScheme == "Keyboard2")
        {
            xAxis = (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
        }
        else
        {
            Debug.LogWarning($"Control scheme '{controlScheme}' is not mapped to any input.");
        }
    }

    private void HandleDash()
    {
        var keyboard = assignedKeyboard as Keyboard;
        if (keyboard != null && keyboard.lKey.wasPressedThisFrame && !isDashing && Grounded())
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
            anim.SetBool("Walking", true);
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
                    anim.SetBool("Walking", false);
                }
            }
        }

        anim.SetBool("Walking", Mathf.Abs(rb.linearVelocity.x) > 0.1f && Grounded());
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
        var keyboard = assignedKeyboard as Keyboard;
        if (keyboard == null || isAttackLocked) return;

        if (Grounded())
        {
            jumpCount = 0;
            isDoubleJumping = false;
        }

        if (keyboard.spaceKey.wasPressedThisFrame && (Grounded() || jumpCount < maxJumps || isDashing))
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

        if (keyboard.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }

        if (keyboard.sKey.wasPressedThisFrame && !Grounded())
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

        var keyboard = assignedKeyboard as Keyboard;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            StartCoroutine(SmoothWallJump());
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

    private IEnumerator SmoothWallJump()
    {
        float jumpDuration = 0.15f;
        float elapsedTime = 0f;

        Vector2 initialPosition = rb.position;
        Vector2 targetPosition = initialPosition + new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);

        while (elapsedTime < jumpDuration)
        {
            rb.position = Vector2.Lerp(initialPosition, targetPosition, elapsedTime / jumpDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
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
        StartCoroutine(SpawnMultipleDashTrails()); // Spawn multiple dash trails

        float dashDuration = 0.2f;
        StartCoroutine(EndDashAfterDuration(dashDuration));
    }

    private IEnumerator SpawnMultipleDashTrails()
    {
        int trailCount = 4; // Number of trails
        float trailDelay = 0.05f; // Delay between each trail

        for (int i = 0; i < trailCount; i++)
        {
            float opacity = 1f - (i * 0.25f); // Gradually decrease opacity for each trail
            SpawnDashTrail(opacity);
            yield return new WaitForSeconds(trailDelay);
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

        StartCoroutine(FadeAndDestroyTrail(trailRenderer));
    }

    private IEnumerator FadeAndDestroyTrail(SpriteRenderer trailRenderer)
    {
        float elapsedTime = 0f;
        Color initialColor = trailRenderer.color;

        while (elapsedTime < dashTrailLifetime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(initialColor.a, 0, elapsedTime / dashTrailLifetime);
            trailRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            yield return null;
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
        var keyboard = assignedKeyboard as Keyboard;
        if (isDashing && keyboard?.spaceKey.wasPressedThisFrame == true)
        {
            isDashing = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        }
    }

    private IEnumerator EndDashAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
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
        StartCoroutine(UnlockAttackAfterDuration(duration));
    }

    private IEnumerator UnlockAttackAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttackLocked = false;
    }

    private void HandlePlatformDrop()
    {
        var keyboard = assignedKeyboard as Keyboard;
        if (keyboard != null && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame))
        {
            StartCoroutine(DisablePlatformCollision());
        }
    }

    private IEnumerator DisablePlatformCollision()
    {
        Collider2D platformCollider = Physics2D.OverlapCircle(groundCheckPoint.position, 0.2f, platformLayer);
        if (platformCollider != null)
        {
            Physics2D.IgnoreCollision(boxCollider, platformCollider, true);
            yield return new WaitForSeconds(platformDropDuration);
            Physics2D.IgnoreCollision(boxCollider, platformCollider, false);
        }
    }
}
