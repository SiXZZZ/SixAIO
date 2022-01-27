using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Collections.Generic;
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
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Range = () => 550f,
                CastTime = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                {
                    if (UseE && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 40)
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

                    return false;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100,
                AlertSpellUsage = (spellSlot) =>
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
            if (SpellE.ExecuteCastSpell())
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
            //MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
