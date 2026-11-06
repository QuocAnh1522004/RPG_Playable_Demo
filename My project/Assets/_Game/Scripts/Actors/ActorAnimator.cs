using UnityEngine;

namespace RpgPlayable
{
    public class ActorAnimator : MonoBehaviour
    {
        public const string Idle = "Idle";
        public const string IdleCombat = "IdleCombat";
        public const string Run = "Run";
        public const string Attack = "Attack";
        public const string Hit = "Hit";
        public const string Dash = "Dash";
        public const string JumpBack = "JumpBack";
        public const string Die = "Die";

        [SerializeField] Animator animator;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        // plain Play: Luna doesn't support CrossFadeInFixedTime (it silently does nothing in the build)
        public void Play(string state)
        {
            if (animator == null) return;
            animator.Play(state, 0, 0f);
        }
    }
}
