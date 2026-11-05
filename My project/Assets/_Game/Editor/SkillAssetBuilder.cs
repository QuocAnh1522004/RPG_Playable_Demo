using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RpgPlayable.EditorTools
{
    // Creates/updates the skill assets from the readme table. Safe to re-run.
    public static class SkillAssetBuilder
    {
        const string OutDir = "Assets/_Game/Data/Skills";
        const string LibraryPath = "Assets/_Game/Data/SkillLibrary.asset";
        const string IconDir = "Assets/PLA_TEST/Art/UI/Skill Icon/";
        const string FxDir = "Assets/PLA_TEST/Art/FX/Prefabs/";

        const string FxDagger = "pb_missile_dagger_reusable";
        const string FxDaggerHit = "fx_dagger_hit_reusable";
        const string FxLightning = "fx_skill_lightning_reusable";
        const string FxFireball = "pb_missile_skill_fireball_reusable";
        const string FxFireballHit = "fx_skill_fireball_hit_reusable";
        const string FxBurn = "combat_fx_burn";
        const string FxPoison = "combat_fx_poison";
        const string FxSlash = "hero 1_sword_fx_atk_slash";
        const string FxSlashHit = "hero 1_fx_atk_hit";

        [MenuItem("RPG Playable/Build Skill Assets")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(OutDir))
            {
                Directory.CreateDirectory(OutDir);
                AssetDatabase.Refresh();
            }

            var all = new List<SkillData>();

            all.Add(Make("flames_from_above", "Flames From Above",
                "At the start of battle, trigger 3 Fire balls, each with a chance to inflict Burn.",
                "skill_c16", s =>
                {
                    s.kind = SkillKind.Active;
                    s.trigger = SkillTrigger.BattleStartOnly;
                    s.projectileCount = 3;
                    s.priority = 0;
                    s.effect = StatusEffect.Burn;
                    s.effectChance = 0.4f;
                    s.castFx = Fx(FxFireball);
                    s.hitFx = Fx(FxFireballHit);
                    s.effectFx = Fx(FxBurn);
                }));

            all.Add(Make("dagger", "Dagger",
                "Throw 1 Dagger at battle start and every 2 rounds thereafter.",
                "skill_c33", s =>
                {
                    s.kind = SkillKind.Active;
                    s.trigger = SkillTrigger.BattleStartThenEveryNRounds;
                    s.roundInterval = 2;
                    s.priority = 1;
                    s.castFx = Fx(FxDagger);
                    s.hitFx = Fx(FxDaggerHit);
                }));

            all.Add(Make("lightning_dagger", "Lightning Dagger",
                "Throw 1 Dagger at battle start and every 2 rounds thereafter. Each Dagger has a chance to create 1 Lightning bolt.",
                "skill_c34", s =>
                {
                    s.kind = SkillKind.Active;
                    s.trigger = SkillTrigger.BattleStartThenEveryNRounds;
                    s.roundInterval = 2;
                    s.priority = 2;
                    s.effect = StatusEffect.Lightning;
                    s.effectChance = 0.5f;
                    s.castFx = Fx(FxDagger);
                    s.hitFx = Fx(FxDaggerHit);
                    s.effectFx = Fx(FxLightning);
                }));

            all.Add(Make("round_fire_strike", "Round Fire Strike",
                "Trigger 1 Fire ball every round, each with a chance to inflict Burn.",
                "skill_c14", s =>
                {
                    s.kind = SkillKind.Active;
                    s.trigger = SkillTrigger.EveryRound;
                    s.priority = 3;
                    s.effect = StatusEffect.Burn;
                    s.effectChance = 0.3f;
                    s.castFx = Fx(FxFireball);
                    s.hitFx = Fx(FxFireballHit);
                    s.effectFx = Fx(FxBurn);
                }));

            // readme says skill_c16 here too (same as Flames From Above), c5 is unused in the pack so use that
            all.Add(Make("basic_attack_poison", "Basic Attack Poison",
                "Basic ATK has a chance to inflict Poison.",
                "skill_c5", s =>
                {
                    s.kind = SkillKind.BasicAttackModifier;
                    s.effect = StatusEffect.Poison;
                    s.effectChance = 0.5f;
                    s.effectFx = Fx(FxPoison);
                }));

            all.Add(Passive("hp", "HP", "Max HP ↑", "skill_c41", StatType.MaxHp, 20));
            all.Add(Passive("combo", "Combo", "Combo ↑", "skill_c1", StatType.Combo, 15));
            all.Add(Passive("huge_hp", "Huge HP", "Max HP ↑↑", "skill_l22", StatType.MaxHp, 50));
            all.Add(Passive("def", "DEF", "DEF ↑", "skill_c43", StatType.Def, 15));
            all.Add(Passive("atk", "ATK", "ATK ↑", "skill_c42", StatType.Atk, 15));
            all.Add(Passive("enhanced_basic_attack", "Enhanced Basic Attack", "Basic ATK DMG ↑↑", "skill_c3", StatType.BasicAttackDamage, 40));
            all.Add(Passive("heavy_attack_chance", "Heavy Attack Chance", "Crit Chance ↑", "skill_c17", StatType.CritChance, 15));

            SkillData basic = Make("basic_attack", "Basic Attack",
                "Attack with equipped weapon every round.", null, s =>
                {
                    s.selectable = false;
                    s.kind = SkillKind.Active;
                    s.trigger = SkillTrigger.EveryRound;
                    s.priority = 4;
                    s.castFx = Fx(FxSlash);
                    s.hitFx = Fx(FxSlashHit);
                });

            var library = AssetDatabase.LoadAssetAtPath<SkillLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SkillLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.basicAttack = basic;
            library.skills = all;
            EditorUtility.SetDirty(library);

            AssetDatabase.SaveAssets();
            Debug.Log("Built " + all.Count + " skills + Basic Attack");
        }

        static SkillData Passive(string id, string name, string desc, string icon, StatType stat, float percent)
        {
            return Make(id, name, desc, icon, s =>
            {
                s.kind = SkillKind.Passive;
                s.stat = stat;
                s.statBonusPercent = percent;
            });
        }

        static SkillData Make(string id, string name, string desc, string icon, System.Action<SkillData> setup)
        {
            string path = OutDir + "/Skill_" + id + ".asset";
            var s = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(s, path);
            }

            // reset so old values don't stick around when re-running
            s.selectable = true;
            s.kind = SkillKind.Passive;
            s.trigger = SkillTrigger.None;
            s.roundInterval = 1;
            s.projectileCount = 1;
            s.priority = 100;
            s.effect = StatusEffect.None;
            s.effectChance = 0f;
            s.stat = StatType.None;
            s.statBonusPercent = 0f;
            s.castFx = null;
            s.hitFx = null;
            s.effectFx = null;

            s.id = id;
            s.displayName = name;
            s.description = desc;
            s.icon = string.IsNullOrEmpty(icon) ? null : LoadIcon(icon);
            setup(s);

            EditorUtility.SetDirty(s);
            return s;
        }

        static Sprite LoadIcon(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconDir + name + ".png");
            if (sprite == null) Debug.LogWarning("Icon not found: " + name);
            return sprite;
        }

        static GameObject Fx(string name)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(FxDir + name + ".prefab");
            if (go == null) Debug.LogWarning("FX prefab not found: " + name);
            return go;
        }
    }
}
