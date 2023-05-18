using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.Rendering;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Kalista : Champion
    {
        private bool _originalTargetChampsOnlySetting;
        public Hero BindedAlly { get; private set; }

        public Kalista()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1150,
                Radius = () => 80,
                Speed = () => 2400,
                IsEnabled = () => UseQ && (!OnlyQifcantAA || Orbwalker.TargetHero is null) && (UnitManager.MyChampion.AttackSpeed <= QOnlyBelowAttackSpeed || UnitManager.EnemyChampions.All(x => x.Distance > UnitManager.MyChampion.TrueAttackRange)),
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsEnabled = () => UseE,
                Range = () => 1100,
                ShouldCast = (mode, target, spellClass, damage) => ShouldCastE(mode),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                Delay = () => 0f,
                IsEnabled = () => UseR,
                Range = () => 1100,
                ShouldCast = (mode, target, spellClass, damage) => BindedAlly != null && BindedAlly.IsAlive && BindedAlly.Distance <= 1100 && BindedAlly.HealthPercent < RHealthPercent
            };
        }

        internal bool ShouldCastE(Orbwalker.OrbWalkingModeType mode)
        {
            var champs = UnitManager.EnemyChampions.Where(x => GetEDamage(x) > 0);
            var minions = UnitManager.EnemyMinions.Where(IsValidTarget);
            if (ESlowIfCanReset && champs.Any() && minions.Any())
            {
                return true;
            }
            if (EBelowHPPercent >= UnitManager.MyChampion.HealthPercent && (champs.Any() || minions.Any()))
            {
                return true;
            }

            return mode switch
            {
                Orbwalker.OrbWalkingModeType.Combo => (UnitManager.EnemyMinions.Where(IsSpecialMinion).Count(IsValidTarget) >= 1) ||
                                                      UnitManager.EnemyChampions.Count(IsValidHero) >= EKillChampions,
                Orbwalker.OrbWalkingModeType.LaneClear =>
                            (UnitManager.EnemyChampions.Count(IsValidHero) >= EKillChampions) ||
                            (UnitManager.EnemyMinions.Where(IsSpecialMinion).Count(IsValidTarget) >= 1) ||
                            (UnitManager.EnemyJungleMobs.Count(x => IsValidTarget(x) && IsEpicJungleMonster(x)) >= EKillEpicMonsters) ||
                            (UnitManager.EnemyJungleMobs.Count(IsValidTarget) >= EKillMonsters) ||
                            (UnitManager.EnemyMinions.Count(IsValidTarget) >= EKillMinions),
                _ => false
            };
        }

        private bool IsValidHero(Hero target)
        {
            return IsValidTarget(target) &&
                   !TargetSelector.IsInvulnerable(target, Oasys.Common.Logic.DamageType.Physical, false) &&
                   (target.IsTargetDummy || (ESettings.GetItem<Switch>("E - " + target.ModelName)?.IsOn ?? false));
        }

        private static bool IsEpicJungleMonster(JungleMob target)
        {
            return target.UnitComponentInfo.SkinName.ToLower().Contains("dragon") ||
                   target.UnitComponentInfo.SkinName.ToLower().Contains("baron") ||
                   target.UnitComponentInfo.SkinName.ToLower().Contains("herald");
        }

        private static bool IsValidTarget(GameObjectBase target)
        {
            return target.Distance <= 1100 && TargetSelector.IsAttackable(target) && target.Health <= GetEDamage(target);
        }

        internal static float GetEDamage(GameObjectBase enemy)
        {
            var kalistaE = enemy.BuffManager.GetActiveBuff("kalistaexpungemarker");
            if (!enemy.IsAlive || kalistaE == null || !kalistaE.IsActive)
            {
                return 0;
            }
            var armorMod = DamageCalculator.GetArmorMod(UnitManager.MyChampion, enemy);
            var firstSpearDaamage = 10 +
                                    (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 10) +
                                    UnitManager.MyChampion.UnitStats.TotalAttackDamage * 0.7 +
                                    UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.2f;

            var additionalSpearDamage = 4 +
                                        UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 6 +
                                        UnitManager.MyChampion.UnitStats.TotalAttackDamage * GetAdditionalSpearLevelAttackDamageMod() +
                                        UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.2f;
            var physicalDamage = firstSpearDaamage + additionalSpearDamage * (kalistaE.Stacks - 1);
            var skin = enemy.UnitComponentInfo.SkinName.ToLower();
            if (skin.Contains("dragon") ||
                skin.Contains("baron") ||
                skin.Contains("herald"))
            {
                physicalDamage *= 0.5;
            }
            physicalDamage *= armorMod;
            var result = (float)(physicalDamage - enemy.NeutralShield - enemy.PhysicalShield);
            //Logger.Log($"Kalista dmg on {enemy.UnitComponentInfo.SkinName}: {result}");
            return result;
        }

        private static bool IsSpecialMinion(GameObjectBase minion) => minion.IsObject(ObjectTypeFlag.AIMinionClient) && (minion.UnitComponentInfo.SkinName.Contains("MinionSiege", StringComparison.OrdinalIgnoreCase) || minion.UnitComponentInfo.SkinName.Contains("MinionSuper", StringComparison.OrdinalIgnoreCase));

        internal static float GetAdditionalSpearLevelAttackDamageMod()
        {
            return UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level switch
            {
                1 => 0.232f,
                2 => 0.2755f,
                3 => 0.319f,
                4 => 0.3625f,
                5 => 0.406f,
                _ => 0,
            };
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();

            if (SpellE.SpellClass.IsSpellReady)
            {
                if (DrawEDamageChampions)
                {
                    foreach (var enemy in UnitManager.EnemyChampions.ToList().Where(x => x.IsAlive && x.Distance <= 2000 && x.W2S.IsValid()).ToList())
                    {
                        RenderFactory.DrawHPBarDamage(enemy, GetEDamage(enemy), EDamageColor);
                    }
                }
                if (DrawEDamageMinions)
                {
                    foreach (var enemy in UnitManager.EnemyMinions.ToList().Where(x => x.IsAlive && x.Distance <= 2000 && x.W2S.IsValid()).ToList())
                    {
                        RenderFactory.DrawHPBarDamage(enemy, GetEDamage(enemy), EDamageColor);
                    }
                }
                if (DrawEDamageMonsters)
                {
                    foreach (var enemy in UnitManager.EnemyJungleMobs.ToList().Where(x => x.IsAlive && x.Distance <= 2000 && x.W2S.IsValid()).ToList())
                    {
                        RenderFactory.DrawHPBarDamage(enemy, GetEDamage(enemy), EDamageColor);
                    }
                }
            }
        }

        internal override void OnCoreMainInput()
        {
            Orbwalker.SelectedTarget = null;

            if (AALaneclearIfNoComboTarget && Orbwalker.TargetHero is null)
            {
                Orbwalker.SelectedTarget = UnitManager.EnemyMinions.OrderBy(x => x.EffectiveArmorHealth).FirstOrDefault(x => TargetSelector.IsAttackable(x) && TargetSelector.IsInRange(x));
                if (Orbwalker.SelectedTarget is null)
                {
                    Orbwalker.SelectedTarget = UnitManager.EnemyTowers.OrderBy(x => x.EffectiveArmorHealth).FirstOrDefault(x => TargetSelector.IsAttackable(x) && TargetSelector.IsInRange(x));
                    if (Orbwalker.SelectedTarget is null)
                    {
                        Orbwalker.SelectedTarget = UnitManager.EnemyJungleMobs.OrderBy(x => x.EffectiveArmorHealth).FirstOrDefault(x => TargetSelector.IsAttackable(x) && TargetSelector.IsInRange(x));
                        if (Orbwalker.SelectedTarget is null)
                        {
                            Orbwalker.SelectedTarget = UnitManager.EnemyInhibitors.OrderBy(x => x.EffectiveArmorHealth).FirstOrDefault(x => TargetSelector.IsAttackable(x) && TargetSelector.IsInRange(x));
                        }
                    }
                }
            }

            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (UseEHarass && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseELaneclear && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (UseELasthit && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        private int _cycles = 0;
        internal override void OnCoreMainTick()
        {
            _cycles++;
            if (BindedAlly is null && _cycles % 10 == 0 && UnitManager.MyChampion.Level >= 6)
            {
                BindedAlly = UnitManager.AllyChampions.FirstOrDefault(ally => ally.BuffManager.GetBuffList().Any(x => x.Name.Contains("kalistacoopstrikeally", StringComparison.OrdinalIgnoreCase)));
                if (BindedAlly is not null)
                {
                    Logger.Log("Binded Ally: " + BindedAlly.Name + " " + BindedAlly.ModelName);
                }
            }
        }

        private bool AALaneclearIfNoComboTarget
        {
            get => MenuTab?.GetItem<Switch>("AA Laneclear If No Combo Target")?.IsOn ?? false;
            set => MenuTab.GetItem<Switch>("AA Laneclear If No Combo Target").IsOn = value;
        }

        private float QOnlyBelowAttackSpeed
        {
            get => QSettings.GetItem<FloatCounter>("Q Only Below Attack Speed").Value;
            set => QSettings.GetItem<FloatCounter>("Q Only Below Attack Speed").Value = value;
        }

        internal bool OnlyQifcantAA
        {
            get => QSettings.GetItem<Switch>("Only Q if cant AA").IsOn;
            set => QSettings.GetItem<Switch>("Only Q if cant AA").IsOn = value;
        }

        private bool DrawEDamageChampions
        {
            get => ESettings.GetItem<Switch>("Draw E Damage Champions").IsOn;
            set => ESettings.GetItem<Switch>("Draw E Damage Champions").IsOn = value;
        }

        private bool DrawEDamageMinions
        {
            get => ESettings.GetItem<Switch>("Draw E Damage Minions").IsOn;
            set => ESettings.GetItem<Switch>("Draw E Damage Minions").IsOn = value;
        }

        private bool DrawEDamageMonsters
        {
            get => ESettings.GetItem<Switch>("Draw E Damage Monsters").IsOn;
            set => ESettings.GetItem<Switch>("Draw E Damage Monsters").IsOn = value;
        }

        public Color EDamageColor => ColorConverter.GetColor(ESettings.GetItem<ModeDisplay>("E Damage Color").SelectedModeName, 255);

        internal bool ESlowIfCanReset
        {
            get => ESettings.GetItem<Switch>("E Slow If Can Reset").IsOn;
            set => ESettings.GetItem<Switch>("E Slow If Can Reset").IsOn = value;
        }

        private int EBelowHPPercent
        {
            get => ESettings.GetItem<Counter>("E Below HP Percent").Value;
            set => ESettings.GetItem<Counter>("E Below HP Percent").Value = value;
        }

        private int EKillMinions
        {
            get => ESettings.GetItem<Counter>("E Kill Minions").Value;
            set => ESettings.GetItem<Counter>("E Kill Minions").Value = value;
        }

        private int EKillMonsters
        {
            get => ESettings.GetItem<Counter>("E Kill Monsters").Value;
            set => ESettings.GetItem<Counter>("E Kill Monsters").Value = value;
        }

        private int EKillEpicMonsters
        {
            get => ESettings.GetItem<Counter>("E Kill Epic Monsters").Value;
            set => ESettings.GetItem<Counter>("E Kill Epic Monsters").Value = value;
        }

        private int EKillChampions
        {
            get => ESettings.GetItem<Counter>("E Kill Champions").Value;
            set => ESettings.GetItem<Counter>("E Kill Champions").Value = value;
        }

        private int RHealthPercent
        {
            get => RSettings.GetItem<Counter>("R Health Percent").Value;
            set => RSettings.GetItem<Counter>("R Health Percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            TabItem.OnTabItemChange += TabItem_OnTabItemChange;
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kalista)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            MenuTab.AddItem(new Switch() { Title = "AA Laneclear If No Combo Target", IsOn = true });

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Only Q if cant AA", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            QSettings.AddItem(new FloatCounter() { Title = "Q Only Below Attack Speed", MinValue = 0.5f, MaxValue = 5.0f, Value = 2.5f, ValueFrequency = 0.1f });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Lasthit", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Harass", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Draw E Damage Champions", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Draw E Damage Minions", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Draw E Damage Monsters", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E Damage Color", ModeNames = ColorConverter.GetColors(), SelectedModeName = "White" });

            ESettings.AddItem(new Counter() { Title = "E Below HP Percent", MinValue = 0, MaxValue = 100, Value = 5, ValueFrequency = 5 });
            ESettings.AddItem(new Switch() { Title = "E Slow If Can Reset", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Kill Minions", MinValue = 0, MaxValue = 10, Value = 3, ValueFrequency = 1 });
            ESettings.AddItem(new Counter() { Title = "E Kill Monsters", MinValue = 0, MaxValue = 10, Value = 3, ValueFrequency = 1 });
            ESettings.AddItem(new Counter() { Title = "E Kill Epic Monsters", MinValue = 0, MaxValue = 10, Value = 1, ValueFrequency = 1 });
            ESettings.AddItem(new Counter() { Title = "E Kill Champions", MinValue = 0, MaxValue = 10, Value = 1, ValueFrequency = 1 });
            foreach (var enemy in UnitManager.EnemyChampions.Where(x => !x.IsTargetDummy))
            {
                ESettings.AddItem(new Switch() { Title = "E - " + enemy.ModelName, IsOn = true });
            }

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Health Percent", MinValue = 0, MaxValue = 100, Value = 20, ValueFrequency = 5 });

            _originalTargetChampsOnlySetting = Oasys.Common.Settings.Orbwalker.HoldTargetChampsOnly;
            SetTargetChampsOnly(false);


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.E, SpellSlot.R);
        }

        private void SetTargetChampsOnly(bool value)
        {
            try
            {
                Oasys.Common.Settings.Orbwalker.HoldTargetChampsOnly = value;
            }
            catch (Exception ex)
            {
                //Logger.Log(ex.Message);
            }
        }

        private void TabItem_OnTabItemChange(string tabName, TabItem tabItem)
        {
            if (AALaneclearIfNoComboTarget &&
                tabItem.TabName == "Orbwalker" &&
                tabItem.GroupName == "Input" &&
                tabItem.Title == "Hold Target Champs Only" &&
                tabItem is Switch targetChamps &&
                targetChamps.IsOn)
            {
                SetTargetChampsOnly(false);
            }

            if (tabItem.TabName == $"SIXAIO - {nameof(Kalista)}" &&
                tabItem.Title == "AA Laneclear If No Combo Target" &&
                tabItem is Switch aaLaneClear)
            {
                SetTargetChampsOnly(!aaLaneClear.IsOn);
            }
        }

        internal override void OnGameMatchComplete()
        {
            try
            {
                TabItem.OnTabItemChange -= TabItem_OnTabItemChange;
                Oasys.Common.Settings.Orbwalker.HoldTargetChampsOnly = _originalTargetChampsOnlySetting;
            }
            catch (Exception ex)
            {
                //Logger.Log(ex.Message);
            }
        }
    }
}
