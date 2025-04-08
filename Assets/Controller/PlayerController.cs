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
    [SerializeField] public bool isControllable = true;

    [Header("Player Settings")]
    [SerializeField] private GameObject controlledGameObject;

    [Header("Key Bindings")]
    public KeyCode lightAttackKey = KeyCode.J;
    public KeyCode heavyAttackKey = KeyCode.K;
    public KeyCode dashKey = KeyCode.L;

    [Header("Ground Check Settings")]
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public LayerMask whatIsGround;
    [SerializeField] private float jumpForce = 45;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1.0f;
    [SerializeField] private float deceleration = 0.95f;
    [SerializeField] private float fallingSpeed = 10f;
    [SerializeField] private float maxFallSpeed = -20f;

    [Header("Sliding Settings")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallSlidingSpeed = 2f;
    [SerializeField] private LayerMask wallInteractionLayers;
    [SerializeField] private float wallCheckRadius = 0.2f;
    private bool isWallSliding;
    private Vector2 originalColliderSize;

    [Header("Jump Settings")]
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
    [SerializeField] private Vector2 wallJumpingPower = new Vector2(4f, 8f);

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private GameObject dashSmokePrefab;
    [SerializeField] private float dashSmokeLifetime = 0.5f;
    private bool isDashing;

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowMotionFactor = 0.5f;
    private bool isSlowMotionActive = false;

    // References
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator anim;
    private PlayerAttack playerAttack;
    private ICharacterBehavior characterBehavior;
    private CinemachineCamera virtualCamera;

    // State Variables
    private bool isGrounded;
    private float xAxis;
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
    }

    private void Update()
    {
        if (!isControllable) return;

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

        if (Keyboard.current.rKey.wasPressedThisFrame) ResetPosition();

        isGrounded = Grounded();

        float vertical = Keyboard.current.wKey.isPressed ? 1 : (Keyboard.current.sKey.isPressed ? -1 : 0);
        float horizontal = Keyboard.current.aKey.isPressed ? -1 : (Keyboard.current.dKey.isPressed ? 1 : 0);

        if (controlledGameObject != null)
        {
            PlayerAttack controlledPlayerAttack = controlledGameObject.GetComponent<PlayerAttack>();
            if (controlledPlayerAttack != null)
            {
                if (Keyboard.current.jKey.wasPressedThisFrame)
                {
                    controlledPlayerAttack.HandleAttack(isGrounded, vertical, horizontal, true);
                }
                else if (Keyboard.current.kKey.wasPressedThisFrame)
                {
                    controlledPlayerAttack.HandleAttack(isGrounded, vertical, horizontal, false);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (!Keyboard.current.sKey.isPressed)
        {
            if (rb.linearVelocity.y < maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
            }
        }
    }

    private void GetInputs()
    {
        if (controlScheme == "None") return;

        if (controlScheme == "Keyboard1" || controlScheme == "Keyboard")
        {
            xAxis = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
        }
        else if (controlScheme == "Keyboard2")
        {
            xAxis = (Keyboard.current.rightArrowKey.isPressed ? 1 : 0) - (Keyboard.current.leftArrowKey.isPressed ? 1 : 0);
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

        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }

        if (Keyboard.current.sKey.wasPressedThisFrame && !Grounded())
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

        if (Keyboard.current.spaceKey.wasPressedThisFrame && wallJumpingCounter > 0f)
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
        if (isDashing && Keyboard.current.spaceKey.wasPressedThisFrame)
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
