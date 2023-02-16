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
using Oasys.Common.Tools.Devices;
using Oasys.Common.GameObject.Clients;
using Oasys.SDK.Rendering;
using SharpDX;
using Newtonsoft.Json;
using SixAIO.Utilities;
using System.IO;
using System.Collections.Generic;
using Oasys.SDK.InputProviders;

namespace SixAIO.Champions
{
    internal sealed class Shen : Champion
    {
        private static TargetSelection _targetSelection;
        private AIBaseClient _spirit;

        public Shen()
        {
            SDKSpell.OnSpellCast += SDKSpell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) => true,
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x =>
                        _spirit is not null &&
                        _spirit.DistanceTo(x.Position) <= 300 &&
                        x.IsAlive &&
                        TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                MinimumMana = () => 150,
                Delay = () => 0f,
                Range = () => 600,
                Radius = () => 80,
                Speed = () => 800 + UnitManager.MyChampion.UnitStats.MoveSpeed,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR && IsAnyAllyLow(),
                TargetSelect = (mode) => GetPrioritizationTarget()
            };
        }

        private void SDKSpell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell != null)
            {
                if (spell.CastSlot == CastSlot.E)
                {
                    SpellQ.ExecuteCastSpell();
                }
                if (spell.CastSlot == CastSlot.Q && UseW)
                {
                    KeyboardProvider.PressKey(System.Windows.Forms.Keys.W);
                }
            }
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (obj.Name.Equals("ShenSpiritUnit", StringComparison.OrdinalIgnoreCase))
            {
                _spirit = obj;
            }
        }

        internal override void OnCoreRender()
        {
            if (_spirit != null)
            {
                RenderFactory.DrawCircle(_spirit.W2S, 60, Color.AliceBlue, 2);
            }
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Shen)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });

            LoadTargetPrioValues();

            LoadAllyHealthPercents();
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

                foreach (var hero in UnitManager.AllyChampions.Where(TargetSelector.IsAttackable))
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
