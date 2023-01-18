using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Annie : Champion
    {
        public Annie()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                Range = () => 625f,
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                                        target != null &&
                                        (target.IsObject(ObjectTypeFlag.AIHeroClient) || target.IsObject(ObjectTypeFlag.AIMinionClient)) &&
                                        (!OnlyQMinionsWhenCanNotStun ||
                                         !target.IsObject(ObjectTypeFlag.AIMinionClient) ||
                                         !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "anniepassiveprimed" && x.Stacks >= 1)),
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LastHit)
                    {
                        return SpellQ.GetTargets(mode, x => x.Health <= QDamage(x)).FirstOrDefault();
                    }
                    if (mode == Orbwalker.OrbWalkingModeType.Mixed ||
                        mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        var lasthit = SpellQ.GetTargets(mode, x => x.Health <= QDamage(x)).FirstOrDefault();
                        if (lasthit != null)
                        {
                            return lasthit;
                        }
                    }
                    return SpellQ.GetTargets(mode).FirstOrDefault();
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => WHitChance,
                Range = () => 600,
                Radius = () => 50f,
                Speed = () => 2000,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 600,
                Speed = () => 5000,
                Radius = () => 260,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        private float QDamage(GameObjectBase target)
        {
            var baseDmg = 45f + SpellQ.SpellClass.Level * 35f;
            var scaleDmg = UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.8f;
            var dmg = baseDmg + scaleDmg;
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, 0, dmg, 0);
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }
        internal override void OnCoreHarassInput()
        {
            if (UseQHarass)
            {
                SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed);
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseQLaneclear)
            {
                SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear);
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (UseQLasthit)
            {
                SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit);
            }
        }

        internal bool OnlyQMinionsWhenCanNotStun
        {
            get => QSettings.GetItem<Switch>("Only Q Minions When Can Not Stun").IsOn;
            set => QSettings.GetItem<Switch>("Only Q Minions When Can Not Stun").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Annie)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Harass", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Lasthit", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Only Q Minions When Can Not Stun", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


        }
    }
}
