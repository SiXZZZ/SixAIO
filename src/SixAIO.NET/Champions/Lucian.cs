using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal class Lucian : Champion
    {
        private bool _originalTargetChampsOnlySetting;
        private static bool IsPassiveActive
        {
            get
            {
                var buff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name.Equals("LucianPassiveBuff", StringComparison.OrdinalIgnoreCase));
                return buff != null && buff.IsActive && buff.Stacks >= 1;
            }
        }

        private static bool AllSpellsOnCooldown()
        {
            var q = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q);
            var w = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W);
            var e = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E);
            return !q.IsSpellReady && !w.IsSpellReady && !e.IsSpellReady;
        }

        private static bool QEOnCooldown()
        {
            var q = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q);
            var e = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E);
            return !q.IsSpellReady && !e.IsSpellReady;
        }

        public Lucian()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseQ && !IsPassiveActive && !IsUltActive() && !UnitManager.MyChampion.AIManager.IsDashing,
                TargetSelect = (mode) =>
                {
                    var range = 500 + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius;
                    var targets = UnitManager.EnemyChampions.Where(x => x.IsAlive && x.Distance <= 1000 && TargetSelector.IsAttackable(x));
                    if (targets.Any(x => x.Distance <= range + x.UnitComponentInfo.UnitBoundingRadius))
                    {
                        return targets.FirstOrDefault(x => x.Distance <= range + x.UnitComponentInfo.UnitBoundingRadius);
                    }
                    if (!Orbwalker.TargetChampionsOnly)
                    {
                        foreach (var target in targets)
                        {
                            var targetMinion = GetMinionBetweenMeAndEnemy(target, 120);
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
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 900,
                Radius = () => 110,
                Speed = () => 1600,
                IsEnabled = () => UseW && !IsPassiveActive && !IsUltActive() && !UnitManager.MyChampion.AIManager.IsDashing && (!OnlyWIfQENotReady || QEOnCooldown()),
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE && !IsPassiveActive && !IsUltActive(),
                ShouldCast = (mode, target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 1200,
                Radius = () => 220,
                Speed = () => 2200,
                Delay = () => 0f,
                IsEnabled = () => UseR && !IsPassiveActive && !IsUltActive(),
                ShouldCast = (mode, target, spellClass, damage) => target is not null && AllSpellsOnCooldown(),
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => 1200,
                Radius = () => 220,
                Speed = () => 2200,
                Delay = () => 0f,
                IsEnabled = () => UseSemiAutoR && !IsUltActive(),
                TargetSelect = (mode) => SpellRSemiAuto.GetTargets(mode).FirstOrDefault()
            };
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == SemiAutoRKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                SpellRSemiAuto.ExecuteCastSpell();
            }
        }


        private static bool IsUltActive()
        {
            return UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "LucianR" && x.Stacks >= 1);
        }

        private GameObjectBase GetMinionBetweenMeAndEnemy(Hero enemy, int width)
        {
            return UnitManager.EnemyMinions.FirstOrDefault(minion => minion.IsAlive && minion.Distance <= 500 && TargetSelector.IsAttackable(minion) &&
                        Geometry.DistanceFromPointToLine(enemy.W2S, new Vector2[] { UnitManager.MyChampion.W2S, minion.W2S }) <= width / 2 &&
                        minion.W2S.Distance(enemy.W2S) < UnitManager.MyChampion.W2S.Distance(enemy.W2S));
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
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
            get => (DashMode)Enum.Parse(typeof(DashMode), ESettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        internal bool OnlyWIfQENotReady
        {
            get => WSettings.GetItem<Switch>("Only W if QE not ready").IsOn;
            set => WSettings.GetItem<Switch>("Only W if QE not ready").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            TabItem.OnTabItemChange += TabItem_OnTabItemChange;
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Lucian)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Only W if QE not ready", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = false });
            ESettings.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

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
