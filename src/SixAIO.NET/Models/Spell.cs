using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Helpers;
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

        public float Range { get; set; }
        public float Speed { get; set; }
        public float Width { get; set; }

        public float[] Mana { get; set; }
        public bool Collision { get; set; }

        public SpellSlot Slot { get; set; }

        public SpellClass SpellClass { get; set; }

        public CastSlot CastSlot { get; set; }

        public SpellSlot SpellSlot { get; set; }

        public float CastTime { get; set; } = 0.3f;

        public Func<GameObjectBase, SpellClass, float, bool> ShouldCast = (target, spellClass, damage) => false;

        public Func<GameObjectBase> TargetSelect = () => null;

        public Func<GameObjectBase, SpellClass, float> Damage = (target, spellClass) => 0f;

        public bool ExecuteCastSpell(bool turnTargetChampionsOnlyOff = false)
        {
            try
            {
                if (UnitManager.MyChampion.IsAlive)
                {
                    var target = TargetSelect();
                    var spellClass = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot);
                    if (turnTargetChampionsOnlyOff)
                    {
                        Orbwalker.TargetChampionsOnly = false;
                    }
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

                        var pos = target != null && Speed != default && CastTime != default
                            ? LeagueNativeRendererManager.WorldToScreen(Prediction.LinePrediction(target, 1, CastTime, Speed))
                            : target.W2S;
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
