using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal class Camille : Champion
    {
        internal Spell SpellQ2;

        public Camille()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) => IsQFirstCast && TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellQ2 = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                                        UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "camilleqprimingcomplete" && x.Stacks >= 1) &&
                                        IsQSecondCast &&
                                        TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                                        TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => WHitChance,
                Range = () => 550,
                Radius = () => 70f,
                Speed = () => 2000,
                Delay = () => 0f,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Range = () => 475,
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) =>
                            target != null &&
                            UnitManager.Enemies.Count(x => x.Position.Distance(target.Position) < 500) <= 2,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)).FirstOrDefault()
            };
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, Oasys.Common.GameObject.GameObjectBase target)
        {
            SpellQ.ExecuteCastSpell();
        }

        private bool IsQFirstCast => SpellQ.SpellClass.SpellData.SpellName == "CamilleQ";
        private bool IsQSecondCast => SpellQ.SpellClass.SpellData.SpellName == "CamilleQ2";

        internal override void OnCoreMainInput()
        {
            if (SpellQ2.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Camille)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });

        }
    }
}
