using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Veigar : Champion
    {
        public Veigar()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => target.IsObject(ObjectTypeFlag.AIMinionClient)
                                                        ? QAllowLaneclearMinionCollision
                                                        : !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 950,
                Radius = () => 140,
                Speed = () => 2200,
                //Damage = (target, spellClass) =>
                //            target != null
                //            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                //            ((-5 + spellClass.Level * 25) +
                //            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * 1.3f) +
                //            (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.15f))
                //            : 0,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 900,
                Speed = () => 5000,
                Radius = () => 240,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => UnitManager.EnemyChampions.Where(x => x.Distance <= 650 &&
                                            TargetSelector.IsAttackable(x) &&
                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                            .FirstOrDefault(RCanKill)
            };
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

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal bool QAllowLaneclearMinionCollision
        {
            get => QSettings.GetItem<Switch>("Q Allow Laneclear minion collision").IsOn;
            set => QSettings.GetItem<Switch>("Q Allow Laneclear minion collision").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Veigar)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q Allow Laneclear minion collision", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            //ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
