using DG.Tweening;
using UnityEngine;

namespace RpgPlayable
{
    // Endless grow/shrink, for call-to-action buttons.
    public class PulseScale : MonoBehaviour
    {
        [SerializeField] float scale = 1.12f; // same as the AFK end card
        [SerializeField] float halfPeriod = 0.5f;

        Tween tween;

        void OnEnable()
        {
            transform.localScale = Vector3.one;
            tween = transform.DOScale(scale, halfPeriod).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        void OnDisable()
        {
            if (tween != null) tween.Kill();
            transform.localScale = Vector3.one;
        }
    }
}
