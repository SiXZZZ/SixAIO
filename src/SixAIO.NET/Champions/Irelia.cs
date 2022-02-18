using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
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
using System.Threading.Tasks;

namespace SixAIO.Champions
{
    internal class Irelia : Champion
    {
        private static bool AllSpellsOnCooldown()
        {
            var q = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q);
            var w = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W);
            var e = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E);
            return !q.IsSpellReady && !w.IsSpellReady && !e.IsSpellReady;
        }

        public Irelia()
        {
            SDKSpell.OnSpellCast += Spell_OnSpellCast;
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                Speed = () => 1400 + UnitManager.MyChampion.UnitStats.MoveSpeed,
                Range = () => 600f,
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            target != null,
                TargetSelect = (mode) =>
                {
                    var champReset = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(x) && CanQResetOnTarget(x));
                    if (champReset != null)
                    {
                        return champReset;
                    }

                    var minionReset = UnitManager.EnemyMinions.FirstOrDefault(x => x.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(x) && CanQResetOnTarget(x));
                    if (minionReset != null)
                    {
                        return minionReset;
                    }

                    var jungleReset = UnitManager.EnemyJungleMobs.FirstOrDefault(x => x.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(x) && CanQResetOnTarget(x));
                    if (jungleReset != null)
                    {
                        return jungleReset;
                    }

                    if (mode == Orbwalker.OrbWalkingModeType.Combo)
                    {
                        var champ = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(x) && CanQResetOnTarget(x));
                        if (champ != null)
                        {
                            return champ;
                        }
                    }

                    return null;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            target != null,
                TargetSelect = (mode) =>
                {
                    //do stuff
                    return null;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Speed = () => 2000,
                Range = () => 1000f,
                Radius = () => 320,
                Delay = () => 0.4f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            target != null,
                TargetSelect = (mode) => UnitManager.EnemyChampions
                                            .FirstOrDefault(x => x.Distance <= SpellR.Range() &&
                                                                 TargetSelector.IsAttackable(x) &&
                                                                 BuffChecker.IsCrowdControlledOrSlowed(x))
            };
        }

        private float GetMissingHealthPercent(GameObjectBase target)
        {
            var missingHealthPercent = 100f - (target.Health / target.MaxHealth * 100f);
            return missingHealthPercent;
        }

        private float GetRDamage(GameObjectBase target, SpellClass spellClass)
        {
            if (target == null)
            {
                return 0;
            }
            var extraDamagePercent = GetMissingHealthPercent(target) * 2.667f;
            if (extraDamagePercent > 200f)
            {
                extraDamagePercent = 200f;
            }
            return DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) * ((1 + (extraDamagePercent / 100f)) *
                   ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.60f) + 50 + 50 * spellClass.Level));
        }

        private bool CanQResetOnTarget(GameObjectBase target)
        {
            return target != null && target.IsAlive && target.Distance <= 600 && TargetSelector.IsAttackable(target)
                  ? HasIreliaMark(target) || target.Health <= GetQDamage(target, UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q))
                  : false;
        }

        private static bool HasIreliaMark(GameObjectBase target)
        {
            var buff = target.BuffManager.GetBuffByName("ireliamark", false, true);
            return buff != null && buff.IsActive && buff.Stacks >= 1;
        }

        private float GetQDamage(GameObjectBase target, SpellClass spellClass)
        {
            if (target == null)
            {
                return 0;
            }
            var nextAA = DamageCalculator.GetNextBasicAttackDamage(UnitManager.MyChampion, target);
            return nextAA + (DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                   ((UnitManager.MyChampion.UnitStats.TotalAttackDamage * 0.60f) + (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.60f) + (-15) + 20 * spellClass.Level));
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.E)
            {
                SpellQ.ExecuteCastSpell();
            }

            if (spell.SpellSlot == SpellSlot.Q)
            {
                SpellR.ExecuteCastSpell();
            }
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            SpellQ.ExecuteCastSpell();
            //SpellW.ExecuteCastSpell();
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || /*SpellW.ExecuteCastSpell() ||*/ SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Irelia)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}