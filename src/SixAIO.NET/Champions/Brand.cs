using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
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
    internal class Brand : Champion
    {
        public Brand()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Range = () => 1100,
                Width = () => 120,
                Speed = () => 1600,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            target != null,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 1000 && x.IsAlive &&
                                                 x.BuffManager.HasBuff("BrandAblaze", false, true) &&
                                                 TargetSelector.IsAttackable(x) && !Collision.MinionCollision(x.W2S, 140) &&
                                                 !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Range = () => 900,
                Speed = () => 5000,
                Width = () => 260,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) => 
                {
                    return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 900 && x.IsAlive && x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed));
                    
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 675 && x.IsAlive &&
                                                 TargetSelector.IsAttackable(x) &&
                                                 !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null &&
                            (UnitManager.Enemies.Count(x => x.Position.Distance(target.Position) < 500) >= 2 || target.Distance < 500),
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 750 && x.IsAlive &&
                                                 TargetSelector.IsAttackable(x) &&
                                                 !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Brand)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
