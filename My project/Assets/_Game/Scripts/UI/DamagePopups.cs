using DG.Tweening;
using TMPro;
using UnityEngine;

namespace RpgPlayable
{
    public enum DamageStyle { Normal, Crit, Burn, Poison, Lightning }

    public class DamagePopups : MonoBehaviour
    {
        [SerializeField] TMP_Text template;
        [SerializeField] Material[] styleMaterials = new Material[5]; // same order as DamageStyle
        [SerializeField] Camera worldCamera;
        [SerializeField] float rise = 110f;
        [SerializeField] float scatter = 50f;
        [SerializeField] float duration = 0.8f;

        void Awake()
        {
            template.gameObject.SetActive(false);
        }

        public void Show(int amount, DamageStyle style, Vector3 worldPos)
        {
            TMP_Text t = Instantiate(template, template.transform.parent);
            t.gameObject.SetActive(true);
            t.text = amount.ToString();

            Material mat = styleMaterials[(int)style];
            if (mat != null) t.fontSharedMaterial = mat;

            float uiScale = transform.lossyScale.x;
            RectTransform rt = t.rectTransform;
            Vector3 screen = worldCamera.WorldToScreenPoint(worldPos);
            rt.position = screen + new Vector3(Random.Range(-scatter, scatter), Random.Range(-10f, 10f), 0f) * uiScale;

            float size = style == DamageStyle.Crit ? 1.4f : 1f;
            rt.localScale = Vector3.one * size * 0.5f;

            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOScale(size, 0.15f).SetEase(Ease.OutBack));
            seq.Join(rt.DOMoveY(rt.position.y + rise * uiScale, duration).SetEase(Ease.OutCubic));
            seq.Insert(duration * 0.6f, DOTween.To(() => t.alpha, a => t.alpha = a, 0f, duration * 0.4f));
            seq.OnComplete(() => Destroy(t.gameObject));
        }
    }
}
