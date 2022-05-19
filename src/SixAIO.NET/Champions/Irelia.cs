using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Irelia : Champion
    {
        private bool _originalTargetChampsOnlySetting;
        private AIBaseClient _ireliaE;

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

                    return null;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Speed = () => 2000,
                Range = () => IsCastingE ? 30_000 : 775,
                Radius = () => 80,
                Delay = () => 0.1f,
                IsEnabled = () => UseE,
                From = () => IsCastingE ? _ireliaE.Position : UnitManager.MyChampion.AIManager.ServerPosition,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Speed = () => 2000,
                Range = () => 1000f,
                Radius = () => 320,
                Delay = () => 0.4f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => UnitManager.EnemyChampions
                                            .FirstOrDefault(x => x.Distance <= SpellR.Range() &&
                                                                 TargetSelector.IsAttackable(x) &&
                                                                 BuffChecker.IsCrowdControlledOrSlowed(x))
            };
        }

        private bool ShouldQ(GameObjectBase target, Orbwalker.OrbWalkingModeType mode)
        {
            if (mode == Orbwalker.OrbWalkingModeType.Combo)
            {
                if (target.IsObject(ObjectTypeFlag.AIHeroClient))
                {
                    return target.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(target) && CanQResetOnTarget(target);
                }

                return target.Distance <= SpellQ.Range() && UnitManager.EnemyChampions.Any(enemy => target.DistanceTo(enemy.Position) <= SpellQ.Range()) && TargetSelector.IsAttackable(target) && CanQResetOnTarget(target);
            }

            return target.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(target) && CanQResetOnTarget(target);
        }

        private bool IsCastingE => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).SpellData.SpellName.Equals("IreliaE", StringComparison.OrdinalIgnoreCase) && _ireliaE is not null;

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
                  ? HasIreliaMark(target) || target.Health <= GetQDamage(target, UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q))
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
            var nextAA = DamageCalculator.GetNextBasicAttackDamage(UnitManager.MyChampion, target);
            return (DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                   (nextAA + minionDmg + (UnitManager.MyChampion.UnitStats.TotalAttackDamage * 0.60f) + (-15) + 20 * spellClass.Level));
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.E)
            {
                SpellQ.ExecuteCastSpell();
            }

            if (spell.SpellSlot == SpellSlot.Q)
            {
                SpellR.ExecuteCastSpell();
            }
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            SpellQ.ExecuteCastSpell();
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || /*SpellW.ExecuteCastSpell() ||*/ SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
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

        internal override void InitializeMenu()
        {
            TabItem.OnTabItemChange += TabItem_OnTabItemChange;
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Irelia)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });

            SetTargetChampsOnly();
        }

        private void SetTargetChampsOnly()
        {
            try
            {
                var orbTab = MenuManagerProvider.GetTab("Orbwalker");
                var orbGroup = orbTab.GetGroup("Input");
                _originalTargetChampsOnlySetting = orbGroup.GetItem<Switch>("Hold Target Champs Only").IsOn;
                orbGroup.GetItem<Switch>("Hold Target Champs Only").IsOn = false;
            }
            catch (Exception ex)
            {
                //Logger.Log(ex.Message);
            }
        }

        private void TabItem_OnTabItemChange(string tabName, TabItem tabItem)
        {
            if (tabItem.TabName == "Orbwalker" &&
                tabItem.GroupName == "Input" &&
                tabItem.Title == "Hold Target Champs Only" &&
                tabItem is Switch itemSwitch &&
                itemSwitch.IsOn)
            {
                SetTargetChampsOnly();
            }
        }

        internal override void OnGameMatchComplete()
        {
            try
            {
                TabItem.OnTabItemChange -= TabItem_OnTabItemChange;

                var orbTab = MenuManagerProvider.GetTab("Orbwalker");
                var orbGroup = orbTab.GetGroup("Input");
                orbGroup.GetItem<Switch>("Hold Target Champs Only")
                        .IsOn = _originalTargetChampsOnlySetting;
            }
            catch (Exception ex)
            {
                //Logger.Log(ex.Message);
            }
        }
    }
}
