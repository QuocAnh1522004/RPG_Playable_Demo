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
        [SerializeField] Dice dice;
        [SerializeField] HeroMover heroMover;
        [SerializeField] SkillPanel skillPanel;
        [SerializeField] TutorialHand tutorialHand;
        [SerializeField] BattleController battle;
        [SerializeField] HeroStats heroStats;
        [SerializeField] GameObject upgradeFx; // hero_spawn_fx, plays on the board hero after each pick

        public FlowStep CurrentStep { get; private set; }
        public int CurrentStageIndex { get; private set; }
        public event Action<FlowStep> StepChanged;

        readonly List<SkillData> ownedSkills = new List<SkillData>();
        public IList<SkillData> OwnedSkills { get { return ownedSkills; } }

        void Awake()
        {
            // before CameraFollow.Start reads the hero position
            heroMover.PlaceAt(startTileIndex);
        }

        void Start()
        {
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            SetStep(FlowStep.Starting);

            for (int i = 0; i < stages.Length; i++)
            {
                CurrentStageIndex = i;
                StageConfig stage = stages[i];

                yield return PickSkill(); //stage %1
                yield return RollDice(stage.targetTileIndex - heroMover.CurrentTile); //stage %2
                yield return MoveHero(stage.targetTileIndex);//stage% 2
                yield return Battle(stage);//stage %3
            }

            // last pick goes straight to the store, opened inside the tap so it counts as a user gesture
            yield return PickSkill(OpenStoreOnPick);//stage 10
            SetStep(FlowStep.Ending);
            yield return null; // Luna, see TweenWait
        }

        void OpenStoreOnPick()
        {
            storeRedirect.MarkGameEnded();
            storeRedirect.OpenStore();
        }

        void SetStep(FlowStep step)
        {
            CurrentStep = step;
            Debug.Log("[Flow] stage " + (CurrentStageIndex + 1) + ": " + step);
            if (StepChanged != null) StepChanged(step);
        }

        IEnumerator PickSkill(Action onPicked = null)
        {
            SetStep(FlowStep.SkillPick);

            List<SkillData> options = skillLibrary.PickRandom(3, ownedSkills);
            yield return skillPanel.ShowAndWaitForPick(options, onPicked);

            if (skillPanel.Picked != null)
            {
                ownedSkills.Add(skillPanel.Picked);
                heroStats.Recalculate(ownedSkills);
                Debug.Log("[Flow] picked " + skillPanel.Picked.displayName);
            }

            // power-up burst on the hero, the next step (hand on the dice) starts while it plays
            FxSpawner.Spawn(upgradeFx, heroMover.transform.position, Quaternion.identity, 3f, 1f);
            AudioManager.Play(AudioManager.Sounds.upgrade, 0.7f);
            yield return null; // Luna, see TweenWait
        }

        IEnumerator RollDice(int result)
        {
            SetStep(FlowStep.DiceRoll);
            tutorialHand.PointAt(dice.AnchorPosition);
            yield return dice.WaitForTapAndRoll(result, tutorialHand.Hide);
            yield return null; // Luna, see TweenWait
        }

        IEnumerator MoveHero(int targetTile)
        {
            SetStep(FlowStep.Moving);
            yield return heroMover.MoveTo(targetTile);
            yield return null; // Luna, see TweenWait
        }

        IEnumerator Battle(StageConfig stage)
        {
            SetStep(FlowStep.Battle);
            yield return battle.Run(stage, ownedSkills);
            heroMover.ClearCurrentTile(); // the beaten monster shrinks away on the board
            yield return null; // Luna, see TweenWait
        }

        void OnValidate()
        {
            if (stages == null) return;

            int from = startTileIndex;
            for (int i = 0; i < stages.Length; i++)
            {
                int roll = stages[i].targetTileIndex - from;
                if (roll < 1 || roll > 6)
                    Debug.LogWarning("Stage " + (i + 1) + " (" + stages[i].label + ") needs a dice roll of " + roll + ", must be 1-6", this);
                if (stages[i].roundsToKill < 1)
                    Debug.LogWarning("Stage " + (i + 1) + ": roundsToKill must be at least 1", this);
                from = stages[i].targetTileIndex;
            }
        }
    }
}
