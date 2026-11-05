using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace RpgPlayable
{
    public class BattleBanner : MonoBehaviour
    {
        [SerializeField] RectTransform banner;
        [SerializeField] TMP_Text label;
        [SerializeField] float holdTime = 0.8f;

        void Awake()
        {
            banner.gameObject.SetActive(false);
        }

        public IEnumerator Play(string text)
        {
            label.text = text;
            banner.gameObject.SetActive(true);

            float width = ((RectTransform)banner.parent).rect.width;
            Vector2 pos = banner.anchoredPosition;
            banner.anchoredPosition = new Vector2(-width, pos.y);

            yield return TweenWait.For(banner.DOAnchorPosX(0f, 0.25f).SetEase(Ease.OutQuad));
            yield return new WaitForSeconds(holdTime);
            yield return TweenWait.For(banner.DOAnchorPosX(width, 0.25f).SetEase(Ease.InQuad));

            banner.gameObject.SetActive(false);
            banner.anchoredPosition = pos;
        }
    }
}
