using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable
{
    public class HpBar : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] Image trail; // the lighter bar that catches up after a hit
        [SerializeField] TMP_Text label;
        [SerializeField] float padding = 30f; // canvas units above the head

        Transform target;
        Camera cam;
        float headHeight;

        public void Follow(Transform target, Camera cam)
        {
            this.target = target;
            this.cam = cam;
            headHeight = MeasureHeight(target);
            LateUpdate();
        }

        public void Set(int hp, int max, bool instant)
        {
            float f = max > 0 ? (float)hp / max : 0f;
            fill.fillAmount = f;
            label.text = hp + " / " + max;

            DOTween.Kill(trail);
            if (instant)
                trail.fillAmount = f;
            else
                DOTween.To(() => trail.fillAmount, v => trail.fillAmount = v, f, 0.4f).SetDelay(0.2f).SetTarget(trail);
        }

        void LateUpdate()
        {
            if (target == null || cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(target.position + Vector3.up * headHeight);
            transform.position = screen + Vector3.up * padding * transform.lossyScale.y;
        }

        // top of the model above its pivot, so the bar fits any model or scale
        static float MeasureHeight(Transform t)
        {
            Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 2f;

            float top = float.MinValue;
            foreach (Renderer r in renderers)
                top = Mathf.Max(top, r.bounds.max.y);
            return top - t.position.y;
        }
    }
}
