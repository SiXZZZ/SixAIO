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
    internal class AutoRanduins
    {
        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Utilities");
        private static Group AutoRanduinsGroup => Tab.GetGroup("Auto Randuins");

        private static bool UseRanduins
        {
            get => AutoRanduinsGroup.GetItem<Switch>("Use Randuins").IsOn;
            set => AutoRanduinsGroup.GetItem<Switch>("Use Randuins").IsOn = value;
        }

        private static int TargetCount
        {
            get => AutoRanduinsGroup.GetItem<Counter>("Targets in range").Value;
            set => AutoRanduinsGroup.GetItem<Counter>("Targets in range").Value = value;
        }

        private static int HealthPercent
        {
            get => AutoRanduinsGroup.GetItem<Counter>("Health Percent").Value;
            set => AutoRanduinsGroup.GetItem<Counter>("Health Percent").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Auto Randuins"));
            AutoRanduinsGroup.AddItem(new Switch() { Title = "Use Randuins", IsOn = true });
            AutoRanduinsGroup.AddItem(new Counter() { Title = "Targets in range", Value = 2, MinValue = 1, MaxValue = 5, ValueFrequency = 1 });
            AutoRanduinsGroup.AddItem(new Counter() { Title = "Health Percent", Value = 70, MinValue = 0, MaxValue = 100, ValueFrequency = 5 });

            CoreEvents.OnCoreMainInputAsync += InputHandler;
            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            try
            {
                if (UseRanduins &&
                    UnitManager.MyChampion.IsAlive &&
                    TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                    HealthPercent >= UnitManager.MyChampion.HealthPercent &&
                    TargetCount <= UnitManager.EnemyChampions.Count(x => x.IsAlive && x.Distance <= 450 && TargetSelector.IsAttackable(x)))
                {
                    if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Randuins_Omen) &&
                        UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Randuins_Omen)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Randuins_Omen);
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
