using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace RpgPlayable
{
    // Lives on a full-screen object at the end of the canvas so it draws on top of everything.
    public class TutorialHand : MonoBehaviour
    {
        [SerializeField] RectTransform hand;
        [SerializeField] Vector2 cardOffset = new Vector2(150f, -20f); // canvas units, so the finger isn't dead center
        [SerializeField] float moveDuration = 0.35f;
        [SerializeField] float stayDuration = 0.9f;

        Coroutine sweep;
        Sequence tap;

        void Awake()
        {
            hand.gameObject.SetActive(false);
        }

        // anchor = 0..1 over the screen (see UiAnchor), for things in the 3D world like the dice
        public void PointAt(Vector2 anchor)
        {
            Stop();
            Show();
            UiAnchor.Place(hand, anchor, Vector2.zero);
            PlayTap();
        }

        // visits each target once, then keeps tapping on the last one
        public void PointAtEach(List<RectTransform> targets, Action<int> onTarget)
        {
            Stop();
            if (targets.Count == 0) return;
            Show();
            UiAnchor.Place(hand, new Vector2(0.5f, 0.5f), Vector2.zero); // UI targets use world positions, reset the anchor
            sweep = StartCoroutine(SweepRoutine(targets, onTarget));
        }

        public void Hide()
        {
            Stop();
            hand.gameObject.SetActive(false);
        }

        IEnumerator SweepRoutine(List<RectTransform> targets, Action<int> onTarget)
        {
            hand.position = TargetPosition(targets[0]);

            for (int i = 0; i < targets.Count; i++)
            {
                if (i > 0)
                    yield return TweenWait.For(hand.DOMove(TargetPosition(targets[i]), moveDuration).SetEase(Ease.InOutQuad));

                if (onTarget != null) onTarget(i);
                PlayTap();
                if (i < targets.Count - 1)
                {
                    yield return new WaitForSeconds(stayDuration);
                    KillTap();
                }
            }
            sweep = null;
        }

        Vector3 TargetPosition(RectTransform target)
        {
            return target.position + (Vector3)(cardOffset * transform.lossyScale.x);
        }

        void PlayTap()
        {
            KillTap();
            tap = DOTween.Sequence();
            tap.Append(hand.DOScale(0.85f, 0.25f).SetEase(Ease.OutQuad));
            tap.Append(hand.DOScale(1f, 0.25f).SetEase(Ease.InQuad));
            tap.SetLoops(-1);
        }

        void KillTap()
        {
            if (tap != null) tap.Kill();
            tap = null;
            hand.localScale = Vector3.one;
        }

        void Stop()
        {
            if (sweep != null) StopCoroutine(sweep);
            sweep = null;
            KillTap();
            hand.DOKill();
        }

        void Show()
        {
            transform.SetAsLastSibling();
            hand.gameObject.SetActive(true);
        }
    }
}
