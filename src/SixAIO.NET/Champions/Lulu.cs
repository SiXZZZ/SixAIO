using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject.ObjectClass;
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
    internal class Lulu : Champion
    {
        public Lulu()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Width = 120,
                Speed = 1450,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 70 &&
                            UnitManager.MyChampion.Mana > QMinMana &&
                            target != null,
                TargetSelect = () => UnitManager.EnemyChampions
                                    .OrderBy(x => x.Health)
                                    .FirstOrDefault(enemy => enemy.IsAlive && enemy.Distance <= 850 && TargetSelector.IsAttackable(enemy))
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 65 &&
                            UnitManager.MyChampion.Mana > WMinMana &&
                            target != null,
                TargetSelect = () =>
                {
                    Hero target = null;

                    if (target == null && Polymorph)
                    {
                        target = UnitManager.EnemyChampions
                                .OrderBy(x => x.Health)
                                // and some enemy is casting a spell/attack with targetindex == ally.index
                                .FirstOrDefault(enemy => enemy.IsAlive && enemy.Distance <= 650 && TargetSelector.IsAttackable(enemy) &&
                                                         !TargetSelector.IsInvulnerable(enemy, Oasys.Common.Logic.DamageType.Magical, false) &&
                                                         enemy.IsCastingSpell);
                    }

                    if (target == null && WBuffAlly)
                    {
                        target = UnitManager.AllyChampions
                                .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 650 && TargetSelector.IsAttackable(ally, false) &&
                                                ally.IsCastingSpell && ally.GetCurrentCastingSpell().IsBasicAttack &&
                                                MenuTab.GetItem<Switch>("Buff Ally - " + ally.ModelName).IsOn);
                    }

                    return target;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 80 &&
                            UnitManager.MyChampion.Mana > EMinMana &&
                            target != null,
                TargetSelect = () =>
                {
                    Hero target = null;

                    if (target == null && EBuffAlly)
                    {
                        target = UnitManager.AllyChampions
                                .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 650 && TargetSelector.IsAttackable(ally, false) &&
                                                ally.IsCastingSpell && ally.GetCurrentCastingSpell().IsBasicAttack &&
                                                MenuTab.GetItem<Switch>("Buff Ally - " + ally.ModelName).IsOn);
                    }

                    if (target == null && EShieldAlly)
                    {
                        target = UnitManager.AllyChampions
                                .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 650 && TargetSelector.IsAttackable(ally, false) &&
                                                MenuTab.GetItem<Switch>("Buff Ally - " + ally.ModelName).IsOn &&
                                                // and some enemy is casting a spell/attack with targetindex == ally.index
                                                (ally.Health / ally.MaxHealth * 100) < EShieldHealthPercent);
                    }

                    if (target == null && EOnEnemies)
                    {
                        target = UnitManager.EnemyChampions
                                .OrderBy(x => x.Health)
                                .FirstOrDefault(enemy => enemy.IsAlive && enemy.Distance <= 650 && TargetSelector.IsAttackable(enemy));
                    }

                    return target;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            UnitManager.MyChampion.Mana > RMinMana &&
                            target != null,
                TargetSelect = () =>
                {
                    Hero target = null;
                    if (target == null && RKnockupEnemies)
                    {
                        target = UnitManager.AllyChampions
                                .FirstOrDefault(allyChamp => allyChamp.IsAlive && allyChamp.Distance <= 900 && TargetSelector.IsAttackable(allyChamp, false) &&
                                                UnitManager.EnemyChampions.Count(enemyChamp => enemyChamp.IsAlive && enemyChamp.DistanceTo(allyChamp.Position) < 400 && TargetSelector.IsAttackable(enemyChamp))
                                                >= MenuTab.GetItem<Counter>("Knockup - " + allyChamp.ModelName).Value);
                    }
                    if (target == null && RBuffAlly)
                    {
                        target = UnitManager.AllyChampions
                                .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 900 && TargetSelector.IsAttackable(ally, false) &&
                                                MenuTab.GetItem<Switch>("Buff Ally - " + ally.ModelName).IsOn &&
                                                (ally.Health / ally.MaxHealth * 100) < RBuffHealthPercent);
                    }

                    return target;
                }
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
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

        private bool WBuffAlly
        {
            get => MenuTab.GetItem<Switch>("W Buff ally").IsOn;
            set => MenuTab.GetItem<Switch>("W Buff ally").IsOn = value;
        }

        private bool Polymorph
        {
            get => MenuTab.GetItem<Switch>("Polymorph").IsOn;
            set => MenuTab.GetItem<Switch>("Polymorph").IsOn = value;
        }

        private bool EBuffAlly
        {
            get => MenuTab.GetItem<Switch>("E Buff ally").IsOn;
            set => MenuTab.GetItem<Switch>("E Buff ally").IsOn = value;
        }

        private bool EShieldAlly
        {
            get => MenuTab.GetItem<Switch>("E Shield ally").IsOn;
            set => MenuTab.GetItem<Switch>("E Shield ally").IsOn = value;
        }

        private int EShieldHealthPercent
        {
            get => MenuTab.GetItem<Counter>("E Shield Health Percent").Value;
            set => MenuTab.GetItem<Counter>("E Shield Health Percent").Value = value;
        }

        private bool EOnEnemies
        {
            get => MenuTab.GetItem<Switch>("E for damage on enemies").IsOn;
            set => MenuTab.GetItem<Switch>("E for damage on enemies").IsOn = value;
        }

        private bool RBuffAlly
        {
            get => MenuTab.GetItem<Switch>("R Buff ally").IsOn;
            set => MenuTab.GetItem<Switch>("R Buff ally").IsOn = value;
        }

        private int RBuffHealthPercent
        {
            get => MenuTab.GetItem<Counter>("R Buff Health Percent").Value;
            set => MenuTab.GetItem<Counter>("R Buff Health Percent").Value = value;
        }

        private bool RKnockupEnemies
        {
            get => MenuTab.GetItem<Switch>("R Knockup enemies").IsOn;
            set => MenuTab.GetItem<Switch>("R Knockup enemies").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Lulu)}"));

            MenuTab.AddItem(new InfoDisplay() { Title = "---Allies to buff---" });
            foreach (var allyChampion in UnitManager.AllyChampions)
            {
                MenuTab.AddItem(new Switch() { Title = "Buff Ally - " + allyChampion.ModelName, IsOn = true });
            }

            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 70, ValueFrequency = 10 });

            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 65, ValueFrequency = 10 });
            MenuTab.AddItem(new Switch() { Title = "Polymorph", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "W Buff ally", IsOn = true });

            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 80, ValueFrequency = 10 });
            MenuTab.AddItem(new Switch() { Title = "E Buff ally", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "E Shield ally", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "E Shield Health Percent", MinValue = 0, MaxValue = 100, Value = 70, ValueFrequency = 5 });
            MenuTab.AddItem(new Switch() { Title = "E for damage on enemies", IsOn = false });

            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 100, ValueFrequency = 10 });
            MenuTab.AddItem(new Switch() { Title = "R Buff ally", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R Buff Health Percent", MinValue = 0, MaxValue = 100, Value = 20, ValueFrequency = 5 });

            MenuTab.AddItem(new Switch() { Title = "R Knockup enemies", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "-Min champs to knockup-" });
            foreach (var allyChampion in UnitManager.AllyChampions)
            {
                MenuTab.AddItem(new Counter() { Title = "Knockup - " + allyChampion.ModelName, MinValue = 1, MaxValue = 5, Value = 3, ValueFrequency = 1 });
            }

        }
    }
}
