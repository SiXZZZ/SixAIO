using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        private static bool AllSpellsOnCooldown()
        {
            var q = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q);
            var w = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W);
            var e = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E);
            return !q.IsSpellReady && !w.IsSpellReady && !e.IsSpellReady;
        }

        public Yasuo()
        {
            SDKSpell.OnSpellCast += Spell_OnSpellCast;
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Speed = () => 1200,
                Radius = () => GetQState() == 3 ? 180 : 80,
                Range = () => GetQState() == 3 ? 1150 : 450,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            target != null &&
                            (target.IsObject(ObjectTypeFlag.AIHeroClient) || GetQState() < 3),
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
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            target != null,
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
                            if (!Orbwalker.TargetChampionsOnly)
                            {
                                foreach (var target in enemyChamps)
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
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            target != null,
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 1400 && TargetSelector.IsAttackable(x) && BuffChecker.IsKnockedUpOrBack(x))
            };
        }


        private GameObjectBase GetMinionBetweenMeAndEnemy(IEnumerable<GameObjectBase> targets, Hero enemy, int width, int distance)
        {
            return targets.FirstOrDefault(minion => minion.IsAlive && minion.Distance <= distance && TargetSelector.IsAttackable(minion) &&
                        Geometry.DistanceFromPointToLine(enemy.W2S, new Vector2[] { UnitManager.MyChampion.W2S, minion.W2S }) <= width &&
                        minion.W2S.Distance(enemy.W2S) < UnitManager.MyChampion.W2S.Distance(enemy.W2S));
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

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.E && (UnitManager.EnemyChampions.Any(x => x.Distance <= UnitManager.MyChampion.AttackRange && TargetSelector.IsAttackable(x)) || GetQState() < 3))
            {
                SpellQ.ExecuteCastSpell();
                SpellR.ExecuteCastSpell();
            }

            if (spell.SpellSlot == SpellSlot.Q)
            {
                SpellR.ExecuteCastSpell();
            }
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            SpellQ.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
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
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear) || SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            TabItem.OnTabItemChange += TabItem_OnTabItemChange;
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Yasuo)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            SetTargetChampsOnly();
        }

        private void SetTargetChampsOnly()
        {
            try
            {
                var orbTab = MenuManagerProvider.GetTab("Orbwalker Input");
                _originalTargetChampsOnlySetting = orbTab.GetItem<Switch>("Hold Target Champs Only").IsOn;
                orbTab.GetItem<Switch>("Hold Target Champs Only").IsOn = false;
            }
            catch (Exception ex)
            {
                //Logger.Log(ex.Message);
            }
        }

        private void TabItem_OnTabItemChange(string tabName, TabItem tabItem)
        {
            if (tabItem.TabName == "Orbwalker Input" &&
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
                MenuManagerProvider
                    .GetTab("Orbwalker Input")
                    .GetItem<Switch>("Hold Target Champs Only")
                    .IsOn = _originalTargetChampsOnlySetting;
            }
            catch (Exception ex)
            {
                //Logger.Log(ex.Message);
            }
        }
    }
}
