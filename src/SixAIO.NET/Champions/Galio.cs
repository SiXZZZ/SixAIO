using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using Oasys.Common.Menu;
using Oasys.SDK;
using Oasys.Common.GameObject;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using SixAIO.Helpers;
using SixAIO.Extensions;

namespace SixAIO.Champions
{
    internal sealed class Galio : Champion
    {
        private static TargetSelection _targetSelection;

        public Galio()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 825,
                Speed = () => 1400,
                Radius = () => 120,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                IsCharge = () => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => Prediction.MenuSelected.HitChance.VeryHigh,
                Range = () => SpellW.ChargeTimer.IsRunning
                                        ? SpellW.SpellClass.IsSpellReady
                                            ? 175 + SpellW.ChargeTimer.ElapsedMilliseconds / 1000 / 0.25f * 22f
                                            : 0
                                        : 175,
                Radius = () => 10,
                Speed = () => float.MaxValue,
                IsEnabled = () => UseW,
                MinimumMana = () => 50,
                IsSpellReady = (spellClass, minMana, minCharges) => SpellW.ChargeTimer.IsRunning || UnitManager.MyChampion.Mana > minMana,
                ShouldCast = (mode, target, spellClass, damage) => target != null && (target.Distance < SpellW.Range() || (!SpellW.ChargeTimer.IsRunning && target.Distance <= 175)),
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Delay = () => 0.4f,
                Range = () => 650,
                Radius = () => 320,
                Speed = () => 2300,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                AllowCastOnMap = () => true,
                IsTargetted = () => true,
                IsEnabled = () => UseR && IsAnyAllyLow(),
                Range = () => 3250 + 750 * SpellR.SpellClass.Level,
                TargetSelect = (mode) => GetPrioritizationTarget()
            };
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Galio)}"));
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

            LoadTargetPrioValues();

            LoadAllyHealthPercents();


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);
        }

        internal void LoadTargetPrioValues()
        {
            try
            {
                using var stream = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "Oasys.Core").GetManifestResourceStream("Oasys.Core.Dependencies.TargetSelection.json");
                using var reader = new StreamReader(stream);
                var jsonText = reader.ReadToEnd();

                _targetSelection = JsonConvert.DeserializeObject<TargetSelection>(jsonText);
                var allies = UnitManager.AllyChampions.Where(x => !x.IsTargetDummy && x.NetworkID != UnitManager.MyChampion.NetworkID);

                InitializeSettings(_targetSelection.TargetPrioritizations.Where(x => allies.Any(e => e.ModelName.Equals(x.Champion, StringComparison.OrdinalIgnoreCase))));
            }
            catch (Exception)
            {
            }
        }

        private void LoadAllyHealthPercents()
        {
            try
            {
                RSettings.AddItem(new InfoDisplay() { Title = "-Ult target if ally health percent is lower than-" });
                foreach (var ally in UnitManager.AllyChampions.Where(x => x.NetworkID != UnitManager.MyChampion.NetworkID))
                {
                    var prio = _targetSelection.TargetPrioritizations.FirstOrDefault(x => ally.ModelName.Equals(x.Champion, StringComparison.OrdinalIgnoreCase));
                    var percent = prio.Prioritization * 10;
                    RSettings.AddItem(new Counter() { Title = "Ally - " + prio.Champion, MinValue = 0, MaxValue = 100, Value = percent, ValueFrequency = 5 });
                }
            }
            catch (Exception)
            {
            }
        }

        internal void InitializeSettings(IEnumerable<TargetPrioritization> targetPrioritizations)
        {
            try
            {
                if (targetPrioritizations.Any())
                {
                    RSettings.AddItem(new InfoDisplay() { Title = "-Ult target prio-" });
                }
                foreach (var targetPrioritization in targetPrioritizations)
                {
                    RSettings.AddItem(new Counter() { Title = targetPrioritization.Champion, MinValue = 0, MaxValue = 5, Value = targetPrioritization.Prioritization, ValueFrequency = 1 });
                }
            }
            catch (Exception)
            {
            }
        }

        private GameObjectBase GetPrioritizationTarget()
        {
            try
            {
                GameObjectBase tempTarget = null;
                var tempPrio = 0;

                foreach (var hero in UnitManager.AllyChampions.Where(TargetSelector.IsAttackable).Where(x => x.Distance <= SpellR.Range()))
                {
                    try
                    {
                        var targetPrio = RSettings.GetItem<Counter>(x => x.Title == hero.ModelName)?.Value ?? 0;
                        if (targetPrio > tempPrio)
                        {
                            tempPrio = targetPrio;
                            tempTarget = hero;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                return tempTarget;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool IsAnyAllyLow()
        {
            try
            {
                return UnitManager.AllyChampions
                        .Any(ally =>
                            ally.IsAlive && ally.HealthPercent <= RSettings.GetItem<Counter>(item => item.Title == "Ally - " + ally?.ModelName)?.Value);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
