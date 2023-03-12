using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal sealed class AutoGoreDrinker
    {
        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Items");
        private static Group AutoGoreDrinkerGroup => Tab.GetGroup("Auto Gore Drinker");

        private static bool UseGoreDrinker
        {
            get => AutoGoreDrinkerGroup.GetItem<Switch>("Use Gore Drinker").IsOn;
            set => AutoGoreDrinkerGroup.GetItem<Switch>("Use Gore Drinker").IsOn = value;
        }

        private static int TargetCount
        {
            get => AutoGoreDrinkerGroup.GetItem<Counter>("Targets in range").Value;
            set => AutoGoreDrinkerGroup.GetItem<Counter>("Targets in range").Value = value;
        }

        private static int HealthPercent
        {
            get => AutoGoreDrinkerGroup.GetItem<Counter>("Health Percent").Value;
            set => AutoGoreDrinkerGroup.GetItem<Counter>("Health Percent").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Auto Gore Drinker"));
            AutoGoreDrinkerGroup.AddItem(new Switch() { Title = "Use Gore Drinker", IsOn = true });
            AutoGoreDrinkerGroup.AddItem(new Counter() { Title = "Targets in range", Value = 2, MinValue = 1, MaxValue = 5, ValueFrequency = 1 });
            AutoGoreDrinkerGroup.AddItem(new Counter() { Title = "Health Percent", Value = 70, MinValue = 0, MaxValue = 100, ValueFrequency = 5 });

            CoreEvents.OnCoreMainInputAsync += InputHandler;
            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            try
            {
                if (UseGoreDrinker &&
                    UnitManager.MyChampion.IsAlive &&
                    TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                    HealthPercent >= UnitManager.MyChampion.HealthPercent &&
                    TargetCount <= UnitManager.EnemyChampions.Count(x => x.IsAlive && x.Distance <= 450 && TargetSelector.IsAttackable(x)))
                {
                    if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Goredrinker) &&
                        UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Goredrinker)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Goredrinker);
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return Task.CompletedTask;
        }
    }
}
