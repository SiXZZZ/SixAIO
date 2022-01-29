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
                Range = () => 825,
                Width = () => 80,
                Speed = () => 2600,
                CastTime = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            target != null,
                TargetSelect = (mode) => Orbwalker.GetTarget(mode, SpellQ.Range())
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Range = () => 1200,
                Width = () => 80,
                Speed = () => 2200,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = (mode) =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= SpellW.Range() && x.IsAlive &&
                                                TargetSelector.IsAttackable(x) &&
                                                x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed) &&
                                                !Collision.MinionCollision(x.W2S, 120))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                CastTime = () => 0f,
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

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                //Logger.Log("OnCoreHarassInput");
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                //Logger.Log("OnCoreLaneClearInput");
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                //Logger.Log("OnCoreLastHitInput");
                return;
            }
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
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = false });
            MenuTab.AddItem(new ModeDisplay() { Title = "E Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            MenuTab.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 850, Value = 750, ValueFrequency = 50 });
        }
    }
}
