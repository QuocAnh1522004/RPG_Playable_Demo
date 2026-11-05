using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable
{
    public class SkillCard : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text tagText;
        [SerializeField] GameObject highlight;

        CanvasGroup group;
        Vector2 home;
        bool homeSet;

        public SkillData Skill { get; private set; }
        public RectTransform Rect { get { return (RectTransform)transform; } }
        public event Action<SkillCard> Clicked;

        // cards sit under an inactive panel until first shown, so don't rely on Awake for these
        CanvasGroup Group
        {
            get
            {
                if (group == null) group = GetComponent<CanvasGroup>();
                if (group == null) group = gameObject.AddComponent<CanvasGroup>();
                return group;
            }
        }

        void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            if (Clicked != null) Clicked(this);
        }

        public void Bind(SkillData skill)
        {
            Skill = skill;
            icon.sprite = skill.icon;
            nameText.text = skill.displayName;
            descriptionText.text = skill.description;
            tagText.text = skill.legendary ? "Legendary" : "Common";
            SetHighlight(false);
        }

        public void PlayIntro(float delay)
        {
            if (!homeSet)
            {
                home = Rect.anchoredPosition;
                homeSet = true;
            }

            Rect.DOKill();
            DOTween.Kill(Group);

            Rect.anchoredPosition = home + new Vector2(-120f, 0f);
            Group.alpha = 0f;
            Rect.DOAnchorPos(home, 0.3f).SetDelay(delay).SetEase(Ease.OutCubic);
            DOTween.To(() => Group.alpha, a => Group.alpha = a, 1f, 0.25f).SetDelay(delay).SetTarget(Group);
        }

        public void SetDimmed(bool dimmed)
        {
            DOTween.Kill(Group);
            DOTween.To(() => Group.alpha, a => Group.alpha = a, dimmed ? 0.4f : 1f, 0.15f).SetTarget(Group);
        }

        public void SetHighlight(bool on)
        {
            if (highlight != null) highlight.SetActive(on);
        }

        public void SetInteractable(bool on)
        {
            button.interactable = on;
        }
    }
}
