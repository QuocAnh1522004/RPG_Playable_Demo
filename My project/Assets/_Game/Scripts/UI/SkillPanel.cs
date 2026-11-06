using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace RpgPlayable
{
    public class SkillPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform content;
        [SerializeField] SkillCard[] cards;
        [SerializeField] TutorialHand hand;
        [SerializeField] float fadeDuration = 0.2f;
        [SerializeField] float cardStagger = 0.08f;

        readonly List<RectTransform> handTargets = new List<RectTransform>();
        SkillCard picked;
        Action onPicked;

        public SkillData Picked { get { return picked != null ? picked.Skill : null; } }

        void Awake()
        {
            foreach (SkillCard card in cards)
                card.Clicked += OnCardClicked;
            root.SetActive(false);
        }

        // onPicked runs inside the click itself, for things that need a real user gesture (opening the store)
        public IEnumerator ShowAndWaitForPick(List<SkillData> options, Action onPicked = null)
        {
            picked = null;
            this.onPicked = onPicked;
            handTargets.Clear();

            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < options.Count;
                cards[i].gameObject.SetActive(used);
                if (!used) continue;

                cards[i].Bind(options[i]);
                cards[i].SetInteractable(true);
                handTargets.Add(cards[i].Rect);
            }

            root.SetActive(true);
            AudioManager.Play(AudioManager.Sounds.panelOpen);
            group.alpha = 0f;
            content.localScale = Vector3.one * 0.9f;
            Fade(1f);
            content.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            for (int i = 0; i < handTargets.Count; i++)
                cards[i].PlayIntro(0.1f + i * cardStagger);
            yield return new WaitForSeconds(0.45f + handTargets.Count * cardStagger);

            hand.PointAtEach(handTargets, HighlightIndex);

            while (picked == null) yield return null;

            hand.Hide();
            AudioManager.Play(AudioManager.Sounds.panelClose);
            foreach (SkillCard card in cards)
            {
                card.SetInteractable(false);
                card.SetHighlight(card == picked);
                if (card != picked) card.SetDimmed(true);
            }

            yield return TweenWait.For(picked.Rect.DOPunchScale(Vector3.one * 0.06f, 0.25f, 6, 0.5f));
            yield return TweenWait.For(Fade(0f));
            root.SetActive(false);
            yield return null; // Luna, see TweenWait
        }

        void OnCardClicked(SkillCard card)
        {
            if (picked != null) return;
            picked = card;
            if (onPicked != null) onPicked();
        }

        void HighlightIndex(int index)
        {
            for (int i = 0; i < cards.Length; i++)
                cards[i].SetHighlight(i == index);
        }

        Tween Fade(float to)
        {
            return DOTween.To(() => group.alpha, a => group.alpha = a, to, fadeDuration);
        }
    }
}
