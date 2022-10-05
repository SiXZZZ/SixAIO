using Newtonsoft.Json;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using SixAIO.Models;
using SixAIO.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Kindred : Champion
    {
        private static TargetSelection _targetSelection;

        private static int PassiveStacks()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("kindredmarkofthekindredstackcounter", false, true);
            return buff == null
                ? 0
                : buff.IsActive && buff.Stacks > 0
                    ? (int)buff.Stacks
                    : 0;
        }

        private static bool HasQBuff()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("kindredqasbuff", false, true);
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        private static bool HasWBuff()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("kindredwclonebuffvisible", false, true);
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        public Kindred()
        {
            SDKSpell.OnSpellCast += Spell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            ShouldQ() &&
                            UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && TargetSelector.IsAttackable(x)),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Delay = () => 0f,
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                            !HasWBuff() &&
                            UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => TargetSelector.IsAttackable(Orbwalker.TargetHero) && Orbwalker.TargetHero.Distance <= ERange()
                            ? Orbwalker.TargetHero
                            : UnitManager.EnemyChampions.FirstOrDefault(x => x.IsAlive && x.Distance <= ERange() && TargetSelector.IsAttackable(x) && !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false))
            };
        }

        private bool ShouldQ() => Qonlyifhaswbuff ? HasWBuff() : true;

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell != null)
            {
                if (spell.SpellSlot == SpellSlot.E)
                {
                    SpellW.ExecuteCastSpell();
                }
                if (spell.SpellSlot == SpellSlot.W)
                {
                    SpellQ.ExecuteCastSpell();
                }
            }
        }

        private static int ERange() => PassiveStacks() switch
        {
            < 4 => 500,
            < 7 => 575,
            < 10 => 600,
            < 13 => 625,
            < 16 => 650,
            < 19 => 675,
            < 22 => 700,
            < 25 => 725,
            >= 25 => 750
        };

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), QSettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => QSettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        private bool Qonlyifhaswbuff
        {
            get => QSettings.GetItem<Switch>("Q only if has w buff").IsOn;
            set => QSettings.GetItem<Switch>("Q only if has w buff").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kindred)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });
            QSettings.AddItem(new Switch() { Title = "Q only if has w buff", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E target range", Value = 1000, MinValue = 0, MaxValue = 2000, ValueFrequency = 50 });
            LoadTargetPrioValues();
            //RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }

        internal void LoadTargetPrioValues()
        {
            try
            {
                using var stream = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "Oasys.Core").GetManifestResourceStream("Oasys.Core.Dependencies.TargetSelection.json");
                using var reader = new StreamReader(stream);
                var jsonText = reader.ReadToEnd();

                _targetSelection = JsonConvert.DeserializeObject<TargetSelection>(jsonText);
                var enemies = UnitManager.EnemyChampions.Where(x => !x.IsTargetDummy);

                InitializeSettings(_targetSelection.TargetPrioritizations.Where(x => enemies.Any(e => e.ModelName.Equals(x.Champion, StringComparison.OrdinalIgnoreCase))));
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
                    ESettings.AddItem(new InfoDisplay() { Title = "-E target prio-" });
                }
                foreach (var targetPrioritization in targetPrioritizations)
                {
                    ESettings.AddItem(new Counter() { Title = targetPrioritization.Champion, MinValue = 0, MaxValue = 5, Value = targetPrioritization.Prioritization, ValueFrequency = 1 });
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
