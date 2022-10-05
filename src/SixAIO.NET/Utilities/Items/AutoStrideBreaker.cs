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
    internal sealed class AutoStrideBreaker
    {
        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Utilities");
        private static Group AutoStrideBreakerGroup => Tab.GetGroup("Auto Stride Breaker");

        private static bool UseStrideBreaker
        {
            get => AutoStrideBreakerGroup.GetItem<Switch>("Use Stride Breaker").IsOn;
            set => AutoStrideBreakerGroup.GetItem<Switch>("Use Stride Breaker").IsOn = value;
        }

        private static int TargetCount
        {
            get => AutoStrideBreakerGroup.GetItem<Counter>("Targets in range").Value;
            set => AutoStrideBreakerGroup.GetItem<Counter>("Targets in range").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Auto Stride Breaker"));
            AutoStrideBreakerGroup.AddItem(new Switch() { Title = "Use Stride Breaker", IsOn = true });
            AutoStrideBreakerGroup.AddItem(new Counter() { Title = "Targets in range", Value = 2, MinValue = 1, MaxValue = 5, ValueFrequency = 1 });

            CoreEvents.OnCoreMainInputAsync += InputHandler;
            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            try
            {
                if (UseStrideBreaker &&
                    UnitManager.MyChampion.IsAlive &&
                    TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                    TargetCount <= UnitManager.EnemyChampions.Count(x => x.IsAlive && x.Distance <= 450 && TargetSelector.IsAttackable(x)))
                {
                    if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Stridebreaker) &&
                        UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Stridebreaker)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Stridebreaker);
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
