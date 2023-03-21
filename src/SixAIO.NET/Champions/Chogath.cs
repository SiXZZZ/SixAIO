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
    internal sealed class Chogath : Champion
    {
        public Chogath()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 950,
                Speed = () => 5000,
                Delay = () => 1.1f,
                Radius = () => 250,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        var heroTarget = SpellQ.GetTargets(Orbwalker.OrbWalkingModeType.Combo).FirstOrDefault();
                        if (heroTarget is null)
                        {
                            return GetJungleTarget(SpellQ.Range(), x => true);
                        }
                    }

                    return SpellQ.GetTargets(Orbwalker.OrbWalkingModeType.Combo).FirstOrDefault();
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => WHitChance,
                Range = () => 650,
                Speed = () => 1500,
                Radius = () => 60,
                Delay = () => 0.5f,
                IsEnabled = () => UseW,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        var heroTarget = SpellW.GetTargets(Orbwalker.OrbWalkingModeType.Combo).FirstOrDefault();
                        if (heroTarget is null)
                        {
                            return GetJungleTarget(SpellW.Range(), x => true);
                        }
                    }

                    return SpellW.GetTargets(Orbwalker.OrbWalkingModeType.Combo).FirstOrDefault();
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) => spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 30,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    var enemy = Orbwalker.TargetHero;
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        enemy = GetJungleTarget(UnitManager.MyChampion.TrueAttackRange, x => true);
                    }

                    return enemy is not null && TargetSelector.IsAttackable(enemy) && TargetSelector.IsInRange(enemy);
                },
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Range = () => UnitManager.MyChampion.TrueAttackRange,
                IsEnabled = () => UseR,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        var heroTarget = SpellR.GetTargets(Orbwalker.OrbWalkingModeType.Combo, x => x.Health < RDamage(x)).FirstOrDefault();
                        if (heroTarget is null)
                        {
                            return GetJungleTarget(SpellR.Range(), x => x.Health < RDamage(x) &&
                                (x.UnitComponentInfo.SkinName.Contains("SRU_Baron", StringComparison.OrdinalIgnoreCase)) ||
                                (x.UnitComponentInfo.SkinName.Contains("SRU_Dragon", StringComparison.OrdinalIgnoreCase)) ||
                                (x.UnitComponentInfo.SkinName.Contains("SRU_RiftHerald", StringComparison.OrdinalIgnoreCase)));
                        }
                    }

                    return SpellR.GetTargets(mode, x => x.Health < RDamage(x)).FirstOrDefault();
                }
            };
        }

        public GameObjectBase GetJungleTarget(float dist, Func<GameObjectBase, bool> predicate)
        {
            foreach (var enemy in UnitManager.EnemyJungleMobs)
            {
                if (enemy.IsJungle && enemy.IsAlive &&
                    enemy.Distance <= dist &&
                    predicate(enemy) &&
                    !enemy.UnitComponentInfo.SkinName.Contains("mini", StringComparison.OrdinalIgnoreCase) &&
                    ((enemy.UnitComponentInfo.SkinName.Contains("SRU_Baron", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Dragon", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_RiftHerald", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Red", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Blue", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("Sru_Crab", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Krug", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Gromp", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Equals("SRU_Murkwolf", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Equals("SRU_Razorbeak", StringComparison.OrdinalIgnoreCase))))
                {
                    return enemy;
                }
            }

            return null;
        }

        private float RDamage(GameObjectBase target)
        {
            var isChampion = target.IsObject(ObjectTypeFlag.AIHeroClient);
            var baseDamage = isChampion
                ? 125 + SpellR.SpellClass.Level * 175
                : 1200;
            var magicScaleDamage = 0.5f * UnitManager.MyChampion.UnitStats.TotalAbilityPower;
            var healthScaleDamage = 0.1f * UnitManager.MyChampion.BonusHealth;
            var damage = baseDamage + magicScaleDamage + healthScaleDamage;
            var totalDamage = DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, 0, 0, damage);
            return totalDamage;
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
        }
        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
        }
        internal override void OnCoreLaneClearInput()
        {
            if ((UseRLaneclear && SpellR.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear)) ||
                (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear)) ||
                (UseWLaneclear && SpellW.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear)) ||
                (UseELaneclear && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear)))
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Chogath)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Use W Laneclear", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Use R Laneclear", IsOn = true });

            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W);
        }
    }
}
