using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Karthus : Champion
    {
        private static string _killMessage;

        private static bool IsEActive()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("KarthusDefile", false, true);
            return buff != null && buff.IsActive && buff.Stacks >= 1;
        }

        public Karthus()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 875,
                Speed = () => 1200,
                Radius = () => 160,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Range = () => 550f,
                Delay = () => 0f,
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    var usingDefile = IsEActive();
                    var targetInRange = UnitManager.EnemyChampions.Any(x => x.Distance < SpellE.Range() && TargetSelector.IsAttackable(x));

                    if (usingDefile && targetInRange)
                    {
                        return false;
                    }
                    else if (!usingDefile && targetInRange)
                    {
                        return true;
                    }
                    else if (usingDefile && !targetInRange)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                RenderSpellUsage = () =>
                {
                    var enemies = UnitManager.EnemyChampions.Where(x => x.IsAlive && x.IsTargetable &&
                                             !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false) &&
                                             RCanKill(x));
                    if (enemies.Any())
                    {
                        _killMessage = $"Can kill: ";
                        foreach (var enemy in enemies)
                        {
                            _killMessage += $"{enemy.ModelName} ";
                        }
                    }
                    else
                    {
                        _killMessage = string.Empty;
                    }
                }
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

            var dmg = 50 + (150 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level) + (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.75f);
            return DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) * dmg;
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreMainTick()
        {
            if (SpellR.ExecuteCastSpell())
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

        internal override void OnCoreRender()
        {
            if (!string.IsNullOrEmpty(_killMessage))
            {
                var pos = new Vector2() { X = LeagueNativeRendererManager.GetWindowsScreenResolution().X / 2, Y = 100 };
                Oasys.SDK.Rendering.RenderFactory.DrawText(_killMessage, 12, pos, Color.Red);
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Karthus)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
