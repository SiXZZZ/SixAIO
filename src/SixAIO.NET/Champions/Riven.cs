using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances;
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
using System.Threading.Tasks;

namespace SixAIO.Champions
{
    internal class Riven : Champion
    {
        private static int PassiveStacks()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("RivenPassiveAABoost", false, true);
            return buff == null
                ? 0
                : buff.IsActive && buff.Stacks > 0
                    ? (int)buff.Stacks
                    : 0;
        }
        //RivenTriCleave = q passive 
        private static BuffEntry GetUltBuff() => UnitManager.MyChampion.BuffManager.GetBuffByName("rivenwindslashready", false, true);

        private static bool IsUltActive()
        {
            var buff = GetUltBuff();
            return buff != null && buff.IsActive;
        }

        private static float UltTimeLeft()
        {
            var buff = GetUltBuff();
            return buff != null && buff.IsActive ? buff.EndTime - GameEngine.GameTime : 0;
        }

        private static bool IsWindSlashReady()
        {
            var buff = GetUltBuff();
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        private static bool AllSpellsOnCooldown()
        {
            var q = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q);
            var w = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W);
            var e = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E);
            return !q.IsSpellReady && !w.IsSpellReady && !e.IsSpellReady;
        }

        private int _lastQCharge = -1;
        private float _lastQChargeTime = 0;
        private float _lastAATime = 0f;
        private float _lastQTime = 0f;

        public Riven()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                CastTime = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            _lastAATime > _lastQTime + 0.333f &&
                            _lastAATime > _lastQChargeTime + 0.333f &&
                            target != null,
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange + (IsUltActive() ? 250 : 150) &&
                                                                                    TargetSelector.IsAttackable(x))
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.EnemyChampions.Any(x => x.Distance <= x.UnitComponentInfo.UnitBoundingRadius + (IsUltActive() ? 300 : 250) &&
                                                                                    TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                CastTime = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            target != null,
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= (IsUltActive() ? 450 : 400) && TargetSelector.IsAttackable(x))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = () => 1100f,
                Speed = () => 1600f,
                Width = () => 200,
                Damage = GetRDamage,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            IsWindSlashReady() &&
                            target != null &&
                            (target.Health < GetRDamage(target, spellClass) ||
                            (UltTimeLeft() > 0 && UltTimeLeft() < 1f) ||
                            (AllSpellsOnCooldown() && PassiveStacks() <= 2) ||
                            (GetMissingHealthPercent(target) < 75.0f)),
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 800 && TargetSelector.IsAttackable(x) &&
                                                                                    x.Health < GetRDamage(x, UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R)))
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

        private void Spell_OnSpellCast(Spell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.Q)
            {
                _lastQTime = GameEngine.GameTime;
            }

            if (spell.SpellSlot == SpellSlot.E)
            {
                SpellW.ExecuteCastSpell();
                SpellQ.ExecuteCastSpell();
                SpellR.ExecuteCastSpell();
            }

            if (spell.SpellSlot == SpellSlot.W)
            {
                SpellQ.ExecuteCastSpell();
                SpellR.ExecuteCastSpell();
            }

            if (spell.SpellSlot == SpellSlot.Q)
            {
                SpellR.ExecuteCastSpell();
                SpellW.ExecuteCastSpell();
            }
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            _lastAATime = gameTime;
            if (target != null)
            {
                SpellQ.ExecuteCastSpell();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreMainTick()
        {
            if (_lastQCharge != UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).Charges)
            {
                _lastQCharge = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).Charges;
                _lastQChargeTime = GameEngine.GameTime;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Riven)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}