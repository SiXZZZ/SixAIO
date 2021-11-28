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

        private static BuffEntry GetUltBuff()
        {
            return UnitManager.MyChampion.BuffManager.GetBuffByName("rivenwindslashready", false, true);
        }

        private static bool IsUltActive()
        {
            var buff = GetUltBuff();
            return buff != null && buff.IsActive;
        }

        private static bool IsWindSlashReady()
        {
            var buff = GetUltBuff();
            if (buff != null)
            {
                Logger.Log(buff.Name + " " + buff.Stacks + " " + buff.StartTime + " " + buff.EndTime);
            }
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        private float _lastAATime = 0f;
        private float _lastQTime = 0f;
        public Riven()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                CastTime = 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            _lastAATime > _lastQTime &&
                            target != null,
                TargetSelect = () => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 400 && TargetSelector.IsAttackable(x))
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            target != null,
                TargetSelect = () => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 250 && TargetSelector.IsAttackable(x))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                CastTime = 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            target != null,
                TargetSelect = () => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 400 && TargetSelector.IsAttackable(x))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = 1100f,
                Speed = 1600f,
                Width = 200,
                Damage = GetRDamage,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            IsWindSlashReady() &&
                            target != null &&
                            target.Health < GetRDamage(target, spellClass),
                TargetSelect = () => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 800 && TargetSelector.IsAttackable(x) &&
                                                                                    x.Health < GetRDamage(x, UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R)))
            };
        }

        private float GetRDamage(GameObjectBase target, SpellClass spellClass)
        {
            if (target == null)
            {
                return 0;
            }
            var missingHealthPercent = 100f - (target.Health / target.MaxHealth * 100f);
            var extraDamagePercent = missingHealthPercent * 2.667f;
            if (extraDamagePercent > 200f)
            {
                extraDamagePercent = 200f;
            }
            return DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) * ((1 + (extraDamagePercent / 100f)) *
                   ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.60f) + 50 + 50 * spellClass.Level));
        }

        private void Spell_OnSpellCast(Spell spell)
        {
            if (spell.SpellSlot == SpellSlot.Q)
            {
                _lastQTime = GameEngine.GameTime;
            }

            if (spell.SpellSlot == SpellSlot.E)
            {
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