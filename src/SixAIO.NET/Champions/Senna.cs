using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Logic;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Models;
using System;
using System.Linq;
using Geometry = Oasys.SDK.Geometry;
using Orbwalker = Oasys.SDK.Orbwalker;
using TargetSelector = Oasys.Common.Logic.TargetSelector;
using DamageCalculator = Oasys.SDK.DamageCalculator;

namespace SixAIO.Champions
{
    internal class Senna : Champion
    {
        private bool _originalTargetChampsOnlySetting;
        public float PassiveStacks()
        {
            var passiveStacks = UnitManager.MyChampion.BuffManager.GetActiveBuff("SennaPassiveStacks");
            return passiveStacks is null ? 0 : passiveStacks.Stacks;
        }

        public Senna()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) =>
                {

                    var range = 600 + 25 * (PassiveStacks() % 20);
                    var targets = UnitManager.EnemyChampions.Where(x => x.IsAlive && x.Distance <= 1300 && TargetSelector.IsAttackable(x));
                    if (targets.Any(x => x.Distance <= range + x.UnitComponentInfo.UnitBoundingRadius))
                    {
                        return targets.FirstOrDefault(x => x.Distance <= range + x.UnitComponentInfo.UnitBoundingRadius);
                    }
                    if (!Orbwalker.TargetChampionsOnly)
                    {
                        foreach (var target in targets)
                        {
                            var targetMinion = GetMinionBetweenMeAndEnemy(target, 100);
                            if (targetMinion != null)
                            {
                                return targetMinion;
                            }
                        }
                    }

                    return targets.FirstOrDefault(x => x.Distance <= range + x.UnitComponentInfo.UnitBoundingRadius);
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 1300,
                Radius = () => 140,
                Speed = () => 1200,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 30000,
                Radius = () => 320,
                Speed = () => 20000,
                Delay = () => 1f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                                        ((125 + spellClass.Level * 125) +
                                        (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                                        (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.7f))
                            : 0,
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                ShouldCast = (mode, target, spellClass, damage) => target != null && target.Health < damage,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange && x.Health <= SpellR.Damage(x, SpellR.SpellClass)).FirstOrDefault()
            };
        }

        private GameObjectBase GetMinionBetweenMeAndEnemy(Hero enemy, int width)
        {
            var myPos = EB.Prediction.Position.PredictUnitPosition(UnitManager.MyChampion, 250);
            var myPosW2s = myPos.To3DWorld().ToW2S();
            var enemyPos = EB.Prediction.Position.PredictUnitPosition(enemy, 250);
            var enemyPosW2s = enemyPos.To3DWorld().ToW2S();
            return UnitManager.EnemyMinions.FirstOrDefault(minion => minion.IsAlive && minion.Distance <= 500 + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius &&
                        TargetSelector.IsAttackable(minion) &&
                        Geometry.DistanceFromPointToLine(enemyPosW2s, new Vector2[] { myPosW2s, minion.W2S }) <= width / 2 &&
                        minion.W2S.Distance(enemyPosW2s) < myPosW2s.Distance(enemyPosW2s));
        }

        private static void TargetSoulsWithOrbwalker()
        {
            var soul = UnitManager.EnemyJungleMobs.FirstOrDefault(x => x.ModelName == "SennaSoul" && TargetSelector.IsInRange(x));
            Orbwalker.SelectedTarget = soul;
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
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
        }

        internal override void OnCoreMainTick()
        {
            TargetSoulsWithOrbwalker();
        }

        private int RMinimumRange
        {
            get => RSettings.GetItem<Counter>("R minimum range").Value;
            set => RSettings.GetItem<Counter>("R minimum range").Value = value;
        }

        private int RMaximumRange
        {
            get => RSettings.GetItem<Counter>("R maximum range").Value;
            set => RSettings.GetItem<Counter>("R maximum range").Value = value;
        }

        internal override void InitializeMenu()
        {
            TabItem.OnTabItemChange += TabItem_OnTabItemChange;
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Senna)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 80, ValueFrequency = 10 });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R minimum range", MinValue = 0, MaxValue = 30_000, Value = 0, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 30_000, Value = 30_000, ValueFrequency = 50 });
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
