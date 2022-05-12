using Newtonsoft.Json;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal partial class AutoExhaust
    {
        public static CastSlot ExhaustSlot;
        private static TargetSelection _targetSelection;

        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group AutoExhaustGroup => Tab.GetGroup("Auto Exhaust");

        private static bool UseExhaust
        {
            get => AutoExhaustGroup?.GetItem<Switch>("Use Exhaust").IsOn ?? false;
            set => AutoExhaustGroup.GetItem<Switch>("Use Exhaust").IsOn = value;
        }

        private static bool ExhaustOnCombo
        {
            get => AutoExhaustGroup?.GetItem<Switch>("Exhaust On Combo")?.IsOn ?? false;
            set => AutoExhaustGroup.GetItem<Switch>("Exhaust On Combo").IsOn = value;
        }

        private static int ExhaustTargetRange
        {
            get => AutoExhaustGroup.GetItem<Counter>("Exhaust target range").Value;
            set => AutoExhaustGroup.GetItem<Counter>("Exhaust target range").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Exhaust, SummonerSpellSlot.First))
            {
                ExhaustSlot = CastSlot.Summoner1;
            }
            else if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Exhaust, SummonerSpellSlot.Second))
            {
                ExhaustSlot = CastSlot.Summoner2;
            }
            else
            {
                CoreEvents.OnCoreMainInputAsync -= OnCoreMainInputAsync;
                return Task.CompletedTask;
            }

            Tab.AddGroup(new Group("Auto Exhaust"));
            AutoExhaustGroup.AddItem(new Switch() { Title = "Use Exhaust", IsOn = true });
            AutoExhaustGroup.AddItem(new Switch() { Title = "Exhaust On Combo", IsOn = true });
            AutoExhaustGroup.AddItem(new Counter() { Title = "Exhaust target range", Value = 650, MinValue = 0, MaxValue = 2000, ValueFrequency = 50 });

            LoadTargetPrioValues();

            LoadAllyHealthPercents();

            return Task.CompletedTask;
        }

        private static void LoadAllyHealthPercents()
        {
            try
            {
                AutoExhaustGroup.AddItem(new InfoDisplay() { Title = "-Exhaust target if ally health percent is lower than-" });
                foreach (var ally in UnitManager.AllyChampions)
                {
                    var prio = _targetSelection.TargetPrioritizations.FirstOrDefault(x => ally.ModelName.Equals(x.Champion, StringComparison.OrdinalIgnoreCase));
                    var percent = prio.Prioritization * 10;
                    AutoExhaustGroup.AddItem(new Counter() { Title = "Ally - " + prio.Champion, MinValue = 0, MaxValue = 100, Value = percent, ValueFrequency = 5 });
                }
            }
            catch (Exception)
            {
            }
        }

        internal static void LoadTargetPrioValues()
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

        internal static void InitializeSettings(IEnumerable<TargetPrioritization> targetPrioritizations)
        {
            try
            {
                if (targetPrioritizations.Any())
                {
                    AutoExhaustGroup.AddItem(new InfoDisplay() { Title = "-Exhaust target prio-" });
                }
                foreach (var targetPrioritization in targetPrioritizations)
                {
                    AutoExhaustGroup.AddItem(new Counter() { Title = targetPrioritization.Champion, MinValue = 0, MaxValue = 5, Value = targetPrioritization.Prioritization, ValueFrequency = 1 });
                }
            }
            catch (Exception)
            {
            }
        }

        internal static Task OnCoreMainInputAsync()
        {
            try
            {
                if (ShouldUseExhaust())
                {
                    var exhaustTarget = GetPrioritizationTarget();
                    if (exhaustTarget is not null && exhaustTarget.Distance <= 650)
                    {
                        SpellCastProvider.CastSpell(ExhaustSlot, exhaustTarget.W2S);
                    }
                }
            }
            catch (Exception)
            {
            }

            return Task.CompletedTask;
        }

        private static GameObjectBase GetPrioritizationTarget()
        {
            try
            {
                GameObjectBase tempTarget = null;
                var tempPrio = 0;

                foreach (var hero in UnitManager.EnemyChampions.Where(x => x.Distance <= ExhaustTargetRange && TargetSelector.IsAttackable(x)))
                {
                    try
                    {
                        var targetPrio = AutoExhaustGroup.GetItem<Counter>(x => x.Title == hero.ModelName)?.Value ?? 0;
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

        private static bool IsAnyAllyLow()
        {
            try
            {
                return UnitManager.AllyChampions.Where(x => x.Distance <= ExhaustTargetRange)
                        .Any(ally =>
                            ally.IsAlive && ally.HealthPercent <= AutoExhaustGroup.GetItem<Counter>(item => item.Title == "Ally - " + ally.ModelName).Value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool ShouldUseExhaust()
        {
            return UseExhaust && ExhaustOnCombo &&
                   UnitManager.MyChampion.IsAlive &&
                   TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                   IsAnyAllyLow();
        }
    }
}
