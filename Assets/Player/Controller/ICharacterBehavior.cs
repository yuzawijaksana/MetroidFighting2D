using UnityEngine;

public interface ICharacterBehavior
{
    void ShrinkCollider(float xFactor, float yFactor);
    void ShrinkColliderForWallSlide();
    void ShrinkColliderForJump();
    void RestoreCollider();
    void PerformAttack(PlayerAttack.AttackType attackType); // Add method for handling attacks
}
