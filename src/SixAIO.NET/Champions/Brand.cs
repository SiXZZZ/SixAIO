using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Models;
using System;
using System.Linq;
using static Oasys.Common.Logic.Orbwalker;

namespace SixAIO.Champions
{
    internal sealed class Brand : Champion
    {
        public Brand()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1050,
                Radius = () => 120,
                Speed = () => 1600,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => x.BuffManager.HasActiveBuff(x => x.Name.Contains("BrandAblaze") && x.Stacks >= 1) &&
                                                                      !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                                .FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 900,
                Speed = () => 1500,
                Radius = () => 260,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                Range = () => 675,
                IsEnabled = () => UseE,
                TargetSelect = (mode) =>
                {
                    var target = SpellE.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)).FirstOrDefault();
                    if (target is null)
                    {
                        target = UnitManager.EnemyMinions.FirstOrDefault(x => TargetSelector.IsAttackable(x) &&
                                                                              x.Distance <= SpellE.Range() &&
                                                                              x.BuffManager.HasActiveBuff(buff => buff.Name.Contains("BrandAblaze")) &&
                                                                              UnitManager.EnemyChampions.Any(c => c.DistanceTo(x.Position) < 600 && TargetSelector.IsAttackable(c)));
                    }
                    return target;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Range = () => 750,
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) =>
                            target != null &&
                            (UnitManager.Enemies.Count(x => x.Position.Distance(target.Position) < 500) >= 2 || target.Distance < 500),
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (Orbwalker.TargetChampionsOnly && SpellE.CanExecuteCastSpell())
            {
                var tempTargetChamps = OrbSettings.TargetChampionsOnly;
                OrbSettings.TargetChampionsOnly = false;
                SpellE.ExecuteCastSpell();
                OrbSettings.TargetChampionsOnly = tempTargetChamps;
            }
            else
            {
                SpellE.ExecuteCastSpell();
            }

            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Brand)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
