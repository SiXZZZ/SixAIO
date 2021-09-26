using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using SharpDX;
using System;

namespace SixAIO.Models
{
    public class Spell
    {
        public Spell(CastSlot castSlot, SpellSlot spellSlot)
        {
            CastSlot = castSlot;
            SpellSlot = spellSlot;
        }

        public float Delay { get; set; }
        public float Range { get; set; }
        public float Speed { get; set; }
        public float Width { get; set; }

        public float[] Mana { get; set; }
        public bool Collision { get; set; }

        public SpellSlot Slot { get; set; }

        public SpellClass SpellClass { get; set; }

        public Spell(SpellSlot slot, float[] mana, float range)
        {
            Mana = mana;
            Slot = slot;
            SpellClass = UnitManager.MyChampion.GetSpellBook().GetSpellClass(slot);
            Range = range;
        }

        public Spell SetSkillshot(float delay, float width, float speed, bool collision)
        {
            Delay = delay;
            Width = width;
            Speed = speed;
            Collision = collision;
            return this;
        }

        public bool IsReady()
        {
            if (SpellClass.Level == 0)
            {
                return false;
            }

            switch (Slot)
            {
                case SpellSlot.Q:
                    //if (MenuManage.MenuQOnState.IsOn == false) return false;
                    break;
                case SpellSlot.W:
                    //if (MenuManage.MenuWOnState.IsOn == false) return false;
                    break;
                case SpellSlot.E:
                    //if (MenuManage.MenuROnState.IsOn == false) return false;
                    break;
                case SpellSlot.R:
                    //if (MenuManage.MenuROnState.IsOn == false) return false;
                    break;

            }
            return (Mana[SpellClass.Level - 1] < UnitManager.MyChampion.Mana - 10 &&
                   SpellClass.IsSpellReady &&
                   UnitManager.MyChampion.IsAlive);
        }

        public bool ManaReady()
        {
            if (SpellClass.Level == 0)
            {
                return false;
            }

            return Mana[SpellClass.Level - 1] < UnitManager.MyChampion.Mana &&
                   UnitManager.MyChampion.IsAlive;
        }

        public float Cooldown()
        {
            return (float)SpellClass.CooldownExpire - GameEngine.GameTime;
        }

        public CastSlot CastSlot { get; set; }

        public SpellSlot SpellSlot { get; set; }

        public float CastTime { get; set; }

        public Func<GameObjectBase, SpellClass, float, bool> ShouldCast = (target, spellClass, damage) => false;

        public Func<GameObjectBase> TargetSelect = () => null;

        public Func<GameObjectBase, SpellClass, float> Damage = (target, spellClass) => 0f;

        public Func<Vector2> CastPosition = () => default;

        public bool ExecuteCastSpell()
        {
            try
            {
                if (UnitManager.MyChampion.IsAlive)
                {
                    var target = TargetSelect();
                    var spellClass = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot);
                    if (target == default && CastTime == default)
                    {
                        return ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpell(CastSlot);
                    }
                    else if (target == default)
                    {
                        return ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpellWithCastTime(CastSlot, CastTime);
                    }
                    else
                    {
                        var pos = CastPosition();
                        if (pos != default)
                        {
                            return ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpellAtPos(CastSlot, pos, CastTime);
                        }
                        else
                        {
                            return ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpellAtPos(CastSlot, target.W2S, CastTime);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public Func<CastSlot, Vector2, float, bool> CastSpellAtPos = (castSlot, pos, castTime) => SpellCastProvider.CastSpell(castSlot, pos, castTime);
        public Func<CastSlot, float, bool> CastSpellWithCastTime = (castSlot, castTime) => SpellCastProvider.CastSpell(castSlot, castTime);
        public Func<CastSlot, bool> CastSpell = (castSlot) => SpellCastProvider.CastSpell(castSlot);

    }
}
