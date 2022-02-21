using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
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
    internal class Cassiopeia : Champion
    {
        public Cassiopeia()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 850,
                Speed = () => 5000,
                Radius = () => 200,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                Range = () => 680,
                Damage = (target, spellClass) => GetEDamage(target, spellClass),
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).OrderBy(IsPoisoned).ThenByDescending(x => x.EffectiveMagicHealth).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => RHitChance,
                Range = () => 750,
                Speed = () => 10000,
                Radius = () => 400,
                Delay = () => 0.5f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x =>
                                                      x.IsFacing(UnitManager.MyChampion) &&
                                                      !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                                .FirstOrDefault()
            };
        }

        internal static float GetEDamage(GameObjectBase enemy, SpellClass spellClass)
        {
            if (enemy == null || spellClass == null)
            {
                return 0;
            }

            var magicResistMod = DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, enemy);

            var magicDamage = 48 + 4 * UnitManager.MyChampion.Level +
                              UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.1f;

            if (IsPoisoned(enemy))
            {
                magicDamage += (20 * spellClass.Level) +
                                UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.6f;
            }

            return (float)magicResistMod * magicDamage;
        }

        private static bool IsPoisoned(GameObjectBase target)
        {
            return target.BuffManager.GetBuffList().Any(buff => buff != null && buff.IsActive && buff.OwnerObjectIndex == UnitManager.MyChampion.Index && buff.Stacks >= 1 &&
                    (buff.Name.Contains("cassiopeiaqdebuff", StringComparison.OrdinalIgnoreCase) || buff.Name.Contains("cassiopeiawpoison", StringComparison.OrdinalIgnoreCase)));
        }

        internal override void OnCoreMainInput()
        {
            //var something = (float)(UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).FinalCooldownExpire / UnitManager.MyChampion.GetAttackCastDelay());
            //Logger.Log("Someething : " + something + "  cd: " + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).FinalCooldownExpire);
            //if (!Orbwalker.AllowAttacking && /*UnitManager.MyChampion.Mana / UnitManager.MyChampion.MaxMana * 100) < 10 || */  something > 2.4f)
            //{
            //    Orbwalker.AllowAttacking = true;
            //}
            //else
            //{
            //    Orbwalker.AllowAttacking = false;
            //}

            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            //var something = (float)(UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).FinalCooldownExpire / UnitManager.MyChampion.GetAttackCastDelay());
            //Logger.Log("Someething : " + something + "  cd: " + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).FinalCooldownExpire);
            //if (!Orbwalker.AllowAttacking && /*UnitManager.MyChampion.Mana / UnitManager.MyChampion.MaxMana * 100) < 10 || */  something > 2.4f)
            //{
            //    Orbwalker.AllowAttacking = true;
            //}
            //else
            //{
            //    Orbwalker.AllowAttacking = false;
            //}

            if (SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Cassiopeia)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
