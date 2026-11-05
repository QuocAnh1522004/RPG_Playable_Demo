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
        [SerializeField] float defaultFade = 0.1f;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public void Play(string state)
        {
            Play(state, defaultFade);
        }

        public void Play(string state, float fade)
        {
            if (animator == null) return;
            animator.CrossFadeInFixedTime(state, fade);
        }
    }
}
