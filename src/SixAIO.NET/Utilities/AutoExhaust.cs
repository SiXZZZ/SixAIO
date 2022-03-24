using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Oasys.Common.GameObject;

namespace SixAIO.Utilities
{
    internal class AutoExhaust
    {
        internal class TargetSelection
        {
            public List<TargetPrioritization> TargetPrioritizations { get; set; } = new List<TargetPrioritization>();
        }

        internal class TargetPrioritization
        {
            public string Champion { get; set; }

            private int prioritization = 0;

            public int Prioritization
            {
                get => prioritization;
                set
                {
                    if (value <= 5 && value >= 0)
                    {
                        prioritization = value;
                    }
                }
            }

            public override string ToString()
            {
                return $"{Champion} has Prioritization: {Prioritization}";
            }
        }

        public static CastSlot ExhaustSlot;
        private static TargetSelection _targetSelection;

        private static Tab _menuTab => MenuManagerProvider.GetTab($"SIXAIO - Auto Exhaust");

        private static bool UseExhaust
        {
            get => _menuTab.GetItem<Switch>("Use Exhaust").IsOn;
            set => _menuTab.GetItem<Switch>("Use Exhaust").IsOn = value;
        }

        private static bool ExhaustOnCombo
        {
            get => _menuTab?.GetItem<Switch>("Exhaust On Combo")?.IsOn ?? false;
            set => _menuTab.GetItem<Switch>("Exhaust On Combo").IsOn = value;
        }

        private static int ExhaustTargetRange
        {
            get => _menuTab.GetItem<Counter>("Exhaust target range").Value;
            set => _menuTab.GetItem<Counter>("Exhaust target range").Value = value;
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
                return Task.CompletedTask;
            }

            MenuManager.AddTab(new Tab($"SIXAIO - Auto Exhaust"));
            _menuTab.AddItem(new Switch() { Title = "Use Exhaust", IsOn = true });
            _menuTab.AddItem(new Switch() { Title = "Exhaust On Combo", IsOn = true });
            _menuTab.AddItem(new Counter() { Title = "Exhaust target range", Value = 650, MinValue = 0, MaxValue = 2000, ValueFrequency = 50 });

            LoadTargetPrioValues();

            LoadAllyHealthPercents();

            return Task.CompletedTask;
        }

        private static void LoadAllyHealthPercents()
        {
            _menuTab.AddItem(new InfoDisplay() { Title = "-Exhaust target if ally health percent is lower than-" });
            foreach (var ally in UnitManager.AllyChampions)
            {
                var prio = _targetSelection.TargetPrioritizations.FirstOrDefault(x => ally.ModelName.Equals(x.Champion, StringComparison.OrdinalIgnoreCase));
                var percent = prio.Prioritization * 10;
                _menuTab.AddItem(new Counter() { Title = prio.Champion, MinValue = 0, MaxValue = 100, Value = percent, ValueFrequency = 5 });
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
                var enemies = UnitManager.EnemyChampions;

                InitializeSettings(_targetSelection.TargetPrioritizations.Where(x => enemies.Any(e => e.ModelName.Equals(x.Champion, StringComparison.OrdinalIgnoreCase))));
            }
            catch (Exception)
            {
            }
        }

        internal static void InitializeSettings(IEnumerable<TargetPrioritization> targetPrioritizations)
        {
            if (targetPrioritizations.Any())
            {
                _menuTab.AddItem(new InfoDisplay() { Title = "-Exhaust target prio-" });
            }
            foreach (var targetPrioritization in targetPrioritizations)
            {
                _menuTab.AddItem(new Counter() { Title = "Ally - " + targetPrioritization.Champion, MinValue = 0, MaxValue = 5, Value = targetPrioritization.Prioritization, ValueFrequency = 1 });
            }
        }

        internal static Task OnCoreMainInputAsync()
        {
            if (ShouldUseExhaust())
            {
                var exhaustTarget = GetPrioritizationTarget();
                if (exhaustTarget is not null && exhaustTarget.Distance <= 650)
                {
                    SpellCastProvider.CastSpell(ExhaustSlot, exhaustTarget.W2S);
                }
            }

            return Task.CompletedTask;
        }

        private static GameObjectBase GetPrioritizationTarget()
        {
            GameObjectBase tempTarget = null;
            var tempPrio = 0;

            foreach (var hero in UnitManager.Enemies.Where(x => x.Distance <= ExhaustTargetRange))
            {
                try
                {
                    var targetPrio = _menuTab.GetItem<Counter>(x => x.Title == hero.ModelName)?.Value ?? 0;
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

        private static bool IsAnyAllyLow()
        {
            return UnitManager.AllyChampions.Where(x => x.Distance <= ExhaustTargetRange)
                    .Any(ally =>
                        ally.IsAlive && ally.HealthPercent <= _menuTab.GetItem<Counter>(item => item.Title == "Ally - " + ally.ModelName).Value);
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
