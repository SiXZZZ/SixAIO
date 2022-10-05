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
    internal sealed class AutoIronSpikeWhip
    {
        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Utilities");
        private static Group AutoIronSpikeWhipGroup => Tab.GetGroup("Auto Iron Spike Whip");

        private static bool UseIronSpikeWhip
        {
            get => AutoIronSpikeWhipGroup.GetItem<Switch>("Use Iron Spike Whip").IsOn;
            set => AutoIronSpikeWhipGroup.GetItem<Switch>("Use Iron Spike Whip").IsOn = value;
        }

        private static int TargetCount
        {
            get => AutoIronSpikeWhipGroup.GetItem<Counter>("Targets in range").Value;
            set => AutoIronSpikeWhipGroup.GetItem<Counter>("Targets in range").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Auto Iron Spike Whip"));
            AutoIronSpikeWhipGroup.AddItem(new Switch() { Title = "Use Iron Spike Whip", IsOn = true });
            AutoIronSpikeWhipGroup.AddItem(new Counter() { Title = "Targets in range", Value = 2, MinValue = 1, MaxValue = 5, ValueFrequency = 1 });

            CoreEvents.OnCoreMainInputAsync += InputHandler;
            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            try
            {
                if (UseIronSpikeWhip &&
                    UnitManager.MyChampion.IsAlive &&
                    TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                    TargetCount <= UnitManager.EnemyChampions.Count(x => x.IsAlive && x.Distance <= 450 && TargetSelector.IsAttackable(x)))
                {
                    if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Ironspike_Whip) &&
                        UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Ironspike_Whip)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Ironspike_Whip);
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
