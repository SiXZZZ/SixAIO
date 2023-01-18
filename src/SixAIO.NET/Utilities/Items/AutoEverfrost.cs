using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal sealed class AutoEverfrost
    {
        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Utilities");
        private static Group AutoEverfrostGroup => Tab.GetGroup("Auto Everfrost");

        private static bool UseEverfrost
        {
            get => AutoEverfrostGroup.GetItem<Switch>("Use Everfrost").IsOn;
            set => AutoEverfrostGroup.GetItem<Switch>("Use Everfrost").IsOn = value;
        }

        internal static Prediction.MenuSelected.HitChance HitChance
        {
            get => (Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Prediction.MenuSelected.HitChance), AutoEverfrostGroup.GetItem<ModeDisplay>("HitChance").SelectedModeName);
            set => AutoEverfrostGroup.GetItem<ModeDisplay>("HitChance").SelectedModeName = value.ToString();
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Auto Everfrost"));
            AutoEverfrostGroup.AddItem(new Switch() { Title = "Use Everfrost", IsOn = true });
            AutoEverfrostGroup.AddItem(new ModeDisplay() { Title = "HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            CoreEvents.OnCoreMainInputAsync += InputHandler;
            return Task.CompletedTask;
        }

        static Spell Everfrost;

        private static Task InputHandler()
        {
            try
            {
                if (UseEverfrost &&
                    UnitManager.MyChampion.IsAlive &&
                    TargetSelector.IsAttackable(UnitManager.MyChampion, false))
                {
                    if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Everfrost) &&
                        UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Everfrost)?.IsReady == true)
                    {
                        var everfrost = UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Everfrost);
                        Everfrost = new Spell((CastSlot)everfrost.SpellCastSlot, everfrost.SpellSlot)
                        {
                            PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                            MinimumHitChance = () => HitChance,
                            Range = () => 850,
                            Radius = () => 120,
                            Speed = () => 1550,
                            IsEnabled = () => UseEverfrost,
                            IsSpellReady = (a, b, c) => everfrost.IsReady,
                            TargetSelect = (mode) => Everfrost.GetTargets(mode).FirstOrDefault()
                        };
                        Everfrost.ExecuteCastSpell();
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
