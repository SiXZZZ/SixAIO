using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Logic;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public CastSlot CastSlot { get; set; }

        public SpellSlot SpellSlot { get; set; }
        public Func<IEnumerable<GameObjectBase>, bool> AllowCollision { get; set; } = (collisions) => true;

        public Func<float> Delay { get; set; } = () => 0.25f;

        public Func<GameObjectBase, SpellClass, float, bool> ShouldCast = (target, spellClass, damage) => false;

        public Func<Oasys.SDK.Orbwalker.OrbWalkingModeType, GameObjectBase> TargetSelect = (mode) => null;

        public Func<GameObjectBase, SpellClass, float> Damage = (target, spellClass) => 0f;
        public Func<float> Range { get; set; } = () => 0f;
        public Func<float> Speed { get; set; } = () => 0f;
        public Func<float> Radius { get; set; } = () => 0f;
        public Func<Vector3> From { get; set; } = () => UnitManager.MyChampion.AIManager.ServerPosition;
        public Func<Prediction.MenuSelected.HitChance> MinimumHitChance { get; set; } = () => Prediction.MenuSelected.HitChance.High;
        public Prediction.MenuSelected.PredictionType PredictionType { get; set; } = Prediction.MenuSelected.PredictionType.Line;

        public Prediction.MenuSelected.PredictionOutput GetPrediction(GameObjectBase target)
        {
            return Prediction.MenuSelected.GetPrediction(PredictionType, target, Range(), Radius(), Delay(), Speed(), From());
        }

        public Prediction.MenuSelected.PredictionOutput GetPrediction(Oasys.SDK.Orbwalker.OrbWalkingModeType mode)
        {
            return Prediction.MenuSelected.GetPrediction(PredictionType, TargetSelect(mode), Range(), Radius(), Delay(), Speed(), From());
        }

        public Prediction.MenuSelected.PredictionOutput GetPrediction()
        {
            return Prediction.MenuSelected.GetPrediction(PredictionType, TargetSelect(Oasys.SDK.Orbwalker.OrbWalkingModeType.Combo), Range(), Radius(), Delay(), Speed(), From());
        }

        private static readonly Func<GameObjectBase, bool> basePredicate = (x) => true;

        public IEnumerable<GameObjectBase> GetTargets(Oasys.SDK.Orbwalker.OrbWalkingModeType mode, Func<GameObjectBase, bool> predicate = null)
        {
            var enemies = new List<GameObjectBase>();
            if (mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.Combo)
            {
                enemies.AddRange(UnitManager.EnemyChampions);
            }
            else if (mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.LaneClear || mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.Mixed)
            {
                enemies.AddRange(UnitManager.EnemyMinions);
                enemies.AddRange(UnitManager.EnemyJungleMobs);
                enemies.AddRange(UnitManager.EnemyChampions);
            }
            else if (mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.LastHit || mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.Freeze)
            {
                enemies.AddRange(UnitManager.EnemyMinions);
            }

            return enemies.Where(x => x.IsAlive && x.DistanceTo(From()) <= Range() && Oasys.Common.Logic.TargetSelector.IsAttackable(x)).Where(predicate ?? basePredicate);
        }

        public Dictionary<GameObjectBase, Prediction.MenuSelected.PredictionOutput>
            GetAvailableTargets<T>(List<T> enemies, Func<GameObjectBase, bool> predicate = null) where T : GameObjectBase =>
                    enemies
                    .Where(x => x.IsAlive && x.DistanceTo(From()) <= Range() && Oasys.Common.Logic.TargetSelector.IsAttackable(x))
                    .Where(predicate ?? basePredicate)
                    .ToDictionary(x => x, x => GetPrediction(x));

        public Dictionary<GameObjectBase, Prediction.MenuSelected.PredictionOutput>
            GetAvailableEnemies(Oasys.SDK.Orbwalker.OrbWalkingModeType mode, Func<GameObjectBase, bool> predicate = null)
        {
            var enemies = new List<GameObjectBase>();
            if (mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.LaneClear || mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.Mixed)
            {
                enemies.AddRange(UnitManager.EnemyMinions);
                enemies.AddRange(UnitManager.EnemyJungleMobs);
                enemies.AddRange(UnitManager.EnemyChampions);
            }
            else if (mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.LastHit || mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.Freeze)
            {
                enemies.AddRange(UnitManager.EnemyMinions);
            }
            else if (mode == Oasys.SDK.Orbwalker.OrbWalkingModeType.Combo)
            {
                enemies.AddRange(UnitManager.EnemyChampions);
            }

            return GetAvailableTargets(enemies, predicate);
        }

        public bool ExecuteCastSpell(Oasys.SDK.Orbwalker.OrbWalkingModeType mode = Oasys.SDK.Orbwalker.OrbWalkingModeType.Combo, bool isCharge = false)
        {
            try
            {
                if (UnitManager.MyChampion.IsAlive && (isCharge || !UnitManager.MyChampion.IsCastingSpell))
                {
                    var target = TargetSelect(mode);
                    if (!ShouldCast(target, SpellClass, Damage(target, SpellClass)))
                    {
                        return false;
                    }
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
                            var predictResult = GetPrediction(target);

                            if (UnitManager.MyChampion.Position.Distance(predictResult.CastPosition) <= Range() &&
                                predictResult.HitChance >= MinimumHitChance() &&
                                (!predictResult.Collision || AllowCollision(predictResult.CollisionObjects)) &&
                                !predictResult.CastPosition.ToW2S().IsZero &&
                                CastSpellAtPos(CastSlot, predictResult.CastPosition.ToW2S(), Delay()))
                            {
                                //Logger.Log($"HitChance: {predictResult.Hitchance} - Target: {predictResult.Input.Unit.UnitComponentInfo.SkinName} - TargetPos: {target.Position} - PredictDistance: {(predictResult.UnitPosition - predictResult.CastPosition).Length()} - W2S: {w2s} - CastPosDist: {UnitManager.MyChampion.Position.Distance(pos)} - CastPos: {predictResult.CastPosition}");
                                //Logger.Log($"{CastSlot} - HitChance: {predictResult.HitChance} - Target: {target.UnitComponentInfo.SkinName} - W2S: {predictResult.CastPosition.ToW2S()} - CastPosDist: {UnitManager.MyChampion.Position.Distance(predictResult.CastPosition)} - CastPos: {predictResult.CastPosition}");

                                OnSpellCast?.Invoke(this, target);
                                return true;
                            }
                        }
                        else if (CastSpellAtPos(CastSlot, target.W2S, Delay()))
                        {
                            //Logger.Log($"{CastSlot} - Target: {target.UnitComponentInfo.SkinName} - W2S: {target.W2S} - using TARGET W2S");
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
