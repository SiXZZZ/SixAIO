﻿using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;
using static Oasys.Common.Logic.Orbwalker;

namespace SixAIO.Champions
{
    internal sealed class Irelia : Champion
    {
        private bool _originalTargetChampsOnlySetting;
        private AIBaseClient _ireliaE;

        private static float QStacks()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("ireliapassivestacks", false, true);
            return buff is null ? 0 : buff.Stacks;
        }

        private static bool AllSpellsOnCooldown()
        {
            var q = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q);
            var w = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W);
            var e = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E);
            return !q.IsSpellReady && !w.IsSpellReady && !e.IsSpellReady;
        }

        public Irelia()
        {
            SDKSpell.OnSpellCast += Spell_OnSpellCast;
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                IsTargetted = () => true,
                Speed = () => 1400 + UnitManager.MyChampion.UnitStats.MoveSpeed,
                Range = () => 600f,
                Delay = () => 0f,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.Combo)
                    {
                        var champ = UnitManager.EnemyChampions.FirstOrDefault(x => ShouldQ(x, mode));
                        if (champ != null)
                        {
                            return champ;
                        }
                    }

                    var champReset = UnitManager.EnemyChampions.FirstOrDefault(x => ShouldQ(x, mode));
                    if (champReset != null)
                    {
                        return champReset;
                    }

                    var minionReset = UnitManager.EnemyMinions.FirstOrDefault(x => ShouldQ(x, mode));
                    if (minionReset != null)
                    {
                        return minionReset;
                    }

                    var jungleReset = UnitManager.EnemyJungleMobs.FirstOrDefault(x => ShouldQ(x, mode));
                    if (jungleReset != null)
                    {
                        return jungleReset;
                    }

                    return EngineManager.MissionInfo.GameType == GameTypes.SoulFighterArena
                            ? UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(x))
                            : null;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Speed = () => 2000,
                Range = () => 775,
                Radius = () => 80,
                Delay = () => 0.5f,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Speed = () => 2000,
                Range = () => RMaximumRange,
                Radius = () => 320,
                Delay = () => 0.4f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        private bool ShouldQ(GameObjectBase target, Orbwalker.OrbWalkingModeType mode)
        {
            if (!ShouldQ(target))
            {
                return false;
            }

            if (mode == Orbwalker.OrbWalkingModeType.Combo)
            {
                if (target.IsObject(ObjectTypeFlag.AIHeroClient))
                {
                    return target.Distance <= SpellQ.Range() &&
                           TargetSelector.IsAttackable(target) &&
                           CanQResetOnTarget(target);
                }

                return (QMinionsMaxStacks || QStacks() < 4) &&
                       target.Distance <= SpellQ.Range() &&
                       UnitManager.EnemyChampions.Any(enemy => target.DistanceTo(enemy.Position) <= SpellQ.Range() && target.DistanceTo(enemy.Position) < enemy.Distance) &&
                       TargetSelector.IsAttackable(target) &&
                       CanQResetOnTarget(target);
            }

            return target.Distance <= SpellQ.Range() &&
                   TargetSelector.IsAttackable(target) &&
                   CanQResetOnTarget(target);
        }

        private bool IsCastingE => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).SpellData.SpellName.Equals("IreliaE", StringComparison.OrdinalIgnoreCase) && _ireliaE is not null;

        private static readonly Vector3 _orderNexusPos = new Vector3(405, 95, 425);
        private static readonly Vector3 _chaosNexusPos = new Vector3(14300, 90, 14400);

        private bool ShouldQ(GameObjectBase target)
        {
            return AllowQInTowerRange ||
                (UnitManager.EnemyTowers.Where(x => x.IsAlive).All(x => x.Position.Distance(target.Position) >= 850) &&
                target.Position.Distance(_orderNexusPos) >= 1000 &&
                target.Position.Distance(_chaosNexusPos) >= 1000);
        }

        private float GetMissingHealthPercent(GameObjectBase target)
        {
            var missingHealthPercent = 100f - (target.Health / target.MaxHealth * 100f);
            return missingHealthPercent;
        }

        private float GetRDamage(GameObjectBase target, SpellClass spellClass)
        {
            if (target == null)
            {
                return 0;
            }
            var extraDamagePercent = GetMissingHealthPercent(target) * 2.667f;
            if (extraDamagePercent > 200f)
            {
                extraDamagePercent = 200f;
            }
            return DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) * ((1 + (extraDamagePercent / 100f)) *
                   ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.60f) + 50 + 50 * spellClass.Level));
        }

        private bool CanQResetOnTarget(GameObjectBase target)
        {
            return target != null && target.IsAlive && target.Distance <= 600 && TargetSelector.IsAttackable(target)
                  ? HasIreliaMark(target) ||
                    target.Health <= GetQDamage(target, UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q))
                  : false;
        }

        private static bool HasIreliaMark(GameObjectBase target)
        {
            var buff = target.BuffManager.GetBuffByName("ireliamark", false, true);
            return buff != null && buff.IsActive && buff.Stacks >= 1;
        }

        private float GetQDamage(GameObjectBase target, SpellClass spellClass)
        {
            if (target == null)
            {
                return 0;
            }
            var minionDmg = target.IsObject(ObjectTypeFlag.AIMinionClient) ? 43 + 12 * UnitManager.MyChampion.Level : 0;
            var nextAA = DamageCalculator.GetNextBasicAttackDamage(UnitManager.MyChampion, target) - UnitManager.MyChampion.UnitStats.TotalAttackDamage;
            return (DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                   (nextAA + minionDmg + (UnitManager.MyChampion.UnitStats.TotalAttackDamage * 0.60f) + (-15) + 20 * spellClass.Level));
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.E)
            {
                SpellQ.ExecuteCastSpell();
            }
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            SpellQ.ExecuteCastSpell();
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (Orbwalker.TargetChampionsOnly && SpellQ.CanExecuteCastSpell())
            {
                var tempTargetChamps = OrbSettings.TargetChampionsOnly;
                OrbSettings.TargetChampionsOnly = false;
                var casted = SpellQ.ExecuteCastSpell();
                OrbSettings.TargetChampionsOnly = tempTargetChamps;
                if (casted)
                {
                    return;
                }
            }
            else
            {
                if (SpellQ.ExecuteCastSpell())
                {
                    return;
                }
            }

            if (SpellE.ExecuteCastSpell() || /*SpellW.ExecuteCastSpell() ||*/ SpellR.ExecuteCastSpell())
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

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (obj.Name.Contains("IreliaE", StringComparison.OrdinalIgnoreCase))
            {
                _ireliaE = obj;
            }
        }
        internal bool QMinionsMaxStacks
        {
            get => QSettings.GetItem<Switch>("Q Minions in combo on max stacks").IsOn;
            set => QSettings.GetItem<Switch>("Q Minions in combo on max stacks").IsOn = value;
        }

        internal bool AllowQInTowerRange
        {
            get => QSettings.GetItem<Switch>("Allow Q in tower range").IsOn;
            set => QSettings.GetItem<Switch>("Allow Q in tower range").IsOn = value;
        }

        private int RMaximumRange
        {
            get => RSettings.GetItem<Counter>("R maximum range").Value;
            set => RSettings.GetItem<Counter>("R maximum range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Irelia)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q Minions in combo on max stacks", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Allow Q in tower range", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 1000, Value = 950, ValueFrequency = 50 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.E, SpellSlot.R);

        }
    }
}
