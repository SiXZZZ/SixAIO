using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Veigar : Champion
    {
        public Veigar()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => target.IsObject(ObjectTypeFlag.AIMinionClient)
                                                        ? QAllowLaneclearMinionCollision
                                                        : !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 950,
                Radius = () => 140,
                Speed = () => 2200,
                Damage = (target, spellClass) => GetQDamage(target),
                IsEnabled = () => UseQ,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.Combo)
                    {
                        return SpellQ.GetTargets(mode).FirstOrDefault();
                    }
                    else if (mode == Orbwalker.OrbWalkingModeType.LastHit)
                    {
                        return SpellQ.GetTargets(mode, x => x.Health <= SpellQ.Damage(x, SpellQ.SpellClass)).FirstOrDefault();
                    }
                    else if (mode == Orbwalker.OrbWalkingModeType.Mixed)
                    {
                        return SpellQ.GetTargets(mode, x => x.IsObject(ObjectTypeFlag.AIHeroClient) || x.Health <= SpellQ.Damage(x, SpellQ.SpellClass)).FirstOrDefault();
                    }
                    else
                    {
                        return SpellQ.GetTargets(mode).FirstOrDefault();
                    }
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 900,
                Speed = () => 5000,
                Radius = () => 240,
                Delay = () => 1.4f,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Range = () => 900,
                Speed = () => 5000,
                Radius = () => 400,
                Delay = () => 0.75f,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                IsTargetted = () => true,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => UnitManager.EnemyChampions.Where(x => x.Distance <= 650 &&
                                            TargetSelector.IsAttackable(x) &&
                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                            .FirstOrDefault(RCanKill)
            };
        }

        private float GetQDamage(GameObjectBase target)
        {
            if (target == null)
            {
                return 0;
            }
            var baseDmg = 40 + SpellQ.SpellClass.Level * 40;
            var scaleDmg = 0.6f * UnitManager.MyChampion.UnitStats.TotalAbilityPower;
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, 0, baseDmg + scaleDmg, 0);
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
            var missingHealthPercent = 100f - (target.Health / target.MaxHealth * 100f);
            var extraDamagePercent = missingHealthPercent * 1.5f;
            if (extraDamagePercent > 100f)
            {
                extraDamagePercent = 100f;
            }
            return (1 + (extraDamagePercent / 100f)) * DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                   ((UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.75f) + 100 + 75 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level);
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (UseQHarass && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (UseQLasthit && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                return;
            }
        }


        internal bool QAllowLaneclearMinionCollision
        {
            get => QSettings.GetItem<Switch>("Q Allow minion collision").IsOn;
            set => QSettings.GetItem<Switch>("Q Allow minion collision").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Veigar)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Harass", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Lasthit", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q Allow minion collision", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);
        }
    }
}
