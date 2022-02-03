using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Helpers;
using SixAIO.Models;
using System.Linq;

namespace SixAIO.Champions
{
    internal class KogMaw : Champion
    {
        public KogMaw()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Range = () => 1200,
                Width = () => 140,
                Speed = () => 1650,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((-5 + spellClass.Level * 25) +
                            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * 1.3f) +
                            (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.15f))
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            UnitManager.MyChampion.Mana > QMinMana &&
                            target != null,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x) && x.Distance <= 1100 && x.IsAlive && !Collision.MinionCollision(x.W2S, 140))
                            .OrderBy(x => x.Health)
                            .FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            UnitManager.MyChampion.Mana > WMinMana &&
                            UnitManager.EnemyChampions.Any(x =>
                                            x.Distance < UnitManager.MyChampion.TrueAttackRange + 110 + (20 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W).Level) &&
                                            TargetSelector.IsAttackable(x))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Range = () => 1360,
                Width = () => 240,
                Speed = () => 1400,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            UnitManager.MyChampion.Mana > EMinMana &&
                            target != null,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions.FirstOrDefault(x => TargetSelector.IsAttackable(x) && x.Distance <= 1200 && x.IsAlive)
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = () => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level * 250 + 1050,
                Width = () => 240,
                Speed = () => 1200,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                                        ((200 + spellClass.Level * 150) +
                                        (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                                        (UnitManager.MyChampion.UnitStats.TotalAbilityPower))
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            UnitManager.MyChampion.Mana > RMinMana &&
                            target != null,
                TargetSelect = (mode) => 
                {
                    if (RTargetshouldbecced || RTargetshouldbeslowed)
                    {
                        if (RTargetshouldbecced)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (x.Health / x.MaxHealth * 100) < RTargetMaxHPPercent &&
                                                                        BuffChecker.IsCrowdControlled(x));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                        if (RTargetshouldbeslowed)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (x.Health / x.MaxHealth * 100) < RTargetMaxHPPercent &&
                                                                        BuffChecker.IsCrowdControlledOrSlowed(x));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                    }
                    else
                    {
                        return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (x.Health / x.MaxHealth * 100) < RTargetMaxHPPercent);
                    }

                    return null;
                }
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
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

        private int EMinMana
        {
            get => MenuTab.GetItem<Counter>("E Min Mana").Value;
            set => MenuTab.GetItem<Counter>("E Min Mana").Value = value;
        }

        private int RMinMana
        {
            get => MenuTab.GetItem<Counter>("R Min Mana").Value;
            set => MenuTab.GetItem<Counter>("R Min Mana").Value = value;
        }

        private int RTargetMaxHPPercent
        {
            get => MenuTab.GetItem<Counter>("R Target Max HP Percent").Value;
            set => MenuTab.GetItem<Counter>("R Target Max HP Percent").Value = value;
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

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(KogMaw)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 40, ValueFrequency = 10 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 0, ValueFrequency = 10 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            MenuTab.AddItem(new Switch() { Title = "R Target should be slowed", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "R Target should be cc'ed", IsOn = true });
        }
    }
}
