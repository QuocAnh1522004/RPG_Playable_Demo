using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable
{
    public class AfkOverlay : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform logo;
        [SerializeField] RectTransform playButton;
        [SerializeField] Button dismissArea; // tapping anywhere else just closes it
        [SerializeField] Button playNow;
        [SerializeField] StoreRedirect store;

        Tween pulse;

        public bool IsShowing { get { return root.activeSelf; } }

        void Awake()
        {
            dismissArea.onClick.AddListener(Hide);
            playNow.onClick.AddListener(store.OpenStore);
            root.SetActive(false);
        }

        public void Show()
        {
            transform.SetAsLastSibling();
            root.SetActive(true);
            AudioManager.Play(AudioManager.Sounds.panelOpen);

            group.alpha = 0f;
            DOTween.To(() => group.alpha, a => group.alpha = a, 1f, 0.25f);

            logo.localScale = Vector3.one * 0.8f;
            logo.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            playButton.localScale = Vector3.one;
            pulse = playButton.DOScale(1.12f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        public void Hide()
        {
            if (pulse != null) pulse.Kill();
            pulse = null;
            root.SetActive(false);
        }
    }
}
