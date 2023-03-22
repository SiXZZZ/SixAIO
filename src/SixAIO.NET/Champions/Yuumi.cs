using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools.Devices;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Yuumi : Champion
    {
        public Yuumi()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                AllowCancelBasicAttack = () => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1750,
                Radius = () => 80,
                Speed = () => 1450,
                IsEnabled = () => UseQ &&
                                  UnitManager.MyChampion.GetCurrentCastingSpell()?.SpellSlot != SpellSlot.R &&
                                  UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "YuumiWAttach" && x.Stacks >= 1),
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                Range = () => 650,
                AllowCancelBasicAttack = () => true,
                IsTargetted = () => true,
                IsEnabled = () => UseW && !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "YuumiWAttach" && x.Stacks >= 1),
                TargetSelect = (mode) =>
                {
                    //YuumiPVisualTracker (1)
                    return UnitManager.AllyChampions
                                    .Where(x => !x.IsMe && x.BuffManager.HasActiveBuff("YuumiPVisualTracker"))
                                    .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 650 && TargetSelector.IsAttackable(ally, false));
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                AllowCancelBasicAttack = () => true,
                IsTargetted = () => true,
                IsEnabled = () => UseE &&
                                  UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "YuumiWAttach" && x.Stacks >= 1),
                TargetSelect = (mode) =>
                {
                    Hero target = null;

                    if (target == null && EBuffAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(x => x.BuffManager.ActiveBuffs.Any(buff => buff.Name == "YuumiWAlly" && buff.Stacks >= 1))
                                            .FirstOrDefault(ally => ally.IsAlive && TargetSelector.IsAttackable(ally, false) && IsCastingSpellOnEnemy(ally));
                    }

                    if (target == null && EShieldAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(x => x.BuffManager.ActiveBuffs.Any(buff => buff.Name == "YuumiWAlly" && buff.Stacks >= 1))
                                            .FirstOrDefault(ally => ally.IsAlive && TargetSelector.IsAttackable(ally, false) &&
                                                            UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && x.IsCastingSpell && x.GetCurrentCastingSpell()?.TargetIndexes?.Any() == true) &&
                                                            ally.HealthPercent < EShieldHealthPercent);
                    }

                    if (target == null && EShieldAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(x => x.BuffManager.ActiveBuffs.Any(buff => buff.Name == "YuumiWAlly" && buff.Stacks >= 1))
                                            .FirstOrDefault(ally => ally.IsAlive && TargetSelector.IsAttackable(ally, false) &&
                                                            UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && x.IsCastingSpell) &&
                                                            ally.HealthPercent < EShieldHealthPercent);
                    }

                    return target;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                AllowCancelBasicAttack = () => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 1100,
                Radius = () => 450,
                Speed = () => 3000,
                Delay = () => 0f,
                IsEnabled = () => UseR &&
                                  UnitManager.MyChampion.GetCurrentCastingSpell()?.SpellSlot != SpellSlot.R &&
                                  UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "YuumiWAttach" && x.Stacks >= 1),
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)).FirstOrDefault()
            };
        }

        private bool IsCastingSpellOnAlly(Hero ally = null)
        {
            foreach (var enemy in UnitManager.EnemyChampions.Where(x => x.IsAlive && x.IsCastingSpell))
            {
                var spell = enemy.GetCurrentCastingSpell();
                if (spell != null)
                {
                    var target = spell.Targets.FirstOrDefault(x => x.IsAlive && x.IsVisible && x.IsTargetable);
                    if (target != null && target.Distance <= 650 && target.IsAlive &&
                        (ally is null ? UnitManager.AllyChampions.Any(x => x.NetworkID == target.NetworkID) : ally.NetworkID == target.NetworkID))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsCastingSpellOnEnemy(Hero ally)
        {
            try
            {
                if (ally.IsAlive && ally.IsCastingSpell)
                {
                    var spell = ally.GetCurrentCastingSpell();
                    if (spell != null && (spell.SpellSlot == SpellSlot.BasicAttack || spell.IsBasicAttack || spell.IsSpecialAttack))
                    {
                        var target = spell.Targets.FirstOrDefault(x => x.IsAlive && x.IsVisible && x.IsTargetable);
                        if (target != null)
                        {
                            return (spell.IsBasicAttack && target.IsAlive && UnitManager.EnemyChampions.Any(x => x.IsAlive && x.NetworkID == target.NetworkID)) ||
                                   (ally.ModelName == "Zeri" && spell.SpellSlot == SpellSlot.Q);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void OnCoreMainTick()
        {
            if (ComboOnTick &&
                UnitManager.MyChampion.IsAlive &&
                !GameEngine.ChatBox.IsChatBoxOpen &&
                GameEngine.IsGameWindowFocused)
            {
                SpellE.ExecuteCastSpell();
                SpellW.ExecuteCastSpell();
                SpellQ.ExecuteCastSpell();
                SpellR.ExecuteCastSpell();
            }

            if (UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "YuumiWAttach" && x.Stacks >= 1))
            {
                if (UseQ && UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "YuumiQCast" && x.Stacks >= 1))
                {
                    var target = SpellQ.TargetSelect(Orbwalker.OrbWalkingModeType.Combo);
                    if (target is not null)
                    {
                        Mouse.SetCursor((int)target.W2S.X, (int)target.W2S.Y);
                    }
                }
                if (UseR)
                {
                    var spell = UnitManager.MyChampion.GetCurrentCastingSpell();
                    if (spell is not null && spell.SpellSlot == SpellSlot.R)
                    {
                        var target = SpellR.TargetSelect(Orbwalker.OrbWalkingModeType.Combo);
                        if (target is not null)
                        {
                            Mouse.SetCursor((int)target.W2S.X, (int)target.W2S.Y);
                        }
                    }
                }
            }
        }

        private bool EBuffAlly
        {
            get => ESettings.GetItem<Switch>("E Buff ally").IsOn;
            set => ESettings.GetItem<Switch>("E Buff ally").IsOn = value;
        }

        private bool EShieldAlly
        {
            get => ESettings.GetItem<Switch>("E Shield ally").IsOn;
            set => ESettings.GetItem<Switch>("E Shield ally").IsOn = value;
        }

        private int EShieldHealthPercent
        {
            get => ESettings.GetItem<Counter>("E Shield Health Percent").Value;
            set => ESettings.GetItem<Counter>("E Shield Health Percent").Value = value;
        }

        private bool ComboOnTick
        {
            get => MenuTab.GetItem<Switch>("Combo On Tick").IsOn;
            set => MenuTab.GetItem<Switch>("Combo On Tick").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Yuumi)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));
            MenuTab.AddItem(new Switch() { Title = "Combo On Tick", IsOn = false });

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Buff ally", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Shield ally", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Shield Health Percent", MinValue = 0, MaxValue = 100, Value = 30, ValueFrequency = 5 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.R);

        }
    }
}
