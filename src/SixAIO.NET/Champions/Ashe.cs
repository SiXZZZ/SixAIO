using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Ashe : Champion
    {
        public Ashe()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) => spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 50 &&
                                               UnitManager.MyChampion.BuffManager.GetBuffList().Any(x => x.IsActive && x.Stacks >= 4 && x.Name == "asheqcastready"),
                ShouldCast = (mode, target, spellClass, damage) => TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => WHitChance,
                Range = () => 1200,
                Radius = () => 18.5f + (9.25f * SpellW.SpellClass.Level),
                Speed = () => 2000,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => UltRange,
                Radius = () => 350,
                Speed = () => 1600,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        private int UltRange
        {
            get => MenuTab.GetItem<Counter>("R Range").Value;
            set => MenuTab.GetItem<Counter>("R Range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Ashe)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            //MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            MenuTab.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R Range", MinValue = 250, MaxValue = 25000, Value = 2500, ValueFrequency = 250 });

        }
    }
}
