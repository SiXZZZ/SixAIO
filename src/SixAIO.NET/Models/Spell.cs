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
                if (UnitManager.MyChampion.IsAlive && !UnitManager.MyChampion.IsCastingSpell)
                {
                    var target = TargetSelect();
                    var spellClass = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot);
                    if (turnTargetChampionsOnlyOff)
                    {
                        Orbwalker.TargetChampionsOnly = false;
                    }
                    if (AlertSpellUsage != default)
                    {
                        AlertSpellUsage?.Invoke(CastSlot);
                        return true;
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
                        if (Range > 0)
                        {
                            var pos = target != null && Speed != default && CastTime != default
                            ? Prediction.LinePrediction(target, 1, CastTime, Speed)
                            : target.Position;
                            var w2s = LeagueNativeRendererManager.WorldToScreen(pos);
                            if (w2s != default && UnitManager.MyChampion.DistanceTo(pos) <= Range)
                            {
                                return ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpellAtPos(CastSlot, w2s, CastTime);
                            }
                        }

                        return ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpellAtPos(CastSlot, target.W2S, CastTime);
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
        public Action<CastSlot> AlertSpellUsage { get; set; }

    }
}
