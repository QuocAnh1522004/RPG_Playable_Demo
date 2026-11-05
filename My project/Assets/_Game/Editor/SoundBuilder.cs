using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RpgPlayable.EditorTools
{
    // Fills the SoundLibrary asset from the pack's AudioClip folder and puts an AudioManager on GameFlow.
    public static class SoundBuilder
    {
        const string Dir = "Assets/PLA_TEST/Art/AudioClip/";
        const string LibraryPath = "Assets/_Game/Data/SoundLibrary.asset";

        [MenuItem("RPG Playable/Build Sounds")]
        static void Build()
        {
            var lib = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            lib.music = Clip("Bgm/ui_bgm_prologue_realm.mp3");

            lib.diceTap = Clip("UI/sfx_ui_button_roll.mp3");
            lib.diceRoll = Clip("UI/sfx_ui_campaign_roll_dice_anim.mp3");
            lib.diceResult = Clip("UI/sfx_global_fx_roll_result.wav");
            lib.panelOpen = Clip("UI/Common Pop-up/sfx_ui_common_pop-up_open.wav");
            lib.panelClose = Clip("UI/Common Pop-up/sfx_ui_common_pop-up_close.mp3");
            lib.pickup = Clip("UI/Common Pop-up/sfx_ui_common_pop-up_close.mp3"); // no coin sound in the pack
            lib.upgrade = Clip("Combat/sfx_main_fx_hero_spawn.mp3");
            lib.footsteps = Clips("Combat/sfx_main_footsteps1.mp3", "Combat/sfx_main_footsteps2.mp3",
                                  "Combat/sfx_main_footsteps3.mp3", "Combat/sfx_main_footsteps4.mp3");

            lib.heroDash = Clip("Combat/sfx_main_anim_dash.mp3");
            lib.heroJumpBack = Clip("Combat/sfx_main_anim_jumpback.mp3");
            lib.heroSwing = Clips("Combat/sfx_main_anim_mele_release1.ogg", "Combat/sfx_main_anim_mele_release2.ogg",
                                  "Combat/sfx_main_anim_mele_release3.ogg", "Combat/sfx_main_anim_mele_release4.ogg");
            lib.heroHurt = Clips("Combat/sfx_main_anim_get_hit1.mp3", "Combat/sfx_main_anim_get_hit2.mp3",
                                 "Combat/sfx_main_anim_get_hit3.mp3");

            lib.monsters = new[]
            {
                new MonsterSounds
                {
                    monsterName = "SLIME",
                    dash = Clip("Combat/sfx_enemy_slime_dash.wav"),
                    attack = Clip("Combat/sfx_enemy_slime_atk.wav"),
                    attackHit = Clip("Combat/sfx_enemy_slime_fx_atk_hit.wav"),
                    hurt = Clip("Combat/sfx_enemy_slime_get_hit.wav"),
                    die = Clip("Combat/sfx_enemy_slime_die.wav"),
                },
                new MonsterSounds
                {
                    monsterName = "TROLL",
                    dash = Clip("Combat/sfx_enemy_troll_dash.wav"),
                    attack = Clip("Combat/sfx_enemy_troll_atk1.wav"),
                    attackHit = Clip("Combat/sfx_enemy_troll_atk_hit1.wav"),
                    hurt = Clip("Combat/sfx_enemy_troll_get_hit1.ogg"),
                    die = Clip("Combat/sfx_enemy_troll_die.ogg"),
                },
                new MonsterSounds
                {
                    monsterName = "TREANT",
                    dash = Clip("Combat/sfx_enemy_treant_atk_voice.ogg"), // ranged, so "dash" is its battle cry
                    attack = Clip("Combat/sfx_enemy_treant_atk_.wav"),
                    attackHit = Clip("Combat/sfx_enemy_treant_atk_hit1.wav"),
                    hurt = Clip("Combat/sfx_enemy_treant_get_hit_voice1.ogg"),
                    die = Clip("Combat/sfx_enemy_treant_die.ogg"),
                },
            };

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();

            var flow = Object.FindFirstObjectByType<GameFlowController>();
            if (flow != null)
            {
                var audio = flow.GetComponent<AudioManager>();
                if (audio == null) audio = Undo.AddComponent<AudioManager>(flow.gameObject);
                var so = new SerializedObject(audio);
                so.FindProperty("library").objectReferenceValue = lib;
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            }
            else
            {
                Debug.LogWarning("No GameFlowController in the scene, add AudioManager by hand");
            }

            Selection.activeObject = lib;
            Debug.Log("Sound library built");
        }

        static AudioClip Clip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + path);
            if (clip == null) Debug.LogWarning("Sound not found: " + path);
            return clip;
        }

        static AudioClip[] Clips(params string[] paths)
        {
            var clips = new AudioClip[paths.Length];
            for (int i = 0; i < paths.Length; i++) clips[i] = Clip(paths[i]);
            return clips;
        }
    }
}
