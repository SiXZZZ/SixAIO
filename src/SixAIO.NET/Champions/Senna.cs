using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Logic;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Models;
using System;
using System.Linq;
using Geometry = Oasys.SDK.Geometry;
using Orbwalker = Oasys.SDK.Orbwalker;
using TargetSelector = Oasys.Common.Logic.TargetSelector;
using DamageCalculator = Oasys.SDK.DamageCalculator;
using static Oasys.Common.Logic.Orbwalker;
using SixAIO.Extensions;

namespace SixAIO.Champions
{
    internal sealed class Senna : Champion
    {
        public float PassiveStacks()
        {
            var passiveStacks = UnitManager.MyChampion.BuffManager.GetActiveBuff("SennaPassiveStacks");
            return passiveStacks is null ? 0 : passiveStacks.Stacks;
        }

        public Senna()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) =>
                {
                    var range = 600 + 25 * (PassiveStacks() / 20) + UnitManager.MyChampion.BoundingRadius;
                    var targets = UnitManager.EnemyChampions.Where(x => x.IsAlive && x.Distance <= 1300 && TargetSelector.IsAttackable(x));
                    if (targets.Any(x => x.Distance <= range + x.BoundingRadius))
                    {
                        return targets.FirstOrDefault(x => x.Distance <= range + x.BoundingRadius);
                    }
                    foreach (var target in targets)
                    {
                        var targetMinion = GetMinionBetweenMeAndEnemy(target, 100, range);
                        if (targetMinion != null)
                        {
                            return targetMinion;
                        }
                    }

                    return targets.FirstOrDefault(x => x.Distance <= range + x.BoundingRadius);
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 1250,
                Radius = () => 140,
                Speed = () => 1200,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => RMaximumRange,
                Radius = () => 320,
                Speed = () => 20000,
                Delay = () => 1f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                                        ((125 + spellClass.Level * 125) +
                                        (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                                        (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.7f))
                            : 0,
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                ShouldCast = (mode, target, spellClass, damage) => target != null && target.Health < damage,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange && x.Health <= SpellR.Damage(x, SpellR.SpellClass)).FirstOrDefault()
            };
        }

        private GameObjectBase GetMinionBetweenMeAndEnemy(Hero enemy, int width, float range)
        {
            var myPos = EB.Prediction.Position.PredictUnitPosition(UnitManager.MyChampion, 400);
            var myPosW2s = myPos.To3DWorld().ToW2S();
            var enemyPos = EB.Prediction.Position.PredictUnitPosition(enemy, 400);
            var enemyPosW2s = enemyPos.To3DWorld().ToW2S();
            var enemyTarget = UnitManager.EnemyMinions.FirstOrDefault(minion => IsOnLineWithTarget(width, range, minion, myPosW2s, enemyPosW2s));
            if (enemyTarget is not null)
            {
                return enemyTarget;
            }
            var soul = UnitManager.EnemyJungleMobs.Where(x => x.ModelName == "SennaSoul" && TargetSelector.IsInRange(x)).FirstOrDefault(minion => IsOnLineWithTarget(width, range, minion, myPosW2s, enemyPosW2s));
            if (soul is not null)
            {
                return soul;
            }
            var allyMinion = UnitManager.AllyMinions.FirstOrDefault(minion => IsOnLineWithTarget(width, range, minion, myPosW2s, enemyPosW2s));
            if (allyMinion is not null)
            {
                return allyMinion;
            }
            var allyChamp = UnitManager.AllyChampions.FirstOrDefault(minion => IsOnLineWithTarget(width, range, minion, myPosW2s, enemyPosW2s));
            if (allyChamp is not null)
            {
                return allyChamp;
            }

            return null;
        }

        private static bool IsOnLineWithTarget(int width, float range, GameObjectBase minion, Vector2 myPosW2s, Vector2 enemyPosW2s)
        {
            return minion.IsAlive && minion.Distance <= range &&
                                    TargetSelector.IsAttackable(minion, false, true) &&
                                    Geometry.DistanceFromPointToLine(enemyPosW2s, new Vector2[] { myPosW2s, minion.W2S }) <= width / 2 &&
                                    minion.W2S.Distance(enemyPosW2s) < myPosW2s.Distance(enemyPosW2s);
        }

        private static void TargetSoulsWithOrbwalker()
        {
            var soul = UnitManager.EnemyJungleMobs.FirstOrDefault(x => x.ModelName == "SennaSoul" && TargetSelector.IsInRange(x));
            Orbwalker.SelectedTarget = soul;
        }

        internal override void OnCoreRender()
        {
            SpellW.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            Orbwalker.SelectedTarget = null;

            if (Orbwalker.TargetChampionsOnly && SpellQ.CanExecuteCastSpell())
            {
                var tempTargetChamps = OrbSettings.TargetChampionsOnly;
                OrbSettings.TargetChampionsOnly = false;
                var casted = SpellQ.ExecuteCastSpell();
                OrbSettings.TargetChampionsOnly = tempTargetChamps;
                if (casted)
                {
                    return;
                }
            }
            else
            {
                if (SpellQ.ExecuteCastSpell())
                {
                    return;
                }
            }

            if (SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            TargetSoulsWithOrbwalker();
            if (UseQLaneclear && SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private int RMinimumRange
        {
            get => RSettings.GetItem<Counter>("R minimum range").Value;
            set => RSettings.GetItem<Counter>("R minimum range").Value = value;
        }

        private int RMaximumRange
        {
            get => RSettings.GetItem<Counter>("R maximum range").Value;
            set => RSettings.GetItem<Counter>("R maximum range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Senna)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 80, ValueFrequency = 10 });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R minimum range", MinValue = 0, MaxValue = 30_000, Value = 0, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 30_000, Value = 30_000, ValueFrequency = 50 });


            MenuTab.AddDrawOptions(SpellSlot.W, SpellSlot.R);

        }
    }
}
