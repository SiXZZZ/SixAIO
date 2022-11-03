using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static Oasys.Common.Logic.Orbwalker;

namespace SixAIO.Champions
{
    internal sealed class Yasuo : Champion
    {
        private bool _originalTargetChampsOnlySetting;

        private static int GetQState() => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).SpellData.SpellName switch
        {
            "YasuoQ1Wrapper" => 1,
            "YasuoQ2Wrapper" => 2,
            "YasuoQ3Wrapper" => 3,
            _ => 1
        };

        public Yasuo()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Speed = () => 1200,
                Radius = () => GetQState() == 3 ? 180 : 80,
                Range = () => UnitManager.MyChampion.AIManager.IsDashing ? 250 : GetQState() == 3 ? 1150 : 450,
                From = () => UnitManager.MyChampion.AIManager.IsDashing ? UnitManager.MyChampion.AIManager.NavEndPosition : UnitManager.MyChampion.AIManager.ServerPosition,
                IsEnabled = () => UseQ,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target,
                                (-5 + spellClass.Level * 25) +
                                (UnitManager.MyChampion.UnitStats.TotalAttackDamage * 1.05f))
                            : 0,
                ShouldCast = (mode, target, spellClass, damage) => target != null || (UnitManager.MyChampion.AIManager.IsDashing && (UnitManager.EnemyChampions.Any(x => SpellQ.From().Distance(x.Position) <= SpellQ.Range() && TargetSelector.IsAttackable(x)) || GetQState() < 3)),
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LastHit)
                    {
                        return SpellQ.GetTargets(mode, x => x.Health <= SpellQ.Damage(x, SpellQ.SpellClass)).FirstOrDefault();
                    }
                    else
                    {
                        var champ = UnitManager.EnemyChampions.FirstOrDefault(x => SpellQ.From().Distance(x.Position) <= SpellQ.Range() && TargetSelector.IsAttackable(x));
                        if (champ != null)
                        {
                            return champ;
                        }
                        if (GetQState() < 3)
                        {
                            var minion = UnitManager.EnemyMinions.FirstOrDefault(x => SpellQ.From().Distance(x.Position) <= SpellQ.Range() && TargetSelector.IsAttackable(x));
                            if (minion != null)
                            {
                                return minion;
                            }

                            var jungle = UnitManager.EnemyJungleMobs.FirstOrDefault(x => SpellQ.From().Distance(x.Position) <= SpellQ.Range() && TargetSelector.IsAttackable(x));
                            if (jungle != null)
                            {
                                return jungle;
                            }
                        }
                    }

                    return null;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => target != null && !UnitManager.EnemyChampions.Any(x => TargetSelector.IsInRange(x) && TargetSelector.IsAttackable(x)),
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.Combo)
                    {
                        var champ = UnitManager.EnemyChampions.FirstOrDefault(CanEOnTarget);
                        if (champ != null)
                        {
                            return champ;
                        }
                        else
                        {
                            var enemyChamps = UnitManager.EnemyChampions.Where(x => CanEOnTarget(x) && x.IsAlive && x.Distance <= 800 && TargetSelector.IsAttackable(x));
                            if (enemyChamps.Any(x => x.Distance <= 450))
                            {
                                return enemyChamps.FirstOrDefault(x => x.Distance <= 450);
                            }
                            else
                            {
                                foreach (var target in UnitManager.EnemyChampions.Where(x => x.Distance <= EChaseRange && TargetSelector.IsAttackable(x)))
                                {
                                    var targetMinion = GetMinionBetweenMeAndEnemy(UnitManager.EnemyMinions.Where(CanEOnTarget), target, 450);
                                    if (targetMinion != null)
                                    {
                                        return targetMinion;
                                    }

                                    var targetJungle = GetMinionBetweenMeAndEnemy(UnitManager.EnemyJungleMobs.Where(CanEOnTarget), target, 450);
                                    if (targetJungle != null)
                                    {
                                        return targetJungle;
                                    }
                                }
                            }
                        }
                    }

                    var minion = UnitManager.EnemyMinions.FirstOrDefault(x => CanEOnTarget(x) && x.Health <= GetEDamage(x, UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E)));
                    if (minion != null)
                    {
                        return minion;
                    }

                    var jungle = UnitManager.EnemyJungleMobs.FirstOrDefault(CanEOnTarget);
                    if (jungle != null)
                    {
                        return jungle;
                    }

                    return null;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) =>
                    RUltEnemies <= UnitManager.EnemyChampions.Count(x => x.Distance <= 1400 && TargetSelector.IsAttackable(x) && BuffChecker.IsKnockedUpOrBack(x))
            };
        }

        private static readonly Vector3 _orderNexusPos = new Vector3(405, 95, 425);
        private static readonly Vector3 _chaosNexusPos = new Vector3(14300, 90, 14400);

        private bool ShouldE(GameObjectBase target)
        {
            var targetPos = target.Position;
            var myPos = UnitManager.MyChampion.Position;
            var endPos = myPos.Extend(targetPos, 500);
            return AllowEInTowerRange ||
                (UnitManager.EnemyTowers.Where(x => x.IsAlive).All(x => x.Position.Distance(endPos) >= 850) &&
                target.Position.Distance(_orderNexusPos) >= 1000 &&
                target.Position.Distance(_chaosNexusPos) >= 1000);
        }

        private GameObjectBase GetMinionBetweenMeAndEnemy(IEnumerable<GameObjectBase> targets, Hero enemy, int distance)
        {
            return targets.FirstOrDefault(minion =>
                        minion.IsAlive &&
                        minion.Distance <= distance &&
                        TargetSelector.IsAttackable(minion) &&
                        minion.DistanceTo(enemy.Position) <= EChaseRange &&
                        minion.DistanceTo(enemy.Position) < enemy.Distance &&
                        UnitManager.MyChampion.Position.Extend(minion.Position, 500).Distance(enemy.Position) < enemy.Distance);
        }

        private float GetMissingHealthPercent(GameObjectBase target)
        {
            var missingHealthPercent = 100f - (target.Health / target.MaxHealth * 100f);
            return missingHealthPercent;
        }

        private float GetRDamage(GameObjectBase target, SpellClass spellClass)
        {
            if (target == null)
            {
                return 0;
            }
            var extraDamagePercent = GetMissingHealthPercent(target) * 2.667f;
            if (extraDamagePercent > 200f)
            {
                extraDamagePercent = 200f;
            }
            return DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) * ((1 + (extraDamagePercent / 100f)) *
                   ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.60f) + 50 + 50 * spellClass.Level));
        }

        private bool CanEOnTarget(GameObjectBase target)
        {
            if (!ShouldE(target))
            {
                return false;
            }

            if (target.IsAlive && target.Distance <= 450 && TargetSelector.IsAttackable(target))
            {
                var eBuff = target.BuffManager.GetBuffByName("YasuoE", false, true);
                return eBuff == null || !eBuff.IsActive || eBuff.Stacks < 1;
            }
            return false;
        }

        private float GetEDamage(GameObjectBase target, SpellClass spellClass)
        {
            if (target == null)
            {
                return 0;
            }
            return DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                   ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.20f) + (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.60f) + 50 + 10 * spellClass.Level);
        }

        internal override void OnCoreMainInput()
        {
            if (Orbwalker.TargetChampionsOnly && SpellE.CanExecuteCastSpell())
            {
                var tempTargetChamps = OrbSettings.TargetChampionsOnly;
                OrbSettings.TargetChampionsOnly = false;
                var casted = SpellE.ExecuteCastSpell();
                OrbSettings.TargetChampionsOnly = tempTargetChamps;
                if (casted)
                {
                    return;
                }
            }
            else
            {
                if (SpellE.ExecuteCastSpell())
                {
                    return;
                }
            }

            if (SpellR.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear) || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
            if (UseELaneclear && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (UseQLasthit && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                return;
            }
        }

        internal bool AllowEInTowerRange
        {
            get => ESettings.GetItem<Switch>("Allow E in tower range").IsOn;
            set => ESettings.GetItem<Switch>("Allow E in tower range").IsOn = value;
        }

        private int EChaseRange
        {
            get => ESettings.GetItem<Counter>("E Chase Range").Value;
            set => ESettings.GetItem<Counter>("E Chase Range").Value = value;
        }

        private int RUltEnemies
        {
            get => RSettings.GetItem<Counter>("R Ult enemies").Value;
            set => RSettings.GetItem<Counter>("R Ult enemies").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Yasuo)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Lasthit", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Allow E in tower range", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "E Chase Range", MinValue = 450, MaxValue = 2500, Value = 1500, ValueFrequency = 50 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Ult enemies", MinValue = 1, MaxValue = 5, Value = 1, ValueFrequency = 1 });

        }
    }
}
