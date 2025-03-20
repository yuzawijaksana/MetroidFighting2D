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

    // Event triggered when an attack is performed
    public static event Action<AttackType, float> OnAttackPerformed;
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
    }

    private bool Grounded()
    {
        return playerController != null && playerController.Grounded();
    }

    public void PerformAttack(AttackType attackType, float duration)
    {
        Debug.Log($"Performing {attackType} attack for {duration} seconds.");

        // Store the current attack duration
        currentAttackDuration = duration;

        // Trigger the event to notify subscribers (e.g., BirdController)
        OnAttackPerformed?.Invoke(attackType, duration);
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
                PerformAttack(AttackType.NeutralLight, 0.5f); // Neutral Light (Upward priority)
            }
            else if (verticalInput < 0)
            {
                PerformAttack(AttackType.DownLight, 0.5f); // Down Light
            }
            else if (Mathf.Abs(horizontalInput) > 0)
            {
                PerformAttack(AttackType.SideLight, 0.5f); // Side Light
            }
            else
            {
                PerformAttack(AttackType.NeutralLight, 0.5f); // Neutral Light
            }
        }
        else
        {
            if (prioritizeUpOverSide && verticalInput > 0)
            {
                PerformAttack(AttackType.NeutralAir, 0.5f); // Neutral Air (Upward priority)
            }
            else if (verticalInput < 0)
            {
                PerformAttack(AttackType.DownAir, 0.5f); // Down Air
            }
            else if (Mathf.Abs(horizontalInput) > 0)
            {
                PerformAttack(AttackType.SideAir, 0.5f); // Side Air
            }
            else
            {
                PerformAttack(AttackType.NeutralAir, 0.5f); // Neutral Air
            }
        }
    }

    private void HandleHeavyAttack(bool isGrounded, float verticalInput, float horizontalInput)
    {
        if (isGrounded)
        {
            if (prioritizeUpOverSide && verticalInput > 0)
            {
                PerformAttack(AttackType.NeutralHeavy, 1.0f); // Neutral Heavy (Upward priority)
            }
            else if (verticalInput < 0)
            {
                PerformAttack(AttackType.DownHeavy, 1.0f); // Down Heavy
            }
            else if (Mathf.Abs(horizontalInput) > 0)
            {
                PerformAttack(AttackType.SideHeavy, 1.0f); // Side Heavy
            }
            else
            {
                PerformAttack(AttackType.NeutralHeavy, 1.0f); // Neutral Heavy
            }
        }
        else
        {
            if (prioritizeUpOverSide && verticalInput > 0)
            {
                PerformAttack(AttackType.Recovery, 1.0f); // Recovery (Upward priority)
            }
            else if (verticalInput < 0)
            {
                PerformAttack(AttackType.GroundPound, 1.0f); // Ground Pound
            }
            else if (Mathf.Abs(horizontalInput) > 0)
            {
                PerformAttack(AttackType.SideAir, 1.0f); // Side Air
            }
            else
            {
                PerformAttack(AttackType.NeutralAir, 1.0f); // Neutral Air
            }
        }
    }

    private System.Collections.IEnumerator DeactivateHitboxAfterDuration(GameObject hitbox, float duration)
    {
        yield return new WaitForSeconds(duration);
        hitbox.SetActive(false); // Hide the hitbox after the duration
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
        // Map attack types to their corresponding hitboxes
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
