using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using System;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal sealed class AutoPotion
    {
        private static float _lastPotion;

        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Items");
        private static Group AutoPotionGroup => Tab.GetGroup("Auto Potion");

        private static bool UsePotion
        {
            get => AutoPotionGroup.GetItem<Switch>("Use Potion").IsOn;
            set => AutoPotionGroup.GetItem<Switch>("Use Potion").IsOn = value;
        }
        private static bool UseRefillablePotion
        {
            get => AutoPotionGroup.GetItem<Switch>("Use Refillable Potion").IsOn;
            set => AutoPotionGroup.GetItem<Switch>("Use Refillable Potion").IsOn = value;
        }
        private static bool UseCorruptingPotion
        {
            get => AutoPotionGroup.GetItem<Switch>("Use Corrupting Potion").IsOn;
            set => AutoPotionGroup.GetItem<Switch>("Use Corrupting Potion").IsOn = value;
        }
        private static bool UseBiscuit
        {
            get => AutoPotionGroup.GetItem<Switch>("Use Biscuit").IsOn;
            set => AutoPotionGroup.GetItem<Switch>("Use Biscuit").IsOn = value;
        }

        private static int HealthFlat
        {
            get => AutoPotionGroup.GetItem<Counter>("Health Flat").Value;
            set => AutoPotionGroup.GetItem<Counter>("Health Flat").Value = value;
        }

        private static int HealthPercent
        {
            get => AutoPotionGroup.GetItem<Counter>("Health Percent").Value;
            set => AutoPotionGroup.GetItem<Counter>("Health Percent").Value = value;
        }

        private static int TimeBetweenPotions
        {
            get => AutoPotionGroup.GetItem<Counter>("Time between potions").Value;
            set => AutoPotionGroup.GetItem<Counter>("Time between potions").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Auto Potion"));
            AutoPotionGroup.AddItem(new Switch() { Title = "Use Potion", IsOn = true });
            AutoPotionGroup.AddItem(new Switch() { Title = "Use Refillable Potion", IsOn = true });
            AutoPotionGroup.AddItem(new Switch() { Title = "Use Corrupting Potion", IsOn = true });
            AutoPotionGroup.AddItem(new Switch() { Title = "Use Biscuit", IsOn = true });
            AutoPotionGroup.AddItem(new Counter() { Title = "Time between potions", Value = 15, MinValue = 0, MaxValue = 120, ValueFrequency = 1 });
            AutoPotionGroup.AddItem(new Counter() { Title = "Health Percent", Value = 30, MinValue = 0, MaxValue = 100, ValueFrequency = 5 });
            AutoPotionGroup.AddItem(new Counter() { Title = "Health Flat", Value = 200, MinValue = 0, MaxValue = 1000, ValueFrequency = 100 });

            CoreEvents.OnCoreMainInputAsync += InputHandler;
            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            try
            {
                var gameTime = GameEngine.GameTime;
                if (UnitManager.MyChampion.IsAlive &&
                    TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                    TimeBetweenPotions + _lastPotion <= gameTime &&
                    (HealthFlat >= UnitManager.MyChampion.Health ||
                    HealthPercent >= UnitManager.MyChampion.HealthPercent))
                {
                    if (UsePotion &&
                        UnitManager.MyChampion.Inventory.HasItem(ItemID.Health_Potion) &&
                        UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Health_Potion)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Health_Potion);
                        _lastPotion = gameTime;
                    }
                    else if (UseRefillablePotion &&
                             UnitManager.MyChampion.Inventory.HasItem(ItemID.Refillable_Potion) &&
                             UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Refillable_Potion)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Refillable_Potion);
                        _lastPotion = gameTime;
                    }
                    else if (UseCorruptingPotion &&
                             UnitManager.MyChampion.Inventory.HasItem(ItemID.Corrupting_Potion) &&
                             UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Corrupting_Potion)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Corrupting_Potion);
                        _lastPotion = gameTime;
                    }
                    else if (UseBiscuit &&
                             UnitManager.MyChampion.Inventory.HasItem(ItemID.Total_Biscuit_of_Everlasting_Will) &&
                             UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Total_Biscuit_of_Everlasting_Will)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Total_Biscuit_of_Everlasting_Will);
                        _lastPotion = gameTime;
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
