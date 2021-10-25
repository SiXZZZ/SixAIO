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
                Damage = (target, spellClass) =>
                            target != null
                            ? Helpers.DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((-5 + spellClass.Level * 25) +
                            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * 1.3f) +
                            (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.15f))
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            UnitManager.MyChampion.Mana > QMinMana &&
                            target != null &&
                            !Collision.MinionCollision(target.W2S, 100),
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x) && x.Distance <= 1000 && x.IsAlive)
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
                            UnitManager.EnemyChampions.Any(x => x.Distance < UnitManager.MyChampion.TrueAttackRange + 110 + (20 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W).Level))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            UnitManager.MyChampion.Mana > EMinMana &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions.FirstOrDefault(x => TargetSelector.IsAttackable(x) && x.Distance <= 800 && x.IsAlive)
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Damage = (target, spellClass) =>
                            target != null
                            ? Helpers.DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                                        ((200 + spellClass.Level * 150) +
                                        (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                                        (UnitManager.MyChampion.UnitStats.TotalAbilityPower))
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            UnitManager.MyChampion.Mana > RMinMana &&
                            UnitManager.EnemyChampions.All(x => x.Distance > UnitManager.MyChampion.AttackRange) &&
                            target != null &&
                            target.Health < damage,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => TargetSelector.IsAttackable(x) && x.BuffManager.GetBuffList().Any(buff => buff.IsActive && buff.EntryType == BuffType.Slow || BuffChecker.IsCrowdControlled(buff)))
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
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

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(KogMaw)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 0, ValueFrequency = 10 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
        }
    }
}
