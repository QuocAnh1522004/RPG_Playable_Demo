using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RpgPlayable
{
    public enum FlowStep { Starting, SkillPick, DiceRoll, Moving, Battle, Ending }

    public class GameFlowController : MonoBehaviour
    {
        [SerializeField] StageConfig[] stages =
        {
            new StageConfig { label = "Small Monster", targetTileIndex = 3, roundsToKill = 1, monsterMaxHp = 120 },
            new StageConfig { label = "Elite Monster", targetTileIndex = 7, roundsToKill = 2, monsterMaxHp = 400 },
            new StageConfig { label = "Final Boss", targetTileIndex = 12, roundsToKill = 3, monsterMaxHp = 1200 },
        };
        [SerializeField] int startTileIndex = 0;

        [SerializeField] SkillLibrary skillLibrary;
        [SerializeField] StoreRedirect storeRedirect;

        // TODO: remove once the real systems are hooked up
        [SerializeField] float placeholderStepSeconds = 0.75f;

        public FlowStep CurrentStep { get; private set; }
        public int CurrentStageIndex { get; private set; }
        public event Action<FlowStep> StepChanged;

        readonly List<SkillData> ownedSkills = new List<SkillData>();
        public IList<SkillData> OwnedSkills { get { return ownedSkills; } }

        int currentTile;

        void Start()
        {
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            SetStep(FlowStep.Starting);
            currentTile = startTileIndex;

            for (int i = 0; i < stages.Length; i++)
            {
                CurrentStageIndex = i;
                StageConfig stage = stages[i];

                yield return PickSkill();
                yield return RollDice(stage.targetTileIndex - currentTile);
                yield return MoveHero(stage.targetTileIndex);
                currentTile = stage.targetTileIndex;
                yield return Battle(stage);
            }

            // last pick goes straight to the store
            yield return PickSkill();
            SetStep(FlowStep.Ending);
            if (storeRedirect != null)
            {
                storeRedirect.MarkGameEnded();
                storeRedirect.OpenStore();
            }
        }

        void SetStep(FlowStep step)
        {
            CurrentStep = step;
            Debug.Log("[Flow] stage " + (CurrentStageIndex + 1) + ": " + step);
            if (StepChanged != null) StepChanged(step);
        }

        IEnumerator PickSkill()
        {
            SetStep(FlowStep.SkillPick);
            // TODO: skill panel + tutorial hand
            if (skillLibrary != null)
            {
                List<SkillData> options = skillLibrary.PickRandom(3, ownedSkills);
                if (options.Count > 0)
                {
                    ownedSkills.Add(options[0]);
                    Debug.Log("[Flow] picked " + options[0].displayName);
                }
            }
            yield return new WaitForSeconds(placeholderStepSeconds);
        }

        IEnumerator RollDice(int result)
        {
            SetStep(FlowStep.DiceRoll);
            // TODO: dice
            Debug.Log("[Flow] dice = " + result);
            yield return new WaitForSeconds(placeholderStepSeconds);
        }

        IEnumerator MoveHero(int targetTile)
        {
            SetStep(FlowStep.Moving);
            // TODO: hero mover
            Debug.Log("[Flow] move " + currentTile + " -> " + targetTile);
            yield return new WaitForSeconds(placeholderStepSeconds);
        }

        IEnumerator Battle(StageConfig stage)
        {
            SetStep(FlowStep.Battle);
            // TODO: battle controller
            Debug.Log("[Flow] battle vs " + stage.label + ", dies in round " + stage.roundsToKill);
            yield return new WaitForSeconds(placeholderStepSeconds * 2f);
        }

        void OnValidate()
        {
            if (stages == null) return;

            int from = startTileIndex;
            for (int i = 0; i < stages.Length; i++)
            {
                int dice = stages[i].targetTileIndex - from;
                if (dice < 1 || dice > 6)
                    Debug.LogWarning("Stage " + (i + 1) + " (" + stages[i].label + ") needs a dice roll of " + dice + ", must be 1-6", this);
                if (stages[i].roundsToKill < 1)
                    Debug.LogWarning("Stage " + (i + 1) + ": roundsToKill must be at least 1", this);
                from = stages[i].targetTileIndex;
            }
        }
    }
}
