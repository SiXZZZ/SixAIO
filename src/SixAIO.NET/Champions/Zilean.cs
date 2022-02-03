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
    internal class Zilean : Champion
    {
        public Zilean()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Range = () => 900,
                Speed = () => 5000,
                Width = () => 140,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 80 &&
                            target != null,
                TargetSelect = (mode) => 
                {
                    var bombTarget = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 900 && x.IsAlive && x.BuffManager.HasBuff("ZileanQ"));
                    if (bombTarget != null)
                    {
                        return bombTarget;
                    }

                    var ccTarget = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 900 && x.IsAlive && BuffChecker.IsCrowdControlledOrSlowed(x));
                    if (ccTarget != null)
                    {
                        return ccTarget;
                    }
                    return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 900 && x.IsAlive);
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            !UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).IsSpellReady &&
                            (!UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).IsSpellReady ||
                              UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 900 && x.IsAlive && x.BuffManager.HasBuff("ZileanQ")) != null)
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            target != null,
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 550 && x.IsAlive && TargetSelector.IsAttackable(x))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                CastTime = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 175 &&
                            target != null,
                TargetSelect = (mode) => 
                UnitManager.AllyChampions
                        .Where(ally => MenuTab.GetItem<Counter>("Ult Ally - " + ally.ModelName).Value > 0)
                        .OrderByDescending(ally => MenuTab.GetItem<Counter>("Ult Ally - " + ally.ModelName).Value)
                        .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 900 && TargetSelector.IsAttackable(ally, false) &&
                                                                 (ally.Health / ally.MaxHealth * 100) < RBuffHealthPercent)
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int RBuffHealthPercent
        {
            get => MenuTab.GetItem<Counter>("R Buff Health Percent").Value;
            set => MenuTab.GetItem<Counter>("R Buff Health Percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Zilean)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R Buff Health Percent", MinValue = 0, MaxValue = 100, Value = 20, ValueFrequency = 5 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---Allies to Ult---" });
            foreach (var allyChampion in UnitManager.AllyChampions)
            {
                MenuTab.AddItem(new Counter() { Title = "Ult Ally - " + allyChampion.ModelName, MinValue = 0, MaxValue = 5, Value = 0 });
            }
        }
    }
}
