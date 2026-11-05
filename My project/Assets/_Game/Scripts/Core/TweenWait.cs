using System.Collections;
using DG.Tweening;

namespace RpgPlayable
{
    public static class TweenWait
    {
        // plain loop instead of WaitForCompletion(), CustomYieldInstruction is risky under Luna
        public static IEnumerator For(Tween tween)
        {
            while (tween != null && tween.IsActive() && !tween.IsComplete())
                yield return null;
        }
    }
}
