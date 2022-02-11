using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Logic;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
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
        public Func<float> Radius { get; set; } = () => 0f;

        public CastSlot CastSlot { get; set; }

        public SpellSlot SpellSlot { get; set; }

        public Func<float> Delay { get; set; } = () => 0.3f;

        public Func<GameObjectBase, SpellClass, float, bool> ShouldCast = (target, spellClass, damage) => false;

        public Func<Oasys.SDK.Orbwalker.OrbWalkingModeType, GameObjectBase> TargetSelect = (mode) => null;

        public Func<GameObjectBase, SpellClass, float> Damage = (target, spellClass) => 0f;

        public bool ShouldCastSpell(Oasys.SDK.Orbwalker.OrbWalkingModeType mode)
        {
            var target = TargetSelect(mode);
            return ShouldCast(target, SpellClass, Damage(target, SpellClass));
        }

        public bool ExecuteCastSpell(Oasys.SDK.Orbwalker.OrbWalkingModeType mode = Oasys.SDK.Orbwalker.OrbWalkingModeType.Combo, bool isCharge = false)
        {
            try
            {
                if (UnitManager.MyChampion.IsAlive && ShouldCastSpell(mode) && (isCharge || !UnitManager.MyChampion.IsCastingSpell))
                {
                    var target = TargetSelect(mode);
                    if (AlertSpellUsage != default)
                    {
                        AlertSpellUsage?.Invoke(CastSlot);
                        return true;
                    }
                    if (target == default && Delay() == default)
                    {
                        var result = CastSpell(CastSlot);
                        if (result)
                        {
                            OnSpellCast?.Invoke(this, target);
                        }
                        return result;
                    }
                    else if (target == default)
                    {
                        var result = CastSpellWithCastTime(CastSlot, Delay());
                        if (result)
                        {
                            OnSpellCast?.Invoke(this, target);
                        }
                        return result;
                    }
                    else if (target != null)
                    {
                        if (Range() > 0)
                        {
                            var input = new PredictionInput()
                            {
                                From = UnitManager.MyChampion.Position,
                                RangeCheckFrom = UnitManager.MyChampion.Position,
                                Speed = Speed(),
                                Delay = Delay(),
                                Radius = Radius(),
                                Range = Range(),
                                Type = Oasys.Common.Logic.SkillshotType.SkillshotLine,
                                Unit = target,
                                CollisionObjects = new CollisionableObjects[] { CollisionableObjects.YasuoWall },
                                Collision = true,
                                Aoe = false,
                                UseBoundingRadius = false
                            };

                            var predictResult = Oasys.SDK.Prediction.GetPrediction(input);

                            if (predictResult.Hitchance < HitChance.Medium)
                            {
                                return false;
                            }

                            Logger.Log($"{predictResult.Hitchance} - {predictResult.Input.Unit.UnitComponentInfo.SkinName} - {(predictResult.UnitPosition - predictResult.CastPosition).Length()}");

                            var pos = predictResult.CastPosition;
                            //var pos = Prediction.Use && target != null && Speed != default && CastTime != default
                            //? Prediction.LinePrediction(target, CastTime(), Speed())
                            //: target.Position;
                            var w2s = LeagueNativeRendererManager.WorldToScreen(pos);
                            if (w2s != default && UnitManager.MyChampion.DistanceTo(pos) <= Range())
                            {
                                var result = CastSpellAtPos(CastSlot, w2s, Delay());
                                if (result)
                                {
                                    OnSpellCast?.Invoke(this, target);
                                }
                                return result;
                            }
                        }
                        else if (CastSpellAtPos(CastSlot, target.W2S, Delay()))
                        {
                            OnSpellCast?.Invoke(this, target);
                            return true;
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
        public Action<CastSlot> AlertSpellUsage { get; set; }

    }
}
