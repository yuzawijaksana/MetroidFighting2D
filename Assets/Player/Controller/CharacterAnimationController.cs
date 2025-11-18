using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    private Animator animator;
    private string lastAnimationName = "";

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on this GameObject.");
        }
    }

    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }

    public void SetTrigger(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    public void SetBool(string parameterName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(parameterName, value);
        }
    }

    public float GetAnimationLength(string animationName)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name == animationName)
                {
                    return clip.length;
                }
            }
        }
        return 0.2f; // Default fallback length
    }

    void Update()
    {
        if (animator == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (var clip in clips)
        {
            if (stateInfo.IsName(clip.name))
            {
                if (clip.name != lastAnimationName)
                {
                    float fps = clip.frameRate;
                    int totalFrames = Mathf.RoundToInt(clip.length * fps);
                    // Debug.Log($"[Animation Debug] Animation: {clip.name}, Frames: {totalFrames}, FPS: {fps}");
                    lastAnimationName = clip.name;
                }
                break;
            }
        }
    }
}
