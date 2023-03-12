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
    internal sealed class AutoGargoyleStoneplate
    {
        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Items");
        private static Group AutoGargoyleStoneplateGroup => Tab.GetGroup("Auto Gargoyle");

        private static bool UseGargoyleStoneplate
        {
            get => AutoGargoyleStoneplateGroup.GetItem<Switch>("Use Gargoyle").IsOn;
            set => AutoGargoyleStoneplateGroup.GetItem<Switch>("Use Gargoyle").IsOn = value;
        }

        private static int BelowHealthPercent
        {
            get => AutoGargoyleStoneplateGroup.GetItem<Counter>("Below health percent").Value;
            set => AutoGargoyleStoneplateGroup.GetItem<Counter>("Below health percent").Value = value;
        }

        private static int TargetCount
        {
            get => AutoGargoyleStoneplateGroup.GetItem<Counter>("Targets in range").Value;
            set => AutoGargoyleStoneplateGroup.GetItem<Counter>("Targets in range").Value = value;
        }

        private static int TargetRange
        {
            get => AutoGargoyleStoneplateGroup.GetItem<Counter>("Target range").Value;
            set => AutoGargoyleStoneplateGroup.GetItem<Counter>("Target range").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Auto Gargoyle"));
            AutoGargoyleStoneplateGroup.AddItem(new Switch() { Title = "Use Gargoyle", IsOn = true });
            AutoGargoyleStoneplateGroup.AddItem(new Counter() { Title = "Below health percent", Value = 40, MinValue = 5, MaxValue = 100, ValueFrequency = 5 });
            AutoGargoyleStoneplateGroup.AddItem(new Counter() { Title = "Targets in range", Value = 2, MinValue = 1, MaxValue = 5, ValueFrequency = 1 });
            AutoGargoyleStoneplateGroup.AddItem(new Counter() { Title = "Target range", Value = 500, MinValue = 100, MaxValue = 2000, ValueFrequency = 50 });

            CoreEvents.OnCoreMainInputAsync += InputHandler;
            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            try
            {
                if (UseGargoyleStoneplate &&
                    UnitManager.MyChampion.IsAlive &&
                    TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                    UnitManager.MyChampion.HealthPercent <= BelowHealthPercent &&
                    TargetCount <= UnitManager.EnemyChampions.Count(x => x.IsAlive && x.Distance <= TargetRange && TargetSelector.IsAttackable(x)))
                {
                    if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Gargoyle_Stoneplate) &&
                        UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Gargoyle_Stoneplate)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Gargoyle_Stoneplate);
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
