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

namespace SixAIO.Champions
{
    internal class Yasuo : Champion
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
                Range = () => UnitManager.MyChampion.AIManager.IsDashing ? 200 : GetQState() == 3 ? 1150 : 450,
                From = () => UnitManager.MyChampion.AIManager.IsDashing ? UnitManager.MyChampion.AIManager.NavEndPosition : UnitManager.MyChampion.AIManager.ServerPosition,
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) => target != null && (target.IsObject(ObjectTypeFlag.AIHeroClient) || GetQState() < 3),
                TargetSelect = (mode) =>
                {
                    var champ = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(x));
                    if (champ != null)
                    {
                        return champ;
                    }
                    if (GetQState() < 3)
                    {
                        var minion = UnitManager.EnemyMinions.FirstOrDefault(x => x.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(x));
                        if (minion != null)
                        {
                            return minion;
                        }

                        var jungle = UnitManager.EnemyJungleMobs.FirstOrDefault(x => x.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(x));
                        if (jungle != null)
                        {
                            return jungle;
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
                            else if (!Orbwalker.TargetChampionsOnly)
                            {
                                foreach (var target in UnitManager.EnemyChampions.Where(x => x.IsAlive && x.Distance <= 1500))
                                {
                                    var targetMinion = GetMinionBetweenMeAndEnemy(UnitManager.EnemyMinions.Where(CanEOnTarget), target, 600, 450);
                                    if (targetMinion != null)
                                    {
                                        return targetMinion;
                                    }

                                    var targetJungle = GetMinionBetweenMeAndEnemy(UnitManager.EnemyJungleMobs.Where(CanEOnTarget), target, 600, 450);
                                    if (targetJungle != null)
                                    {
                                        return targetJungle;
                                    }
                                }
                            }
                        }
                    }
                    if (!Orbwalker.TargetChampionsOnly)
                    {
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

        private GameObjectBase GetMinionBetweenMeAndEnemy(IEnumerable<GameObjectBase> targets, Hero enemy, int width, int distance)
        {
            return targets.FirstOrDefault(minion =>
                        minion.IsAlive && minion.Distance <= distance &&
                        TargetSelector.IsAttackable(minion) &&
                        Geometry.DistanceFromPointToLine(enemy.W2S, new Vector2[] { UnitManager.MyChampion.W2S, enemy.W2S }) <= width &&
                        minion.Distance(enemy) < UnitManager.MyChampion.Distance(enemy));
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
            if (SpellR.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
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

        private int RUltEnemies
        {
            get => RSettings.GetItem<Counter>("R Ult enemies").Value;
            set => RSettings.GetItem<Counter>("R Ult enemies").Value = value;
        }

        internal override void InitializeMenu()
        {
            TabItem.OnTabItemChange += TabItem_OnTabItemChange;
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Yasuo)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Ult enemies", MinValue = 1, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            SetTargetChampsOnly();
        }

        private void SetTargetChampsOnly()
        {
            try
            {
                var orbTab = MenuManagerProvider.GetTab("Orbwalker");
                var orbGroup = orbTab.GetGroup("Input");
                _originalTargetChampsOnlySetting = orbGroup.GetItem<Switch>("Hold Target Champs Only").IsOn;
                orbGroup.GetItem<Switch>("Hold Target Champs Only").IsOn = false;
            }
            catch (Exception ex)
            {
                //Logger.Log(ex.Message);
            }
        }

        private void TabItem_OnTabItemChange(string tabName, TabItem tabItem)
        {
            if (tabItem.TabName == "Orbwalker" &&
                tabItem.GroupName == "Input" &&
                tabItem.Title == "Hold Target Champs Only" &&
                tabItem is Switch itemSwitch &&
                itemSwitch.IsOn)
            {
                SetTargetChampsOnly();
            }
        }

        internal override void OnGameMatchComplete()
        {
            try
            {
                TabItem.OnTabItemChange -= TabItem_OnTabItemChange;

                var orbTab = MenuManagerProvider.GetTab("Orbwalker");
                var orbGroup = orbTab.GetGroup("Input");
                orbGroup.GetItem<Switch>("Hold Target Champs Only")
                        .IsOn = _originalTargetChampsOnlySetting;
            }
            catch (Exception ex)
            {
                //Logger.Log(ex.Message);
            }
        }
    }
}
