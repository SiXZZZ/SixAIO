using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.Rendering;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Kalista : Champion
    {
        public Hero BindedAlly { get; private set; }

        public Kalista()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1200,
                Radius = () => 80,
                Speed = () => 2400,
                Damage = (target, spellClass) => -45 + (65 * spellClass.Level) + UnitManager.MyChampion.UnitStats.TotalAttackDamage,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => ShouldCastE(mode),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Delay = () => 0f,
                IsEnabled = () => UseR,
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
                Orbwalker.OrbWalkingModeType.Combo => UnitManager.EnemyChampions.Count(IsValidHero) >= EKillChampions,
                Orbwalker.OrbWalkingModeType.LaneClear =>
                            (UnitManager.EnemyChampions.Count(IsValidHero) >= EKillChampions) ||
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
            var firstSpearDaamage = 10 + (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 10) + UnitManager.MyChampion.UnitStats.TotalAttackDamage * 0.7;

            var additionalSpearDamage = 4 +
                                        UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 6 +
                                        UnitManager.MyChampion.UnitStats.TotalAttackDamage * GetAdditionalSpearLevelAttackDamageMod();
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

        internal static float GetAdditionalSpearLevelAttackDamageMod()
        {
            return UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level switch
            {
                1 => 0.232f,
                2 => 0.275f,
                3 => 0.319f,
                4 => 0.363f,
                5 => 0.406f,
                _ => 0,
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreRender()
        {
            if (DrawEDamage && SpellE.SpellClass.IsSpellReady)
            {
                var champs = UnitManager.EnemyChampions.Where(x => x.IsVisible && x.Distance <= 2000 && x.W2S.IsValid() && GetEDamage(x) > 0);
                foreach (var enemy in champs)
                {
                    //var pos = new Vector2(champ.HealthBarScreenPosition.X + 40, champ.HealthBarScreenPosition.Y - 40);
                    //RenderFactory.DrawText($"{(int)GetEDamage(champ)}", 12, pos, Color.White);
                    RenderFactory.DrawHPBarDamage(enemy, GetEDamage(enemy), EDamageColor);
                }
                var minions = UnitManager.EnemyMinions.Where(x => x.IsVisible && x.Distance <= 2000 && x.W2S.IsValid() && GetEDamage(x) > 0);
                foreach (var enemy in minions)
                {
                    //var pos = new Vector2(champ.HealthBarScreenPosition.X + 40, champ.HealthBarScreenPosition.Y - 40);
                    //RenderFactory.DrawText($"{(int)GetEDamage(champ)}", 12, pos, Color.White);
                    RenderFactory.DrawHPBarDamage(enemy, GetEDamage(enemy), EDamageColor);
                }
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

        internal bool DrawEDamage
        {
            get => ESettings.GetItem<Switch>("Draw E Damage").IsOn;
            set => ESettings.GetItem<Switch>("Draw E Damage").IsOn = value;
        }

        public Color EDamageColor => ColorConverter.GetColor(ESettings.GetItem<ModeDisplay>("E Damage Color").SelectedModeName, ESettings.GetItem<Counter>("E Damage Color Alpha").Value);

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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kalista)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Draw E Damage", IsOn = false });
            ESettings.AddItem(new ModeDisplay() { Title = "E Damage Color", ModeNames = ColorConverter.GetColors(), SelectedModeName = "Orange" });
            ESettings.AddItem(new Counter() { Title = "E Damage Color Alpha", MinValue = 0, MaxValue = 255, Value = 75, ValueFrequency = 5 });

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
        }
    }
}
