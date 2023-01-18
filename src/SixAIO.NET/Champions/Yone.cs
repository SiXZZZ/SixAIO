using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal sealed class Yone : Champion
    {
        private static int GetQState() => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).SpellData.SpellName switch
        {
            "YoneQ" => 1,
            "YoneQ3" => 3,
            _ => 1
        };

        public Yone()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Speed = () => 1500,
                Radius = () => GetQState() == 3 ? 160 : 80,
                Range = () => GetQState() == 3 ? 1050 : 450,
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
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => WHitChance,
                Range = () => 600,
                Radius = () => 80f,
                Speed = () => 10000,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => RMaximumRange,
                Radius = () => 225,
                Speed = () => 10000,
                Delay = () => 0.75f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => RMaximumRange,
                Radius = () => 225,
                Speed = () => 10000,
                Delay = () => 0.75f,
                IsEnabled = () => UseSemiAutoR,
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

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
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

        private int RMaximumRange
        {
            get => RSettings.GetItem<Counter>("R maximum range").Value;
            set => RSettings.GetItem<Counter>("R maximum range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Yone)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 1000, Value = 950, ValueFrequency = 50 });

        }
    }
}
