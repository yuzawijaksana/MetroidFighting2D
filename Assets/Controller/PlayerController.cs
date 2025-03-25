using System.Collections;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Control Settings")]
    [SerializeField] public string controlScheme = "None"; // Dropdown for control scheme (e.g., Keyboard1, Keyboard2, None)
    [SerializeField] public bool isControllable = true; // Dropdown to enable/disable control

    // References
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator anim;
    private PlayerAttack playerAttack;
    private ICharacterBehavior characterBehavior;

    
    [Header("Player Settings")]
    [SerializeField] private GameObject controlledGameObject; // Reference to the GameObject to move
    
    // Key Bindings
    [Header("Key Bindings")]
    public KeyCode lightAttackKey = KeyCode.J;
    public KeyCode heavyAttackKey = KeyCode.K;
    public KeyCode dashKey = KeyCode.L;

    // Ground Check Settings
    [Header("Ground Check Settings")]
    [SerializeField] private float jumpForce = 45;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask whatIsGround;

    // Movement Settings
    [SerializeField] private float walkSpeed = 1.0f;
    [SerializeField] private float deceleration = 0.95f;
    [SerializeField] private float fallingSpeed = 10f;
    [SerializeField] private float maxFallSpeed = -20f;
    
    // Sliding Settings
    [Header("Sliding Settings")]
    private bool isWallSliding;
    [SerializeField] private float wallSlidingSpeed = 2f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    private Vector2 originalColliderSize;

    // Jump Smoke Settings
    [Header("Jump Settings")]
    [SerializeField] private GameObject jumpSmokePrefab; // Prefab for the jump smoke effect
    [SerializeField] private float jumpSmokeLifetime = 0.5f; // Lifetime of the jump smoke effect

    // Wall Jumping Settings
    [Header("Wall Jumping Settings")]
    [SerializeField] private bool isWallJumping;
    [SerializeField] private float wallJumpingDirection;
    [SerializeField] private float wallJumpingTime = 0.2f;
    [SerializeField] private float wallJumpingCounter;
    [SerializeField] private float wallJumpingDuration = 0.4f;
    [SerializeField] private Vector2 wallJumpingPower = new Vector2(4f, 8f);

    // Dash Settings
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private GameObject dashSmokePrefab; // Prefab for the dash smoke effect
    [SerializeField] private float dashSmokeLifetime = 0.5f; // Lifetime of the dash smoke effect
    private bool isDashing;


    // Double Jump Settings
    [Header("Double Jump Settings")]
    [SerializeField] private int maxJumps = 2;
    private int jumpCount;
    private bool isDoubleJumping;

    // Slow Motion Settings
    [Header("Slow Motion Settings")]
    [SerializeField] private float slowMotionFactor = 0.5f;
    private bool isSlowMotionActive = false;

    // State Variables
    private bool isGrounded;
    private float xAxis;
    private bool isFacingRight = true;
    private bool isAttacking = false; // Flag to track if an attack is being performed
    private bool isAttackLocked = false; // Add attack lock flag

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
        characterBehavior = GetComponent<ICharacterBehavior>(); // Dynamically link to character behavior
        originalColliderSize = boxCollider.size;
    }

    private void Update()
    {
        if (!isControllable) return; // Skip update if the character is not controllable

        if (isAttackLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        GetInputs();
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
        }

        if (Input.GetKeyDown(KeyCode.BackQuote)) ToggleSlowMotion();
        if (Input.GetKeyDown(KeyCode.R)) ResetPosition();

        isGrounded = Grounded();

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        // Ensure attacks are handled only by the linked controlledGameObject
        if (controlledGameObject != null)
        {
            PlayerAttack controlledPlayerAttack = controlledGameObject.GetComponent<PlayerAttack>();
            if (controlledPlayerAttack != null)
            {
                if (Input.GetKeyDown(lightAttackKey))
                {
                    controlledPlayerAttack.HandleAttack(isGrounded, vertical, horizontal, true);
                }
                else if (Input.GetKeyDown(heavyAttackKey))
                {
                    controlledPlayerAttack.HandleAttack(isGrounded, vertical, horizontal, false);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
    }

    private void GetInputs()
    {
        if (controlScheme == "None") return; // Skip input if no control scheme is selected

        // Debug log to verify control scheme
        Debug.Log($"Using Control Scheme: {controlScheme}");

        if (controlScheme == "Keyboard1")
        {
            xAxis = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical"); // Capture vertical input
        }
        else if (controlScheme == "Keyboard2")
        {
            xAxis = Input.GetAxisRaw("Horizontal2"); // Example for a second keyboard
            float verticalInput = Input.GetAxisRaw("Vertical2"); // Example for vertical input on a second keyboard
        }
        else if (controlScheme == "Keyboard") // Add a case for "Keyboard"
        {
            xAxis = Input.GetAxisRaw("Horizontal"); // Default to "Horizontal" axis
            float verticalInput = Input.GetAxisRaw("Vertical"); // Default to "Vertical" axis
        }
        else
        {
            Debug.LogWarning($"Control scheme '{controlScheme}' is not mapped to any input.");
        }
    }

    private void HandleDash()
    {
        if (Input.GetKeyDown(dashKey) && !isDashing && Grounded())
        {
            Dash();
        }
    }

    private void Move()
    {
        if (controlledGameObject == null) return; // Ensure the controlled GameObject is set

        if (xAxis != 0)
        {
            float targetSpeed = walkSpeed * xAxis;
            float acceleration = 10f;

            // Apply movement only to the Rigidbody2D of the controlled GameObject
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
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
            isFacingRight = false;
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
            isFacingRight = true;
        }
    }

    public bool Grounded()
    {
        float groundCheckRadius = 0.2f;
        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, whatIsGround);
    }

    private bool isWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }

    private void WallSlide()
    {
        if (isWalled() && !Grounded())
        {
            isWallSliding = true;
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
        if (isAttacking) return;

        if (Grounded())
        {
            jumpCount = 0;
            isDoubleJumping = false;
        }

        if (Input.GetButtonDown("Jump") && (Grounded() || jumpCount < maxJumps || isDashing))
        {
            characterBehavior?.ShrinkColliderForJump();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            jumpCount++;

            SpawnJumpSmoke();

            if (!Grounded() && jumpCount == 2)
            {
                isDoubleJumping = true;
                anim.SetBool("DoubleJumping", true);
            }
            else
            {
                anim.SetBool("Jumping", true);
            }

            if (isDashing) isDashing = false;
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }

        if (Input.GetKeyDown(KeyCode.S) && !Grounded())
        {
            rb.AddForce(new Vector2(0, -fallingSpeed), ForceMode2D.Impulse);
        }

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

        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
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

        float dashDuration = 0.2f;
        StartCoroutine(EndDashAfterDuration(dashDuration));
    }

    private void SpawnDashSmoke()
    {
        if (dashSmokePrefab != null)
        {
            GameObject smoke = Instantiate(dashSmokePrefab, transform.position, Quaternion.identity);
            Destroy(smoke, dashSmokeLifetime);
        }
    }

    private void SpawnJumpSmoke()
    {
        if (jumpSmokePrefab != null)
        {
            GameObject smoke = Instantiate(jumpSmokePrefab, transform.position, Quaternion.identity);
            Destroy(smoke, jumpSmokeLifetime);
        }
    }

    private void JumpWhileDashing()
    {
        if (isDashing && Input.GetButtonDown("Jump"))
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
}
