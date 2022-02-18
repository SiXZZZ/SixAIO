using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Varus : Champion
    {
        private static int WStacks<T>(T target) where T : GameObjectBase
        {
            var buff = target.BuffManager.GetBuffByName("VarusWDebuff", false, true);
            return buff == null
                ? 0
                : buff.IsActive && buff.Stacks > 0
                    ? (int)buff.Stacks
                    : 0;
        }

        public Varus()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsCharge = () => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => SpellQ.ChargeTimer.IsRunning
                                    ? SpellQ.SpellClass.IsSpellReady
                                            ? 895 + SpellQ.ChargeTimer.ElapsedMilliseconds / 1000 / 0.25f * 140
                                            : 0
                                    : 1600,
                Radius = () => 140,
                Speed = () => 1900,
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            (SpellQ.ChargeTimer.IsRunning || UnitManager.MyChampion.Mana > 85) &&
                            target != null &&
                            (SpellQ.ChargeTimer.IsRunning ? target.Distance < SpellQ.Range() : target.Distance < 1600),
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks)).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            SpellQ.ShouldCast(target, spellClass, damage) &&
                            target != null &&
                            (target.Health / target.MaxHealth * 100) < UseOnlyWIfXLTEHPPercent,
                TargetSelect = (mode) => SpellQ.TargetSelect(mode),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Range = () => 925,
                Radius = () => 300,
                Speed = () => 1600,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 80 &&
                            target != null,
                TargetSelect = (mode) => SpellE.GetTargets(mode, x => (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks)).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 1370,
                Radius = () => 240,
                Speed = () => 1500,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => (x.Health / x.MaxHealth * 100) <= UseOnlyRIfXLTEHPPercent &&
                                                                        (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks) &&
                                                                        RIfMoreThanEnemiesNear < UnitManager.EnemyChampions.Count(enemy =>
                                                                        TargetSelector.IsAttackable(enemy) && enemy.Distance(x) < REnemiesCloserThan))
                                                .FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int UseOnlyIfXGTEWStacks
        {
            get => MenuTab.GetItem<Counter>("Use only if x >= W stacks").Value;
            set => MenuTab.GetItem<Counter>("Use only if x >= W stacks").Value = value;
        }

        private int UseOnlyWIfXLTEHPPercent
        {
            get => MenuTab.GetItem<Counter>("Use only W if x <= HP percent").Value;
            set => MenuTab.GetItem<Counter>("Use only W if x <= HP percent").Value = value;
        }

        private int UseOnlyRIfXLTEHPPercent
        {
            get => MenuTab.GetItem<Counter>("Use only R if x <= HP percent").Value;
            set => MenuTab.GetItem<Counter>("Use only R if x <= HP percent").Value = value;
        }

        private int RIfMoreThanEnemiesNear
        {
            get => MenuTab.GetItem<Counter>("R x >= Enemies Near Target").Value;
            set => MenuTab.GetItem<Counter>("R x >= Enemies Near Target").Value = value;
        }

        private int REnemiesCloserThan
        {
            get => MenuTab.GetItem<Counter>("R Enemies Closer Than").Value;
            set => MenuTab.GetItem<Counter>("R Enemies Closer Than").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Varus)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---General Settings---" });
            MenuTab.AddItem(new Counter() { Title = "Use only if x >= W stacks", MinValue = 0, MaxValue = 3, Value = 3, ValueFrequency = 1 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Use only W if x <= HP percent", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Use only R if x <= HP percent", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            MenuTab.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            MenuTab.AddItem(new Counter() { Title = "R x >= Enemies Near Target", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            MenuTab.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 200, MaxValue = 800, Value = 600, ValueFrequency = 50 });
        }
    }
}
