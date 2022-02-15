using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools.Devices;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Draven : Champion
    {
        private static IEnumerable<GameObjectBase> Axes() => UnitManager.AllNativeObjects.Where(x => x.IsAlive && x.Distance <= 2000 &&
                                                        x.Name.Contains("Draven") && x.Name.Contains("Q") && x.Name.Contains("reticle"));

        private static int PassiveStacks()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("dravenpassivestacks", false, true);
            return buff != null ? (int)buff.Stacks : 0;
        }

        private static int QStacks()
        {
            var buff1 = UnitManager.MyChampion.BuffManager.GetBuffByName("DravenSpinning", false, true);
            var buff2 = UnitManager.MyChampion.BuffManager.GetBuffByName("dravenspinningleft", false, true);

            var stacks = buff1 != null && buff1.IsActive ? (int)buff1.Stacks : 0;
            stacks += buff2 != null && buff2.IsActive ? (int)buff2.Stacks : 0;
            //Logger.Log("Q Stacks " + stacks);
            return stacks;
        }

        private static float GetRDamage(GameObjectBase target)
        {
            var rLevel = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level;
            var dmg = (0.9f + rLevel * 0.2f) * UnitManager.MyChampion.UnitStats.BonusAttackDamage + 75 + 100 * rLevel;
            var effectiveDamage = DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) * dmg;
            return target.Health - PassiveStacks() - effectiveDamage < 0
                    ? PassiveStacks() + effectiveDamage
                    : effectiveDamage;
        }

        public Draven()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero) &&
                            QStacks() <= 1,
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero) &&
                            QStacks() > 0,
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionType = Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 1100,
                Radius = () => 260,
                Speed = () => 1400,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 70 &&
                            target != null,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionType = Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Delay = () => 0.5f,
                Range = () => 30000,
                Radius = () => 320,
                Speed = () => 2000,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault(x => x.Health < GetRDamage(x))
            };
        }

        internal override void OnCoreMainInput()
        {
            //if (UseQCatchRange)
            //{
            //    var axes = Axes();
            //    if (axes.Any())
            //    {
            //        var catchAxe = axes.Where(x => (QCatchMode == CatchMode.Mouse && x.DistanceTo(GameEngine.WorldMousePosition) <= QCatchRange) ||
            //                                       (QCatchMode == CatchMode.Self && x.Distance <= QCatchRange))
            //                           .OrderBy(x => x.DistanceTo(GameEngine.WorldMousePosition))
            //                           .FirstOrDefault();
            //        if (catchAxe != null)
            //        {
            //            Orbwalker.ForceMovePosition = catchAxe.W2S;
            //        }
            //        else
            //        {
            //            Orbwalker.ForceMovePosition = Vector2.Zero;
            //        }
            //    }
            //    else
            //    {
            //        Orbwalker.ForceMovePosition = Vector2.Zero;
            //    }
            //}
            //try something with if w is ready =/= axe is catched and store the current axe to catch to filter it out from the rest

            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreMainInputRelease()
        {
            Orbwalker.ForceMovePosition = Vector2.Zero;
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private bool UseQCatchRange
        {
            get => MenuTab.GetItem<Switch>("Use Q Catch Range").IsOn;
            set => MenuTab.GetItem<Switch>("Use Q Catch Range").IsOn = value;
        }

        private int QCatchRange
        {
            get => MenuTab.GetItem<Counter>("Q Catch Range").Value;
            set => MenuTab.GetItem<Counter>("Q Catch Range").Value = value;
        }

        private CatchMode QCatchMode
        {
            get => (CatchMode)Enum.Parse(typeof(CatchMode), MenuTab.GetItem<ModeDisplay>("Q Catch Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Q Catch Mode").SelectedModeName = value.ToString();
        }

        private enum CatchMode
        {
            Mouse,
            Self
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Draven)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use Q Catch Range", IsOn = true });
            //MenuTab.AddItem(new Counter() { Title = "Q Catch Range", MinValue = 0, MaxValue = 800, Value = 500, ValueFrequency = 50 });
            //MenuTab.AddItem(new ModeDisplay() { Title = "Q Catch Mode", ModeNames = Enum.GetNames(typeof(CatchMode)).ToList(), SelectedModeName = "Mouse" });

            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });

            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
