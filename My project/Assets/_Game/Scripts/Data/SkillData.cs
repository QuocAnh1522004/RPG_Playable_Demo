using UnityEngine;

namespace RpgPlayable
{
    public enum SkillKind { Active, BasicAttackModifier, Passive }

    public enum SkillTrigger { None, BattleStartOnly, BattleStartThenEveryNRounds, EveryRound }

    public enum StatType { None, MaxHp, Atk, Def, CritChance, Combo, BasicAttackDamage }

    public enum StatusEffect { None, Burn, Poison, Lightning }

    [CreateAssetMenu(menuName = "RPG Playable/Skill Data", fileName = "Skill_")]
    public class SkillData : ScriptableObject
    {
        [Header("Display")]
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public bool selectable = true; // false for Basic Attack
        public bool legendary;

        [Header("Behaviour")]
        public SkillKind kind;
        public SkillTrigger trigger;
        public int roundInterval = 1;
        public int projectileCount = 1;
        public bool fromAbove; // projectiles drop onto the target instead of flying from the hero
        public int priority = 100; // lower fires first

        [Header("On-hit effect")]
        public StatusEffect effect;
        [Range(0f, 1f)] public float effectChance;

        [Header("Passive")]
        public StatType stat;
        public float statBonusPercent;

        [Header("FX")]
        public GameObject castFx;
        public GameObject hitFx;
        public GameObject effectFx;

        [Header("Sound")]
        public AudioClip hitSound;
        public AudioClip effectSound;

        public bool TriggersOnRound(int round)
        {
            if (kind != SkillKind.Active) return false;

            switch (trigger)
            {
                case SkillTrigger.BattleStartOnly: return round == 1;
                case SkillTrigger.BattleStartThenEveryNRounds: return (round - 1) % Mathf.Max(1, roundInterval) == 0;
                case SkillTrigger.EveryRound: return true;
                default: return false;
            }
        }
    }
}
