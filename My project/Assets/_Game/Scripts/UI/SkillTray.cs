using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable
{
    // Bottom "Skills" panel in battle, one slot per owned skill.
    public class SkillTray : MonoBehaviour
    {
        [SerializeField] RectTransform slotTemplate; // frame with the icon Image as its first child
        [SerializeField] float spacing = 130f;

        readonly List<GameObject> slots = new List<GameObject>();

        void Awake()
        {
            slotTemplate.gameObject.SetActive(false);
        }

        public void Show(IList<SkillData> owned)
        {
            foreach (GameObject s in slots) Destroy(s);
            slots.Clear();

            for (int i = 0; i < owned.Count; i++)
            {
                RectTransform slot = Instantiate(slotTemplate, slotTemplate.parent);
                slot.gameObject.SetActive(true);
                slot.anchoredPosition = slotTemplate.anchoredPosition + new Vector2(i * spacing, 0f);
                slot.GetChild(0).GetComponent<Image>().sprite = owned[i].icon;
                slots.Add(slot.gameObject);
            }
        }
    }
}
