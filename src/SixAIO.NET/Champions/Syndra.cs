using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Syndra : Champion
    {
        private static IEnumerable<GameObjectBase> GetOrbs() => UnitManager.AllNativeObjects.Where(x => x.UnitComponentInfo.SkinName == "SyndraOrbs");

        public Syndra()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Delay = () => 0f,
                Range = () => 800,
                Speed = () => 1000,
                Radius = () => 180,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 80 &&
                            target != null,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => EHitChance,
                Range = () => 800,
                Radius = () => 140,
                Speed = () => 2500,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            target != null,
                TargetSelect = (mode) => 
                {
                    var targets = UnitManager.EnemyChampions
                                                .Where(x => x.IsAlive && x.Distance <= 1000 &&
                                                            TargetSelector.IsAttackable(x) &&
                                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false));
                    if (targets.Any(x => x.Distance <= 550))
                    {
                        return targets.FirstOrDefault(x => x.Distance <= 550);
                    }

                    foreach (var target in targets)
                    {
                        var targetOrb = GetOrbsBetweenMeAndEnemy(target, 180);
                        if (targetOrb != null && targetOrb.Any())
                        {
                            return targetOrb.FirstOrDefault();
                        }
                    }

                    return null;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) => UnitManager.EnemyChampions.Where(x => x.Distance <= (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level >= 3 ? 750 : 675) &&
                                            TargetSelector.IsAttackable(x) &&
                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                            .FirstOrDefault(RCanKill)
            };
        }

        private IEnumerable<GameObjectBase> GetOrbsBetweenMeAndEnemy(Hero enemy, int width)
        {
            return GetOrbs().Where(orb => orb.Distance <= 800 &&
                        Geometry.DistanceFromPointToLine(enemy.W2S, new Vector2[] { UnitManager.MyChampion.W2S, orb.W2S }) <= width &&
                        orb.W2S.Distance(enemy.W2S) < UnitManager.MyChampion.W2S.Distance(enemy.W2S));
        }

        private static bool RCanKill(GameObjectBase target)
        {
            return GetRDamage(target) > target.Health;
        }

        private static float GetRDamage(GameObjectBase target)
        {
            if (target == null)
            {
                return 0;
            }

            var charges = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Charges;
            charges = charges > 7 ? 7 : charges;
            var chargeDamage = 40 + (50 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level) + (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.2f);
            return DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) * (charges * chargeDamage);
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || /*SpellW.ExecuteCastSpell() ||*/ SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Syndra)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
