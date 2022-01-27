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
        private static bool _isChargingQ;
        private static System.Diagnostics.Stopwatch _stopwatch = new();

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
                CastSpellAtPos = (castSlot, pos, castTime) =>
                {
                    if (_isChargingQ)
                    {
                        var released = SpellCastProvider.ReleaseChargeSpell(SpellCastSlot.Q, pos, castTime);
                        if (released)
                        {
                            _stopwatch.Stop();
                            _isChargingQ = false;
                        }
                        return released;
                    }
                    else
                    {
                        SpellW.ExecuteCastSpell();
                        _isChargingQ = true;
                        _stopwatch.Restart();
                        return SpellCastProvider.StartChargeSpell(SpellCastSlot.Q);
                    }
                },
                Range = () => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).IsSpellReady
                        ? 895 + _stopwatch.ElapsedMilliseconds / 1000 / 0.25f * 140
                        : 0,
                Width = () => 140,
                Speed = () => 1900,
                CastTime = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            (_isChargingQ || UnitManager.MyChampion.Mana > 85) &&
                            target != null &&
                            (_isChargingQ ? target.Distance < SpellQ.Range() : target.Distance < 1600),
                TargetSelect = (mode) =>
                {
                    if (QTargetshouldbecced || QTargetshouldbeslowed)
                    {
                        if (QTargetshouldbecced)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => (_isChargingQ ? x.Distance < SpellQ.Range() : x.Distance < 1600) &&
                                                                        x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks) &&
                                                                        x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlled));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                        if (QTargetshouldbeslowed)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => (_isChargingQ ? x.Distance < SpellQ.Range() : x.Distance < 1600) &&
                                                                        x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks) &&
                                                                        x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                    }
                    else
                    {
                        return UnitManager.EnemyChampions.FirstOrDefault(x => (_isChargingQ ? x.Distance < SpellQ.Range() : x.Distance < 1600) &&
                                                                             x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                             (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks));
                    }

                    return null;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                CastTime = () => 0f,
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
                Range = () => 925,
                Width = () => 300,
                Speed = () => 1600,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 80 &&
                            target != null,
                TargetSelect = (mode) =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 925 && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                 (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = () => 1370,
                Width = () => 240,
                Speed = () => 1500,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) =>
                {
                    if (RTargetshouldbecced || RTargetshouldbeslowed)
                    {
                        if (RTargetshouldbecced)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (x.Health / x.MaxHealth * 100) <= UseOnlyRIfXLTEHPPercent &&
                                                                        (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks) &&
                                                                        x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlled) &&
                                                                                    RIfMoreThanEnemiesNear < UnitManager.EnemyChampions.Count(enemy =>
                                                                                    TargetSelector.IsAttackable(enemy) && enemy.Distance(x) < REnemiesCloserThan));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                        if (RTargetshouldbeslowed)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (x.Health / x.MaxHealth * 100) <= UseOnlyRIfXLTEHPPercent &&
                                                                        (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks) &&
                                                                        x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed) &&
                                                                                    RIfMoreThanEnemiesNear < UnitManager.EnemyChampions.Count(enemy =>
                                                                                    TargetSelector.IsAttackable(enemy) && enemy.Distance(x) < REnemiesCloserThan));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                    }
                    else
                    {
                        return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                             (UseOnlyIfXGTEWStacks == 0 || WStacks(x) >= UseOnlyIfXGTEWStacks) &&
                                                                                    RIfMoreThanEnemiesNear < UnitManager.EnemyChampions.Count(enemy =>
                                                                                    TargetSelector.IsAttackable(enemy) && enemy.Distance(x) < REnemiesCloserThan));
                    }

                    return null;
                }
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell(Enums.InputMode.Combo, true) || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int UseOnlyIfXGTEWStacks
        {
            get => MenuTab.GetItem<Counter>("Use only if x >= W stacks").Value;
            set => MenuTab.GetItem<Counter>("Use only if x >= W stacks").Value = value;
        }

        private bool QTargetshouldbeslowed
        {
            get => MenuTab.GetItem<Switch>("Q Target should be slowed").IsOn;
            set => MenuTab.GetItem<Switch>("Q Target should be slowed").IsOn = value;
        }

        private bool QTargetshouldbecced
        {
            get => MenuTab.GetItem<Switch>("Q Target should be cc'ed").IsOn;
            set => MenuTab.GetItem<Switch>("Q Target should be cc'ed").IsOn = value;
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

        private bool RTargetshouldbeslowed
        {
            get => MenuTab.GetItem<Switch>("R Target should be slowed").IsOn;
            set => MenuTab.GetItem<Switch>("R Target should be slowed").IsOn = value;
        }

        private bool RTargetshouldbecced
        {
            get => MenuTab.GetItem<Switch>("R Target should be cc'ed").IsOn;
            set => MenuTab.GetItem<Switch>("R Target should be cc'ed").IsOn = value;
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
            MenuTab.AddItem(new Switch() { Title = "Q Target should be slowed", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Q Target should be cc'ed", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Use only W if x <= HP percent", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Use only R if x <= HP percent", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            MenuTab.AddItem(new Switch() { Title = "R Target should be slowed", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "R Target should be cc'ed", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R x >= Enemies Near Target", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            MenuTab.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 200, MaxValue = 800, Value = 600, ValueFrequency = 50 });
        }
    }
}
