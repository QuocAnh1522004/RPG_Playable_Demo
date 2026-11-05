using System;
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

        public SkillData Skill { get; private set; }
        public RectTransform Rect { get { return (RectTransform)transform; } }
        public event Action<SkillCard> Clicked;

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
