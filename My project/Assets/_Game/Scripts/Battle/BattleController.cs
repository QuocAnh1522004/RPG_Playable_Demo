using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace RpgPlayable
{
    // Scripted auto-battle. The fight always ends in stage.roundsToKill rounds: the monster's HP is
    // planned per round (maxHp * (1 - round / rounds)) and that chunk is split over the hero's hits,
    // so whatever skills were picked only change how the fight looks, not how long it lasts.
    public class BattleController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] Camera boardCamera;
        [SerializeField] Camera battleCamera;
        [SerializeField] Transform heroSpot;
        [SerializeField] Transform monsterSpot;
        [SerializeField] ActorAnimator hero;
        [SerializeField] HeroStats heroStats;
        [SerializeField] SkillLibrary skillLibrary;

        [Header("UI")]
        [SerializeField] GameObject ui;
        [SerializeField] HpBar heroBar;
        [SerializeField] HpBar monsterBar;
        [SerializeField] DamagePopups popups;
        [SerializeField] BattleBanner banner;
        [SerializeField] SkillTray skillTray;

        [Header("Feel")]
        [SerializeField] float hitHeight = 1f;
        [SerializeField] float meleeDistance = 1.2f;
        [SerializeField] float projectileTime = 0.35f;
        [SerializeField] float projectileStagger = 0.15f;
        [SerializeField] float fxScale = 1f;
        [SerializeField] float actionGap = 0.25f;

        class Hit
        {
            public int damage;
            public float weight;
            public bool crit;
            public StatusEffect effect;
            public SkillData source;
            public Hit proc;
        }

        class HeroAction
        {
            public SkillData skill;
            public bool melee;
            public readonly List<Hit> hits = new List<Hit>();
        }

        ActorAnimator monster;
        Transform monsterTf;
        StageConfig stage;
        int monsterHp;

        void Awake()
        {
            ui.SetActive(false);
            battleCamera.enabled = false;
        }

        public IEnumerator Run(StageConfig stage, IList<SkillData> owned)
        {
            Enter(stage, owned);
            yield return banner.Play(stage.monsterName);

            int rounds = Mathf.Max(1, stage.roundsToKill);
            for (int round = 1; round <= rounds; round++)
            {
                int hpAfter = round == rounds ? 0 : Mathf.RoundToInt(stage.monsterMaxHp * (1f - (float)round / rounds));

                List<HeroAction> plan = PlanHeroTurn(round, owned);
                SplitDamage(plan, monsterHp - hpAfter);

                foreach (HeroAction action in plan)
                {
                    yield return action.melee ? Melee(action) : Projectiles(action);
                    yield return new WaitForSeconds(actionGap);
                }

                if (round == rounds) break; // dies on the hero's turn, no counter attack
                yield return MonsterTurn();
                yield return new WaitForSeconds(actionGap);
            }

            monster.Play(ActorAnimator.Die);
            yield return new WaitForSeconds(1.2f);
            Exit();
        }

        void Enter(StageConfig stage, IList<SkillData> owned)
        {
            this.stage = stage;
            boardCamera.enabled = false;
            battleCamera.enabled = true;
            ui.SetActive(true);

            GameObject m = Instantiate(stage.battlePrefab, monsterSpot);
            monsterTf = m.transform;
            monsterTf.localPosition = Vector3.zero;
            monsterTf.localRotation = Quaternion.identity;
            monsterTf.localScale *= stage.battleScale;

            monster = m.GetComponent<ActorAnimator>();
            if (monster == null) monster = m.AddComponent<ActorAnimator>();
            monster.Play(ActorAnimator.Idle, 0f);

            hero.transform.localPosition = Vector3.zero;
            hero.Play(ActorAnimator.IdleCombat, 0f);

            monsterHp = stage.monsterMaxHp;
            monsterBar.Follow(monsterTf, battleCamera);
            monsterBar.Set(monsterHp, stage.monsterMaxHp, true);
            heroBar.Follow(hero.transform, battleCamera);
            heroBar.Set(heroStats.Hp, heroStats.MaxHp, true);
            skillTray.Show(owned);
        }

        void Exit()
        {
            Destroy(monsterTf.gameObject);
            if (stage.monster != null) stage.monster.SetActive(false);

            ui.SetActive(false);
            battleCamera.enabled = false;
            boardCamera.enabled = true;
        }


        List<HeroAction> PlanHeroTurn(int round, IList<SkillData> owned)
        {
            var actives = new List<SkillData>();
            var basicMods = new List<SkillData>();
            foreach (SkillData s in owned)
            {
                if (s.kind == SkillKind.Active) actives.Add(s);
                else if (s.kind == SkillKind.BasicAttackModifier) basicMods.Add(s);
            }
            actives.Add(skillLibrary.basicAttack);
            actives.Sort((a, b) => a.priority.CompareTo(b.priority));

            var plan = new List<HeroAction>();
            foreach (SkillData skill in actives)
            {
                if (!skill.TriggersOnRound(round)) continue;

                var action = new HeroAction { skill = skill, melee = skill == skillLibrary.basicAttack };
                for (int i = 0; i < skill.projectileCount; i++)
                {
                    var hit = new Hit { weight = action.melee ? 1.6f : 1f, source = skill };

                    if (action.melee)
                    {
                        hit.crit = Random.value < heroStats.CritChance;
                        if (hit.crit) hit.weight *= 1.8f;
                        foreach (SkillData mod in basicMods)
                            if (Random.value < mod.effectChance) hit.proc = Proc(mod);
                    }
                    else if (skill.effect != StatusEffect.None && Random.value < skill.effectChance)
                    {
                        hit.proc = Proc(skill);
                    }

                    action.hits.Add(hit);
                }
                plan.Add(action);
            }
            return plan;
        }

        static Hit Proc(SkillData source)
        {
            return new Hit { effect = source.effect, source = source, weight = 0.4f };
        }

        // splits this round's HP chunk over every hit in order, the last hit takes whatever is left
        static void SplitDamage(List<HeroAction> plan, int total)
        {
            var all = new List<Hit>();
            foreach (HeroAction a in plan)
            {
                foreach (Hit h in a.hits)
                {
                    all.Add(h);
                    if (h.proc != null) all.Add(h.proc);
                }
            }
            if (all.Count == 0) return;

            float sum = 0f;
            foreach (Hit h in all)
            {
                h.weight *= Random.Range(0.85f, 1.15f);
                sum += h.weight;
            }

            int left = total;
            for (int i = 0; i < all.Count; i++)
            {
                int remainingHits = all.Count - 1 - i;
                int dmg = remainingHits == 0 ? left : Mathf.RoundToInt(total * all[i].weight / sum);
                dmg = Mathf.Clamp(dmg, 0, Mathf.Max(0, left - remainingHits));
                all[i].damage = dmg;
                left -= dmg;
            }
        }


        IEnumerator Melee(HeroAction action)
        {
            Transform heroTf = hero.transform;
            Vector3 home = heroTf.position;
            Vector3 front = monsterTf.position + (home - monsterTf.position).normalized * meleeDistance;

            hero.Play(ActorAnimator.Dash);
            yield return TweenWait.For(heroTf.DOMove(front, 0.2f).SetEase(Ease.OutQuad));

            foreach (Hit hit in action.hits)
            {
                hero.Play(ActorAnimator.Attack);
                yield return new WaitForSeconds(0.25f);
                FxSpawner.Spawn(action.skill.castFx, MonsterChest(), heroTf.rotation, 1f, fxScale);
                ApplyHit(hit);
                yield return new WaitForSeconds(0.3f);
            }

            hero.Play(ActorAnimator.JumpBack);
            yield return TweenWait.For(heroTf.DOMove(home, 0.3f).SetEase(Ease.OutQuad));
            hero.Play(ActorAnimator.IdleCombat);
        }

        IEnumerator Projectiles(HeroAction action)
        {
            for (int i = 0; i < action.hits.Count; i++)
            {
                StartCoroutine(Fly(action.skill, action.hits[i], i, action.hits.Count));
                yield return new WaitForSeconds(projectileStagger);
            }
            yield return new WaitForSeconds(projectileTime + 0.1f);
        }

        IEnumerator Fly(SkillData skill, Hit hit, int index, int count)
        {
            Vector3 target = MonsterChest();
            Vector3 start = HeroChest();
            if (skill.fromAbove)
            {
                float spread = (index - (count - 1) * 0.5f) * 0.8f;
                start = target + new Vector3(spread, 6f, 0f);
            }

            GameObject p = FxSpawner.Spawn(skill.castFx, start, Quaternion.LookRotation(target - start), 0f, fxScale);
            if (p != null)
            {
                yield return TweenWait.For(p.transform.DOMove(target, projectileTime).SetEase(Ease.InQuad));
                Destroy(p);
            }
            else
            {
                yield return new WaitForSeconds(projectileTime);
            }

            ApplyHit(hit);
        }

        void ApplyHit(Hit hit)
        {
            FxSpawner.Spawn(hit.source.hitFx, MonsterChest(), Quaternion.identity, 1.5f, fxScale);
            DamageMonster(hit.damage, hit.crit ? DamageStyle.Crit : DamageStyle.Normal);

            if (hit.proc != null)
            {
                FxSpawner.Spawn(hit.proc.source.effectFx, MonsterChest(), Quaternion.identity, 1.5f, fxScale);
                DamageMonster(hit.proc.damage, StyleFor(hit.proc.effect));
            }
        }

        void DamageMonster(int amount, DamageStyle style)
        {
            if (amount <= 0) return;

            monsterHp = Mathf.Max(0, monsterHp - amount);
            monsterBar.Set(monsterHp, stage.monsterMaxHp, false);
            popups.Show(amount, style, MonsterChest() + Vector3.up);
            if (monsterHp > 0) monster.Play(ActorAnimator.Hit);
        }

        static DamageStyle StyleFor(StatusEffect effect)
        {
            switch (effect)
            {
                case StatusEffect.Burn: return DamageStyle.Burn;
                case StatusEffect.Poison: return DamageStyle.Poison;
                case StatusEffect.Lightning: return DamageStyle.Lightning;
                default: return DamageStyle.Normal;
            }
        }


        IEnumerator MonsterTurn()
        {
            int damage = Mathf.Max(5, stage.monsterAttack - heroStats.Def);

            if (stage.ranged)
            {
                monster.Play(ActorAnimator.Attack);
                yield return new WaitForSeconds(0.35f);

                GameObject p = FxSpawner.Spawn(stage.battleProjectile, MonsterChest(), Quaternion.identity, 0f, 1f);
                if (p != null)
                {
                    yield return TweenWait.For(p.transform.DOJump(HeroChest(), 1.2f, 1, 0.45f));
                    Destroy(p);
                }
                HitHero(damage);
                yield return new WaitForSeconds(0.4f);
            }
            else
            {
                Vector3 home = monsterTf.position;
                Vector3 front = hero.transform.position + (home - hero.transform.position).normalized * meleeDistance;

                monster.Play(ActorAnimator.Dash);
                yield return TweenWait.For(monsterTf.DOMove(front, 0.2f).SetEase(Ease.OutQuad));
                monster.Play(ActorAnimator.Attack);
                yield return new WaitForSeconds(0.3f);
                HitHero(damage);
                yield return new WaitForSeconds(0.3f);
                yield return TweenWait.For(monsterTf.DOMove(home, 0.25f).SetEase(Ease.OutQuad));
                monster.Play(ActorAnimator.Idle);
            }
        }

        void HitHero(int damage)
        {
            FxSpawner.Spawn(stage.monsterHitFx, HeroChest(), Quaternion.identity, 1.5f, fxScale);
            heroStats.TakeDamage(damage);
            heroBar.Set(heroStats.Hp, heroStats.MaxHp, false);
            popups.Show(damage, DamageStyle.Normal, HeroChest() + Vector3.up);
            hero.Play(ActorAnimator.Hit);
        }

        Vector3 HeroChest()
        {
            return hero.transform.position + Vector3.up * hitHeight;
        }

        Vector3 MonsterChest()
        {
            return monsterTf.position + Vector3.up * hitHeight * stage.battleScale;
        }
    }
}
