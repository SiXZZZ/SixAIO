using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Helpers;
using System;

namespace SixAIO.Models
{
    public class Spell
    {
        public static event Action<Spell, GameObjectBase> OnSpellCast;

        public Spell(CastSlot castSlot, SpellSlot spellSlot)
        {
            CastSlot = castSlot;
            SpellSlot = spellSlot;
        }

        public SpellClass SpellClass => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot);

        public Func<float> Range { get; set; } = () => 0f;
        public Func<float> Speed { get; set; } = () => 0f;
        public Func<float> Width { get; set; } = () => 0f;

        public CastSlot CastSlot { get; set; }

        public SpellSlot SpellSlot { get; set; }

        public Func<float> CastTime { get; set; } = () => 0.3f;

        public Func<GameObjectBase, SpellClass, float, bool> ShouldCast = (target, spellClass, damage) => false;

        public Func<Orbwalker.OrbWalkingModeType, GameObjectBase> TargetSelect = (mode) => null;

        public Func<GameObjectBase, SpellClass, float> Damage = (target, spellClass) => 0f;

        public bool ShouldCastSpell(Orbwalker.OrbWalkingModeType mode)
        {
            var target = TargetSelect(mode);
            return ShouldCast(target, SpellClass, Damage(target, SpellClass));
        }

        public bool ExecuteCastSpell(Orbwalker.OrbWalkingModeType mode = Orbwalker.OrbWalkingModeType.Combo, bool isCharge = false)
        {
            try
            {
                if (UnitManager.MyChampion.IsAlive && (isCharge || !UnitManager.MyChampion.IsCastingSpell))
                {
                    var target = TargetSelect(mode);
                    if (AlertSpellUsage != default)
                    {
                        AlertSpellUsage?.Invoke(CastSlot);
                        return true;
                    }
                    if (target == default && CastTime() == default)
                    {
                        var result = ShouldCastSpell(mode) && CastSpell(CastSlot);
                        if (result)
                        {
                            OnSpellCast?.Invoke(this, target);
                        }
                        return result;
                    }
                    else if (target == default)
                    {
                        var result = ShouldCastSpell(mode) && CastSpellWithCastTime(CastSlot, CastTime());
                        if (result)
                        {
                            OnSpellCast?.Invoke(this, target);
                        }
                        return result;
                    }
                    else
                    {
                        if (Range() > 0)
                        {
                            var pos = target != null && Speed != default && CastTime != default
                            ? Prediction.LinePrediction(target, CastTime(), Speed())
                            : target.Position;
                            var w2s = LeagueNativeRendererManager.WorldToScreen(pos);
                            if (w2s != default && UnitManager.MyChampion.DistanceTo(pos) <= Range())
                            {
                                var result = ShouldCastSpell(mode) && CastSpellAtPos(CastSlot, w2s, CastTime());
                                if (result)
                                {
                                    OnSpellCast?.Invoke(this, target);
                                }
                                return result;
                            }
                        }

                        var res = ShouldCastSpell(mode) && CastSpellAtPos(CastSlot, target.W2S, CastTime());
                        if (res)
                        {
                            OnSpellCast?.Invoke(this, target);
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
