using System.Collections;
using UnityEngine;
using System;

public class PlayerAttack : MonoBehaviour
{
    public enum AttackType
    {
        NeutralLight,
        SideLight,
        DownLight,
        NeutralAir,
        SideAir,
        DownAir,
        NeutralHeavy,
        SideHeavy,
        DownHeavy,
        Recovery,
        GroundPound
    }

    // Hitbox references for each attack type
    public GameObject neutralLight;
    public GameObject sideLight;
    public GameObject downLight;
    public GameObject neutralAir;
    public GameObject sideAir;
    public GameObject downAir;
    public GameObject neutralHeavy;
    public GameObject sideHeavy;
    public GameObject downHeavy;
    public GameObject recovery;
    public GameObject groundPound;

    [Header("Light Attack Durations")]
    [SerializeField] private float neutralLightDuration = 0.5f;
    [SerializeField] private float sideLightDuration = 0.5f;
    [SerializeField] private float downLightDuration = 0.5f;
    [SerializeField] private float neutralAirDuration = 0.5f;
    [SerializeField] private float sideAirDuration = 0.5f;
    [SerializeField] private float downAirDuration = 0.5f;

    [Header("Heavy Attack Durations")]
    [SerializeField] private float neutralHeavyDuration = 1.0f;
    [SerializeField] private float sideHeavyDuration = 1.0f;
    [SerializeField] private float downHeavyDuration = 1.0f;
    [SerializeField] private float recoveryDuration = 1.0f;
    [SerializeField] private float groundPoundDuration = 1.0f;

    // Event triggered when an attack is performed
    public static event Action<AttackHitbox> OnAttackPerformed; // Removed ulong parameter
    private PlayerController playerController;
    private float currentAttackDuration;

    [Header("Attack Prioritization")]
    [SerializeField] private bool prioritizeUpOverSide = true;

    private void Start()
    {
        // Ensure all hitboxes are hidden at the start
        HideAllHitboxes();

        // Get reference to PlayerController
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController component not found on the same GameObject.");
        }
    }

    private void Update()
    {
        // Removed network ownership checks
    }

    private bool Grounded()
    {
        return playerController != null && playerController.Grounded();
    }

    public void PerformAttack(AttackType attackType, float duration)
    {
        currentAttackDuration = duration;

        GameObject hitboxObject = GetHitboxForAttackType(attackType);
        if (hitboxObject != null)
        {
            AttackHitbox hitbox = hitboxObject.GetComponent<AttackHitbox>();
            if (hitbox != null)
            {
                hitbox.Initialize(gameObject); // Set the originating player
                hitboxObject.SetActive(true);
                hitbox.StartAttack(duration); // Start the attack and reset hit objects after the duration
                StartCoroutine(DeactivateHitboxAfterDuration(hitboxObject, duration, hitbox));

                // Trigger the OnAttackPerformed event only for this character
                OnAttackPerformed?.Invoke(hitbox);
            }
        }
    }

    public float GetCurrentAttackDuration()
    {
        return currentAttackDuration;
    }

    public void HandleAttack(bool isGrounded, float verticalInput, float horizontalInput, bool isLightAttack)
    {
        if (isLightAttack)
        {
            HandleLightAttack(isGrounded, verticalInput, horizontalInput);
        }
        else
        {
            HandleHeavyAttack(isGrounded, verticalInput, horizontalInput);
        }
    }

    private void HandleLightAttack(bool isGrounded, float verticalInput, float horizontalInput)
    {
        if (isGrounded)
        {
            if (prioritizeUpOverSide && verticalInput > 0)
            {
                PerformAttack(AttackType.NeutralLight, neutralLightDuration); // Neutral Light (Upward priority)
            }
            else if (verticalInput < 0)
            {
                PerformAttack(AttackType.DownLight, downLightDuration); // Down Light
            }
            else if (Mathf.Abs(horizontalInput) > 0)
            {
                PerformAttack(AttackType.SideLight, sideLightDuration); // Side Light
            }
            else
            {
                PerformAttack(AttackType.NeutralLight, neutralLightDuration); // Neutral Light
            }
        }
        else
        {
            if (prioritizeUpOverSide && verticalInput > 0)
            {
                PerformAttack(AttackType.NeutralAir, neutralAirDuration); // Neutral Air (Upward priority)
            }
            else if (verticalInput < 0)
            {
                PerformAttack(AttackType.DownAir, downAirDuration); // Down Air
            }
            else if (Mathf.Abs(horizontalInput) > 0)
            {
                PerformAttack(AttackType.SideAir, sideAirDuration); // Side Air
            }
            else
            {
                PerformAttack(AttackType.NeutralAir, neutralAirDuration); // Neutral Air
            }
        }
    }

    private void HandleHeavyAttack(bool isGrounded, float verticalInput, float horizontalInput)
    {
        if (isGrounded)
        {
            if (prioritizeUpOverSide && verticalInput > 0)
            {
                PerformAttack(AttackType.NeutralHeavy, neutralHeavyDuration); // Neutral Heavy (Upward priority)
            }
            else if (verticalInput < 0)
            {
                PerformAttack(AttackType.DownHeavy, downHeavyDuration); // Down Heavy
            }
            else if (Mathf.Abs(horizontalInput) > 0)
            {
                PerformAttack(AttackType.SideHeavy, sideHeavyDuration); // Side Heavy
            }
            else
            {
                PerformAttack(AttackType.NeutralHeavy, neutralHeavyDuration); // Neutral Heavy
            }
        }
        else
        {
            if (prioritizeUpOverSide && verticalInput > 0)
            {
                PerformAttack(AttackType.Recovery, recoveryDuration); // Recovery (Upward priority)
            }
            else if (verticalInput < 0)
            {
                PerformAttack(AttackType.GroundPound, groundPoundDuration); // Ground Pound
            }
            else if (Mathf.Abs(horizontalInput) > 0)
            {
                PerformAttack(AttackType.SideAir, sideAirDuration); // Side Air
            }
            else
            {
                PerformAttack(AttackType.NeutralAir, neutralAirDuration); // Neutral Air
            }
        }
    }

    private IEnumerator DeactivateHitboxAfterDuration(GameObject hitbox, float duration, AttackHitbox attackHitbox)
    {
        yield return new WaitForSeconds(duration);
        hitbox.SetActive(false); // Hide the hitbox after the duration
        attackHitbox.ResetHitObjects(); // Ensure hit objects are reset
    }

    private void HideAllHitboxes()
    {
        // Deactivate all hitboxes
        if (neutralLight) neutralLight.SetActive(false);
        if (sideLight) sideLight.SetActive(false);
        if (downLight) downLight.SetActive(false);
        if (neutralAir) neutralAir.SetActive(false);
        if (sideAir) sideAir.SetActive(false);
        if (downAir) downAir.SetActive(false);
        if (neutralHeavy) neutralHeavy.SetActive(false);
        if (sideHeavy) sideHeavy.SetActive(false);
        if (downHeavy) downHeavy.SetActive(false);
        if (recovery) recovery.SetActive(false);
        if (groundPound) groundPound.SetActive(false);
    }

    private GameObject GetHitboxForAttackType(AttackType attackType)
    {
        return attackType switch
        {
            AttackType.NeutralLight => neutralLight,
            AttackType.SideLight => sideLight,
            AttackType.DownLight => downLight,
            AttackType.NeutralAir => neutralAir,
            AttackType.SideAir => sideAir,
            AttackType.DownAir => downAir,
            AttackType.NeutralHeavy => neutralHeavy,
            AttackType.SideHeavy => sideHeavy,
            AttackType.DownHeavy => downHeavy,
            AttackType.Recovery => recovery,
            AttackType.GroundPound => groundPound,
            _ => null
        };
    }
}
