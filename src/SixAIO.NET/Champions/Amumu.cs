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
    internal class Amumu : Champion
    {
        private static bool IsWActive()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("AuraofDespair", false, true);
            return buff != null && buff.Stacks >= 1;
        }

        public Amumu()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = collisions => !collisions.Any(),
                PredictionType = Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1100,
                Radius = () => 160,
                Speed = () => 2000,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 70 &&
                            UnitManager.MyChampion.Mana > QMinMana &&
                            spellClass.Charges > 0 &&
                            target != null,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                {
                    return IsWActive()
                        ? UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 8 &&
                            UnitManager.MyChampion.Mana > WMinMana &&
                            !UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance <= 350 && x.IsAlive)
                        : UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 8 &&
                            UnitManager.MyChampion.Mana > WMinMana &&
                            UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance <= 350 && x.IsAlive);
                },
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) =>
                {
                    return UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 35 &&
                            UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance <= 350 && x.IsAlive);
                },
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldCast = (target, spellClass, damage) =>
                {
                    return (UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            UnitManager.MyChampion.Mana > RMinMana &&
                            UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear);
                },
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int QMinMana
        {
            get => MenuTab.GetItem<Counter>("Q Min Mana").Value;
            set => MenuTab.GetItem<Counter>("Q Min Mana").Value = value;
        }

        private int WMinMana
        {
            get => MenuTab.GetItem<Counter>("W Min Mana").Value;
            set => MenuTab.GetItem<Counter>("W Min Mana").Value = value;
        }

        private int RMinMana
        {
            get => MenuTab.GetItem<Counter>("R Min Mana").Value;
            set => MenuTab.GetItem<Counter>("R Min Mana").Value = value;
        }

        private int RIfMoreThanEnemiesNear
        {
            get => MenuTab.GetItem<Counter>("R If More Than Enemies Near").Value;
            set => MenuTab.GetItem<Counter>("R If More Than Enemies Near").Value = value;
        }

        private int REnemiesCloserThan
        {
            get => MenuTab.GetItem<Counter>("R Enemies Closer Than").Value;
            set => MenuTab.GetItem<Counter>("R Enemies Closer Than").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Amumu)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            MenuTab.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 600, Value = 350, ValueFrequency = 50 });
        }
    }
}
