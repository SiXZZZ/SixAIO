using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using Oasys.Common.Menu;
using Oasys.SDK;
using Oasys.SDK.Tools;
using Oasys.Common.Extensions;
using Oasys.Common.Tools.Devices;
using System.Runtime.Serialization;

namespace SixAIO.Champions
{
    internal sealed class Sion : Champion
    {
        public Sion()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsCharge = () => true,
                CastPosition = (pos) =>
                {
                    if (pos.IsValid() && pos.X > 0 && pos.X < Mouse.Resolution.X && pos.Y > 0 && pos.Y < Mouse.Resolution.Y)
                    {
                        Mouse.SetCursor(pos);
                    }

                    return pos;
                },
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () =>
                {
                    var range = 0f;
                    if (SpellQ.ChargeTimer.ElapsedMilliseconds > 1150)
                    {
                        range = 850;
                    }

                    var result = SpellQ.ChargeTimer.IsRunning
                                                        ? SpellQ.SpellClass.IsSpellReady
                                                                ? range
                                                                : 0
                                                        : 850;
                    return result;
                },
                Speed = () => 2000,
                Radius = () => 250,
                IsEnabled = () => UseQ,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                ShouldCast = (mode, target, spellClass, damage) =>
                            target != null &&
                            (SpellQ.ChargeTimer.IsRunning
                            ? target.Distance < SpellQ.Range()
                            : target.Distance < 850),
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 500 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 800,
                Radius = () => 80,
                Speed = () => 1800,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 1500,
                Radius = () => 150,
                Speed = () => 1800,
                IsEnabled = () => UseR && !UnitManager.MyChampion.BuffManager.HasActiveBuff("SionR"),
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Sion)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
