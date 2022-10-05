using Newtonsoft.Json;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
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
    internal sealed class ComboSmite
    {
        public static CastSlot SmiteCastSlot;
        public static SpellSlot SmiteSlot;
        private static TargetSelection _targetSelection;

        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group ComboSmiteGroup => Tab.GetGroup("Combo Smite");

        private static bool UseSmite
        {
            get => ComboSmiteGroup?.GetItem<Switch>("Use Smite").IsOn ?? false;
            set => ComboSmiteGroup.GetItem<Switch>("Use Smite").IsOn = value;
        }

        private static bool SmiteOnCombo
        {
            get => ComboSmiteGroup?.GetItem<Switch>("Smite On Combo")?.IsOn ?? false;
            set => ComboSmiteGroup.GetItem<Switch>("Smite On Combo").IsOn = value;
        }

        private static int SmiteTargetRange
        {
            get => ComboSmiteGroup.GetItem<Counter>("Smite target range").Value;
            set => ComboSmiteGroup.GetItem<Counter>("Smite target range").Value = value;
        }

        private static int MinimumSmiteCharges
        {
            get => ComboSmiteGroup.GetItem<Counter>("Minimum Smite Charges").Value;
            set => ComboSmiteGroup.GetItem<Counter>("Minimum Smite Charges").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Smite, SummonerSpellSlot.First))
            {
                SmiteCastSlot = CastSlot.Summoner1;
                SmiteSlot = SpellSlot.Summoner1;
            }
            else if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Smite, SummonerSpellSlot.Second))
            {
                SmiteCastSlot = CastSlot.Summoner2;
                SmiteSlot = SpellSlot.Summoner2;
            }
            else
            {
                CoreEvents.OnCoreMainInputAsync -= OnCoreMainInputAsync;
                return Task.CompletedTask;
            }

            Tab.AddGroup(new Group("Combo Smite"));
            ComboSmiteGroup.AddItem(new Switch() { Title = "Use Smite", IsOn = true });
            ComboSmiteGroup.AddItem(new Switch() { Title = "Smite On Combo", IsOn = true });
            ComboSmiteGroup.AddItem(new Counter() { Title = "Smite target range", Value = 1000, MinValue = 0, MaxValue = 2000, ValueFrequency = 50 });
            ComboSmiteGroup.AddItem(new Counter() { Title = "Minimum Smite Charges", Value = 1, MinValue = 1, MaxValue = 2, ValueFrequency = 1 });

            LoadTargetPrioValues();

            return Task.CompletedTask;
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
                    ComboSmiteGroup.AddItem(new InfoDisplay() { Title = "-Smite target prio-" });
                }
                foreach (var targetPrioritization in targetPrioritizations)
                {
                    ComboSmiteGroup.AddItem(new Counter() { Title = targetPrioritization.Champion, MinValue = 0, MaxValue = 5, Value = targetPrioritization.Prioritization, ValueFrequency = 1 });
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
                if (ShouldUseSmite())
                {
                    var target = GetPrioritizationTarget();
                    if (target is not null && target.Distance <= 500 && (IsBlueSmite || (IsRedSmite && TargetSelector.IsInRange(target) && TargetSelector.IsAttackable(target))))
                    {
                        SpellCastProvider.CastSpell(SmiteCastSlot, target.W2S);
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

                foreach (var hero in UnitManager.EnemyChampions.Where(x => x.Distance <= SmiteTargetRange && TargetSelector.IsAttackable(x)))
                {
                    try
                    {
                        var targetPrio = ComboSmiteGroup.GetItem<Counter>(x => x.Title == hero.ModelName)?.Value ?? 1;
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

        private static SpellClass SmiteSpell => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SmiteSlot);

        private static bool IsRedSmite => SmiteSpell.SpellData.SpellName.Equals("S5_SummonerSmiteDuel", StringComparison.OrdinalIgnoreCase);
        private static bool IsBlueSmite => SmiteSpell.SpellData.SpellName.Equals("S5_SummonerSmitePlayerGanker", StringComparison.OrdinalIgnoreCase);
        private static bool IsNormalSmite => SmiteSpell.SpellData.SpellName.Equals("SummonerSmite", StringComparison.OrdinalIgnoreCase);

        private static bool ShouldUseSmite()
        {
            return UseSmite && SmiteOnCombo &&
                   UnitManager.MyChampion.IsAlive &&
                   SmiteSpell.Charges >= MinimumSmiteCharges &&
                   (IsBlueSmite || IsRedSmite) &&
                   TargetSelector.IsAttackable(UnitManager.MyChampion, false);
        }
    }
}
