using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using Oasys.Common.Menu;
using Oasys.SDK;
using Oasys.Common.GameObject;
using SixAIO.Extensions;

namespace SixAIO.Champions
{
    internal sealed class Viego : Champion
    {
        public Viego()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 600,
                Speed = () => 3000,
                Radius = () => 120,
                Delay = () => 0.35f,
                IsEnabled = () => UseQ && SpellQ.SpellClass.SpellData.SpellName == "ViegoQ",
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                AllowCollision = (target, collisions) => false,
                IsCharge = () => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () =>
                {
                    var range = SpellW.ChargeTimer.ElapsedMilliseconds switch
                    {
                        > 1000 => 900,
                        > 750 => 800,
                        > 500 => 700,
                        > 250 => 600,
                        _ => 500f,
                    };
                    var result = SpellW.ChargeTimer.IsRunning
                                ? SpellW.SpellClass.IsSpellReady
                                        ? range
                                        : 500
                                : 900;
                    return result;
                },
                Delay = () => 0f,
                Speed = () => 1300,
                Radius = () => 120,
                IsEnabled = () => UseW && SpellW.SpellClass.SpellData.SpellName == "ViegoW",
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                ShouldCast = (mode, target, spellClass, damage) =>
                            target != null &&
                            (SpellW.ChargeTimer.IsRunning
                                ? target.Distance < SpellW.Range()
                                : target.Distance < 900),
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 500,
                Speed = () => 2500,
                Radius = () => 300,
                Delay = () => 0.5f,
                IsEnabled = () => UseR && SpellR.SpellClass.SpellData.SpellName == "ViegoR",
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.Health <= RDamage(x)).FirstOrDefault()
            };
        }

        private float RDamage(GameObjectBase target)
        {
            if (target is null)
            {
                return 0;
            }

            var spellLevel = SpellR.SpellClass.Level;
            var baseDamage = 8 + 4 * spellLevel;
            var bonusDamage = 0.05f * UnitManager.MyChampion.UnitStats.BonusAttackDamage;
            var totalScale = baseDamage + bonusDamage;
            var damage = totalScale / 100 * target.MissingHealth + 1.2f * UnitManager.MyChampion.UnitStats.TotalAttackDamage;
            var critMod = UnitManager.MyChampion.UnitStats.Crit;
            damage *= 1 + critMod;
            var actualDmg = DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, damage);
            return actualDmg;
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
            SpellW.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        //modelname ViegoSoul for posses targets

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Viego)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });


            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);

        }
    }
}
