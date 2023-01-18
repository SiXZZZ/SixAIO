using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Syndra : Champion
    {
        private List<AIBaseClient> _orbs = new List<AIBaseClient>();

        private List<AIBaseClient> Orbs => _orbs.Where(IsOrb).ToList();

        private static bool IsOrb(AIBaseClient obj)
        {
            return obj is not null && obj.Distance <= 2000 && obj.IsAlive && obj.Position.IsValid() &&
                   obj.Name.Contains("Syndra_", StringComparison.OrdinalIgnoreCase) &&
                   obj.Name.Contains("_Q_", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsWActive => UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "syndrawtooltip" && x.Stacks >= 1);

        public Syndra()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 800,
                Speed = () => 1000,
                Radius = () => 180,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => !IsWActive && SpellQ.GetTargets(mode).FirstOrDefault() is not null
                                            ? Orbs.FirstOrDefault(x => x.Distance <= 925) ?? (GameObjectBase)UnitManager.EnemyMinions.FirstOrDefault(x => x.Distance <= 925)
                                            : SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                TargetSelect = (mode) =>
                {
                    var targets = UnitManager.EnemyChampions
                                                .Where(x => x.IsAlive && x.Distance <= 1000 &&
                                                            TargetSelector.IsAttackable(x) &&
                                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false));

                    foreach (var target in targets)
                    {
                        var targetOrb = GetOrbsBetweenMeAndEnemy(target, 180);
                        if (targetOrb != null && targetOrb.Any())
                        {
                            return targetOrb.FirstOrDefault();
                        }
                    }

                    if (UsePushAway)
                    {
                        return PushAwayModeSelected switch
                        {
                            PushAwayMode.Melee => targets.FirstOrDefault(x => x.CombatType == CombatTypes.Melee && x.Distance < PushAwayRange),
                            PushAwayMode.LowerThanMyRange => targets.FirstOrDefault(x => x.AttackRange < UnitManager.MyChampion.AttackRange && x.Distance < PushAwayRange),
                            PushAwayMode.DashNearMe => targets.FirstOrDefault(x => x.AIManager.IsDashing && UnitManager.MyChampion.DistanceTo(x.AIManager.NavEndPosition) < PushAwayRange),
                            PushAwayMode.Everything => targets.FirstOrDefault(x => x.Distance < PushAwayRange),
                            _ => null,
                        };
                    }

                    return null;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Delay = () => 0f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => UnitManager.EnemyChampions.Where(x => x.Distance <= (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level >= 3 ? 750 : 675) &&
                                            TargetSelector.IsAttackable(x) &&
                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                            .FirstOrDefault(RCanKill)
            };
        }

        private IEnumerable<GameObjectBase> GetOrbsBetweenMeAndEnemy(Hero enemy, int width)
        {
            return Orbs.Where(orb => orb is not null && orb.Distance <= 800 &&
                        Geometry.DistanceFromPointToLine(enemy.W2S, new Vector2[] { UnitManager.MyChampion.W2S, orb.W2S }) <= width &&
                        orb.W2S.Distance(enemy.W2S) < UnitManager.MyChampion.W2S.Distance(enemy.W2S));
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

            var charges = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Charges;
            charges = charges > 7 ? 7 : charges;
            var chargeDamage = 40 + (50 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level) + (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.2f);
            return DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) * (charges * chargeDamage);
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsOrb(obj))
            {
                _orbs.Add(obj);
            }
        }

        internal override void OnDeleteObject(AIBaseClient obj)
        {
            if (!IsOrb(obj))
            {
                _orbs.Remove(obj);
            }
        }

        //internal override void OnCoreRender()
        //{
        //    try
        //    {
        //        if (UnitManager.MyChampion.IsAlive)
        //        {
        //            var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);
        //            var color = Color.Blue;

        //            foreach (var orb in Orbs)
        //            {
        //                var orbW2S = LeagueNativeRendererManager.WorldToScreenSpell(orb.Position);
        //                if (!orbW2S.IsZero)
        //                {
        //                    Oasys.SDK.Rendering.RenderFactory.DrawLine(w2s.X, w2s.Y, orbW2S.X, orbW2S.Y, 2, color);
        //                    Oasys.SDK.Rendering.RenderFactory.DrawText(orb.Name, 18, orbW2S, color);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private bool UsePushAway
        {
            get => ESettings.GetItem<Switch>("Use Push Away").IsOn;
            set => ESettings.GetItem<Switch>("Use Push Away").IsOn = value;
        }

        private int PushAwayRange
        {
            get => ESettings.GetItem<Counter>("Push Away Range").Value;
            set => ESettings.GetItem<Counter>("Push Away Range").Value = value;
        }

        private PushAwayMode PushAwayModeSelected
        {
            get => (PushAwayMode)Enum.Parse(typeof(PushAwayMode), ESettings.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName = value.ToString();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Syndra)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use Push Away", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "Push Away Range", MinValue = 50, MaxValue = 800, Value = 250, ValueFrequency = 25 });
            ESettings.AddItem(new ModeDisplay() { Title = "Push Away Mode", ModeNames = PushAwayHelper.ConstructPushAwayModeTable(), SelectedModeName = "Everything" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
