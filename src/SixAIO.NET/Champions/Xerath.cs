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
    internal class Xerath : Champion
    {
        private static bool _isChargingQ;
        private static System.Diagnostics.Stopwatch _stopwatch = new();

        public Xerath()
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
                        _isChargingQ = true;
                        _stopwatch.Restart();
                        return SpellCastProvider.StartChargeSpell(SpellCastSlot.Q);
                    }
                },
                Range = () => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).IsSpellReady
                        ? 735 + _stopwatch.ElapsedMilliseconds / 1000 / 0.25f * 102f
                        : 0,
                Radius = () => 140,
                Speed = () => 1900,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            (_isChargingQ || UnitManager.MyChampion.Mana > 120) &&
                            target != null &&
                            (_isChargingQ ? target.Distance < SpellQ.Range() : target.Distance < 1450),
                TargetSelect = (mode) =>
                {
                    if (QTargetshouldbecced || QTargetshouldbeslowed)
                    {
                        if (QTargetshouldbecced)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => (_isChargingQ ? x.Distance < SpellQ.Range() : x.Distance < 1450) &&
                                                                        x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        BuffChecker.IsCrowdControlled(x));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                        if (QTargetshouldbeslowed)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => (_isChargingQ ? x.Distance < SpellQ.Range() : x.Distance < 1450) &&
                                                                        x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        BuffChecker.IsCrowdControlledOrSlowed(x));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                    }
                    else
                    {
                        return UnitManager.EnemyChampions.FirstOrDefault(x => (_isChargingQ ? x.Distance < SpellQ.Range() : x.Distance < 1450) &&
                                                                             x.IsAlive && TargetSelector.IsAttackable(x));
                    }

                    return null;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Range = () => 1000,
                Radius = () => 250,
                Speed = () => -1,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 110 &&
                            target != null,
                TargetSelect = (mode) =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= SpellW.Range() && x.IsAlive && TargetSelector.IsAttackable(x))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Range = () => 1125,
                Radius = () => 120,
                Speed = () => 1400,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 80 &&
                            target != null,
                TargetSelect = (mode) =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= SpellE.Range() && x.IsAlive && TargetSelector.IsAttackable(x) && !Collision.MinionCollision(x.Position, 120))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = () => 5000,
                Radius = () => 200,
                Speed = () => 1500,
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) =>
                {
                    return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x));
                }
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell(isCharge: true) /*|| SpellR.ExecuteCastSpell()*/)
            {
                return;
            }
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

        //private int UseOnlyRIfXLTEHPPercent
        //{
        //    get => MenuTab.GetItem<Counter>("Use only R if x <= HP percent").Value;
        //    set => MenuTab.GetItem<Counter>("Use only R if x <= HP percent").Value = value;
        //}

        //private bool RTargetshouldbeslowed
        //{
        //    get => MenuTab.GetItem<Switch>("R Target should be slowed").IsOn;
        //    set => MenuTab.GetItem<Switch>("R Target should be slowed").IsOn = value;
        //}

        //private bool RTargetshouldbecced
        //{
        //    get => MenuTab.GetItem<Switch>("R Target should be cc'ed").IsOn;
        //    set => MenuTab.GetItem<Switch>("R Target should be cc'ed").IsOn = value;
        //}

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Xerath)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Q Target should be slowed", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Q Target should be cc'ed", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });

            //MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            //MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            //MenuTab.AddItem(new Counter() { Title = "Use only R if x <= HP percent", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            //MenuTab.AddItem(new Switch() { Title = "R Target should be slowed", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "R Target should be cc'ed", IsOn = true });
        }
    }
}
