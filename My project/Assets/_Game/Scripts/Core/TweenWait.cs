using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace RpgPlayable
{
    // LUNA RULE: Luna's coroutine runner kills the whole chain when a nested IEnumerator finishes and its
    // parent then finishes in the same step (Unity just resumes the grandparent). So every coroutine that
    // yields another IEnumerator ends with "yield return null;" - keep that when adding new ones.
    public static class TweenWait
    {
        // Polling IsActive()/IsComplete() never ends under Luna's DOTween port, which froze the whole flow
        // in the build. Wait for the kill callback instead, with the tween's own length as a safety net.
        public static IEnumerator For(Tween tween)
        {
            if (tween == null) yield break;

            bool done = false;
            tween.onKill += () => done = true; // autoKill fires this on complete, and a manual Kill does too

            float limit = tween.Delay() + tween.Duration(true) + 0.1f;
            float elapsed = 0f;
            while (!done && elapsed < limit)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
