using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RpgPlayable.EditorTools
{
    // No parameters on purpose: code calls ActorAnimator.Play("Attack") and one-shots
    // fall back to idle by exit time. Re-running rebuilds in place so scene refs stay.
    public static class AnimatorBuilder
    {
        const string ClipDir = "Assets/PLA_TEST/Art/Animations/";
        const string OutDir = "Assets/_Game/Animators";
        const float ReturnFade = 0.1f;

        struct StateDef
        {
            public string state;
            public string clip;
            public string returnTo;

            public StateDef(string state, string clip, string returnTo)
            {
                this.state = state;
                this.clip = clip;
                this.returnTo = returnTo;
            }
        }

        static StateDef Stay(string state, string clip) { return new StateDef(state, clip, null); }
        static StateDef Once(string state, string clip, string returnTo) { return new StateDef(state, clip, returnTo); }

        [MenuItem("RPG Playable/Build Animator Controllers")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(OutDir))
            {
                Directory.CreateDirectory(OutDir);
                AssetDatabase.Refresh();
            }

            Build("Hero", "Idle",
                Stay("Idle", "hero1_sword_idle_inW"),
                Stay("IdleCombat", "hero1_sword_idle_inC"),
                Stay("Run", "hero1_sword_run_inW"),
                Once("Attack", "hero1_sword_atk", "IdleCombat"),
                Once("Hit", "hero1_sword_hit", "IdleCombat"),
                Once("Dash", "hero1_sword_dash", "IdleCombat"),
                Once("JumpBack", "hero1_sword_jumpback", "IdleCombat"));

            Build("Slime", "Idle",
                Stay("Idle", "slime_idle_inC"),
                Once("Attack", "slime_atk", "Idle"),
                Once("Hit", "slime_get_hit", "Idle"),
                Once("Dash", "slime_dash", "Idle"),
                Once("JumpBack", "slime_jumpback", "Idle"),
                Stay("Die", "slime_die"));

            Build("Troll", "Idle",
                Stay("Idle", "troll_idle_inC"),
                Once("Attack", "troll_atk", "Idle"),
                Once("Hit", "troll_hit", "Idle"),
                Once("Dash", "troll_dash", "Idle"),
                Once("JumpBack", "troll_jumpback", "Idle"),
                Stay("Die", "troll_die"));

            Build("Treant", "Idle",
                Stay("Idle", "treant_idle_inC"),
                Once("Attack", "treant_atk", "Idle"),
                Once("Hit", "treant_hit", "Idle"),
                Stay("Die", "treant_die"));

            AssetDatabase.SaveAssets();
            Debug.Log("Animator controllers built in " + OutDir);
        }

        static void Build(string actor, string defaultState, params StateDef[] defs)
        {
            string path = OutDir + "/" + actor + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in sm.states)
                sm.RemoveState(child.state);
            foreach (AnimatorStateTransition t in sm.anyStateTransitions)
                sm.RemoveAnyStateTransition(t);
            foreach (AnimatorControllerParameter p in controller.parameters)
                controller.RemoveParameter(p);

            var states = new AnimatorState[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDir + defs[i].clip + ".anim");
                if (clip == null)
                    Debug.LogWarning(actor + ": missing clip " + defs[i].clip);

                var pos = new Vector3(300 + (i % 3) * 220, 60 + (i / 3) * 90, 0);
                states[i] = sm.AddState(defs[i].state, pos);
                states[i].motion = clip;
                if (defs[i].state == defaultState) sm.defaultState = states[i];
            }

            for (int i = 0; i < defs.Length; i++)
            {
                if (string.IsNullOrEmpty(defs[i].returnTo)) continue;

                AnimatorState target = Find(states, defs[i].returnTo);
                if (target == null)
                {
                    Debug.LogWarning(actor + ": missing return state " + defs[i].returnTo);
                    continue;
                }

                var clip = states[i].motion as AnimationClip;
                float length = clip != null ? Mathf.Max(clip.length, 0.01f) : 1f;

                AnimatorStateTransition tr = states[i].AddTransition(target);
                tr.hasExitTime = true;
                tr.hasFixedDuration = true;
                tr.duration = ReturnFade;
                tr.exitTime = Mathf.Clamp01(1f - ReturnFade / length);
            }

            EditorUtility.SetDirty(controller);
        }

        static AnimatorState Find(AnimatorState[] states, string name)
        {
            foreach (var s in states)
                if (s.name == name) return s;
            return null;
        }
    }
}
