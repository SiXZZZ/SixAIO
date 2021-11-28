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
        public static event Action<Spell> OnSpellCast;

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
                        OnSpellCast?.Invoke(this);
                        return true;
                    }
                    if (target == default && CastTime == default)
                    {
                        var result = ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpell(CastSlot);
                        if (result)
                        {
                            OnSpellCast?.Invoke(this);
                        }
                        return result;
                    }
                    else if (target == default)
                    {
                        var result = ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpellWithCastTime(CastSlot, CastTime);
                        if (result)
                        {
                            OnSpellCast?.Invoke(this);
                        }
                        return result;
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
                                var result = ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpellAtPos(CastSlot, w2s, CastTime);
                                if (result)
                                {
                                    OnSpellCast?.Invoke(this);
                                }
                                return result;
                            }
                        }

                        var res = ShouldCast(target, spellClass, Damage(target, spellClass)) && CastSpellAtPos(CastSlot, target.W2S, CastTime);
                        if (res)
                        {
                            OnSpellCast?.Invoke(this);
                        }
                        return res;
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
