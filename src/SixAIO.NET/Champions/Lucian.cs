using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Lucian : Champion
    {
        private bool _originalTargetChampsOnlySetting;

        public Lucian()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) =>
                {
                    var targets = UnitManager.EnemyChampions
                                                .Where(x => x.IsAlive && x.Distance <= 1000 &&
                                                            TargetSelector.IsAttackable(x) &&
                                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false));
                    if (targets.Any(x => x.Distance <= 550))
                    {
                        return targets.FirstOrDefault(x => x.Distance <= 550);
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

                    return targets.FirstOrDefault(x => x.Distance <= 550);
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 900,
                Radius = () => 110,
                Speed = () => 1600,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
        }

        private GameObjectBase GetMinionBetweenMeAndEnemy(Hero enemy, int width)
        {
            return UnitManager.EnemyMinions.FirstOrDefault(minion => minion.IsAlive && minion.Distance <= 500 && TargetSelector.IsAttackable(minion) &&
                        Geometry.DistanceFromPointToLine(enemy.W2S, new Vector2[] { UnitManager.MyChampion.W2S, minion.W2S }) <= width &&
                        minion.W2S.Distance(enemy.W2S) < UnitManager.MyChampion.W2S.Distance(enemy.W2S));
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), MenuTab.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        internal override void InitializeMenu()
        {
            TabItem.OnTabItemChange += TabItem_OnTabItemChange;
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Lucian)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = false });
            MenuTab.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });
            //MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
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
