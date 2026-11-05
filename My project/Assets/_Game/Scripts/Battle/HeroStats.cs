using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgPlayable
{
    public class HeroStats : MonoBehaviour
    {
        [SerializeField] int baseMaxHp = 1000;
        [SerializeField] int baseAtk = 300;
        [SerializeField] int baseDef = 10;
        [SerializeField] float baseCritChance = 0.1f;

        public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public int Atk { get; private set; }
        public int Def { get; private set; }
        public float CritChance { get; private set; }

        public event Action Changed;

        void Awake()
        {
            Recalculate(null);
            Hp = MaxHp;
        }

        public void Recalculate(IList<SkillData> owned)
        {
            float hp = 0f, atk = 0f, def = 0f, crit = 0f;
            if (owned != null)
            {
                foreach (SkillData s in owned)
                {
                    if (s.kind != SkillKind.Passive) continue;
                    switch (s.stat)
                    {
                        case StatType.MaxHp: hp += s.statBonusPercent; break;
                        case StatType.Atk: atk += s.statBonusPercent; break;
                        case StatType.Def: def += s.statBonusPercent; break;
                        case StatType.CritChance: crit += s.statBonusPercent; break;
                    }
                }
            }

            int oldMax = MaxHp;
            MaxHp = Mathf.RoundToInt(baseMaxHp * (1f + hp / 100f));
            Atk = Mathf.RoundToInt(baseAtk * (1f + atk / 100f));
            Def = Mathf.RoundToInt(baseDef * (1f + def / 100f));
            CritChance = baseCritChance + crit / 100f;

            // more max hp also heals by the same amount, like 1000/1000 -> 1200/1200 in the video
            if (oldMax > 0) Hp += MaxHp - oldMax;
            Notify();
        }

        public void TakeDamage(int amount)
        {
            Hp = Mathf.Max(1, Hp - amount); // the hero can't die in the ad
            Notify();
        }

        void Notify()
        {
            if (Changed != null) Changed();
        }
    }
}
