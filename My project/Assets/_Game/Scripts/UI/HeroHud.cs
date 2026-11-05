using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable
{
    public class HeroHud : MonoBehaviour
    {
        [SerializeField] HeroStats stats;
        [SerializeField] Image hpFill;
        [SerializeField] TMP_Text hpText;
        [SerializeField] TMP_Text atkText;
        [SerializeField] TMP_Text defText;

        int lastMaxHp, lastAtk, lastDef;

        void OnEnable()
        {
            stats.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            stats.Changed -= Refresh;
        }

        void Refresh()
        {
            if (stats.MaxHp <= 0) return;

            hpFill.fillAmount = (float)stats.Hp / stats.MaxHp;
            hpText.text = stats.Hp + " / " + stats.MaxHp;
            atkText.text = stats.Atk.ToString();
            defText.text = stats.Def.ToString();

            // little bump when a passive raises a stat
            if (lastMaxHp > 0 && stats.MaxHp > lastMaxHp) Bump(hpText);
            if (lastAtk > 0 && stats.Atk > lastAtk) Bump(atkText);
            if (lastDef > 0 && stats.Def > lastDef) Bump(defText);

            lastMaxHp = stats.MaxHp;
            lastAtk = stats.Atk;
            lastDef = stats.Def;
        }

        static void Bump(TMP_Text text)
        {
            text.transform.DOKill(true);
            text.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 0.5f);
        }
    }
}
