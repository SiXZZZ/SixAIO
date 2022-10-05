using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal sealed class Evelynn : Champion
    {
        private bool IsQLine()
        {
            return UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).SpellData.SpellName == "EvelynnQ";
        }

        private bool IsWActive(GameObjectBase gameObject) => gameObject.BuffManager.ActiveBuffs.Any(x => x.IsActive && x.Stacks >= 1 && x.Name.Equals("EvelynnW", StringComparison.OrdinalIgnoreCase) && x.StartTime + 2.5f <= GameEngine.GameTime);

        public Evelynn()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => !IsQLine(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 800,
                Radius = () => 100,
                Speed = () => 2400,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).OrderBy(x => IsWActive(x)).FirstOrDefault(x => !OnlyQOnWTargets || IsWActive(x) || !IsQLine())
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => UnitManager.EnemyChampions.Where(x => x.Distance <= 350 && TargetSelector.IsAttackable(x)).OrderBy(x => IsWActive(x)).FirstOrDefault(x => !OnlyEOnWTargets || IsWActive(x))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => RHitChance,
                Range = () => 500,
                Radius = () => 180,
                Speed = () => 2000,
                Delay = () => 0.35f,
                Damage = (target, spellClass) =>
                {
                    if (target == null)
                    {
                        return 0;
                    }

                    var baseDamage = 125 * spellClass.Level;
                    var scaleDamage = 0.75f * UnitManager.MyChampion.UnitStats.TotalAbilityPower;
                    var dmgMod = target.HealthPercent < 30 ? 2.4f : 1f;

                    return DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) * (dmgMod * (baseDamage + scaleDamage));
                },
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) => target != null && target.Health < damage,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal bool OnlyQOnWTargets
        {
            get => QSettings.GetItem<Switch>("Only Q On W Targets").IsOn;
            set => QSettings.GetItem<Switch>("Only Q On W Targets").IsOn = value;
        }

        internal bool OnlyEOnWTargets
        {
            get => ESettings.GetItem<Switch>("Only E On W Targets").IsOn;
            set => ESettings.GetItem<Switch>("Only E On W Targets").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Evelynn)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Only Q On W Targets", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Only E On W Targets", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
