using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Enums;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Zeri : Champion
    {
        public Zeri()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionType = Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 825,
                Radius = () => 80,
                Speed = () => 2600,
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            target != null,
                TargetSelect = (mode) => Orbwalker.GetTarget(mode, SpellQ.Range())
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionType = Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 1200,
                Radius = () => 80,
                Speed = () => 2200,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 80 &&
                            DashModeSelected == DashMode.ToMouse &&
                            UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= x.TrueAttackRange + 500 && x.IsAlive && TargetSelector.IsAttackable(x)) != null,
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldCast = (target, spellClass, damage) =>
                {
                    return (UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear);
                },
            };
        }

        private static bool IsQActive()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("zeriqpassiveready", false, true);
            return buff != null && buff.IsActive && buff.Stacks >= 1;
        }

        internal override void OnCoreMainInput()
        {
            if (OnlyBasicAttackOnChampions)
            {
                Orbwalker.AllowAttacking = true;
            }
            if (OnlyBasicAttackOnFullCharge)
            {
                Orbwalker.AllowAttacking = IsQActive();
            }
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (OnlyBasicAttackOnChampions)
            {
                Orbwalker.AllowAttacking = false;
            }
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (OnlyBasicAttackOnChampions)
            {
                Orbwalker.AllowAttacking = false;
            }
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (OnlyBasicAttackOnChampions)
            {
                Orbwalker.AllowAttacking = false;
            }
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                return;
            }
        }

        private bool OnlyBasicAttackOnFullCharge
        {
            get => MenuTab.GetItem<Switch>("Only basic attack on full charge").IsOn;
            set => MenuTab.GetItem<Switch>("Only basic attack on full charge").IsOn = value;
        }

        private bool OnlyBasicAttackOnChampions
        {
            get => MenuTab.GetItem<Switch>("Only basic attack on champions").IsOn;
            set => MenuTab.GetItem<Switch>("Only basic attack on champions").IsOn = value;
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), MenuTab.GetItem<ModeDisplay>("E Dash Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("E Dash Mode").SelectedModeName = value.ToString();
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Zeri)}"));
            MenuTab.AddItem(new Switch() { Title = "Only basic attack on full charge", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Only basic attack on champions", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = false });
            MenuTab.AddItem(new ModeDisplay() { Title = "E Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            MenuTab.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 850, Value = 750, ValueFrequency = 50 });
        }
    }
}
