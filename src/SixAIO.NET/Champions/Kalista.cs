using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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

namespace SixAIO.Champions
{
    internal class Kalista : Champion
    {
        private enum Mode
        {
            Champs,
            Jungle,
            Everything,
        }

        private static Mode _mode = Mode.Everything;

        public Oasys.Common.GameObject.ObjectClass.Hero BindedAlly { get; private set; }

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
                ShouldCast = (target, spellClass, damage) => UseQ && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 70 && target != null,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) => ShouldCastE(spellClass),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) =>
                            BindedAlly != null && BindedAlly.IsAlive && BindedAlly.Distance <= 1100 && (BindedAlly.Health / BindedAlly.MaxHealth * 100) < RHealthPercent
                                        ? BindedAlly
                                        : null
            };
        }

        internal bool ShouldCastE(SpellClass spellClass)
        {
            if (UseE && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 40)
            {
                switch (_mode)
                {
                    case Mode.Champs:
                        {
                            return UnitManager.EnemyChampions
                                .Count(x => x.Distance <= 1100 && x.IsAlive &&
                                            TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x) &&
                                                                !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false) &&
                                                                MenuTab.GetItem<Switch>("E - " + x.ModelName).IsOn)
                                 >= 1;
                        }
                    case Mode.Jungle:
                        {
                            return (UnitManager.EnemyJungleMobs.Count(x => x.Distance <= 1100 && x.IsAlive &&
                                           TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x) &&
                                           (x.UnitComponentInfo.SkinName.ToLower().Contains("dragon") ||
                                           x.UnitComponentInfo.SkinName.ToLower().Contains("baron") ||
                                           x.UnitComponentInfo.SkinName.ToLower().Contains("herald")))
                                >= 1) ||
                              (UnitManager.EnemyJungleMobs.Count(x => x.Distance <= 1100 && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x))
                                 >= 3);
                        }
                    case Mode.Everything:
                        {
                            return (UnitManager.EnemyChampions.Count(x => x.Distance <= 1100 && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x) &&
                                                                          !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false) &&
                                                                          MenuTab.GetItem<Switch>("E - " + x.ModelName).IsOn) >= 1) ||
                                (UnitManager.EnemyJungleMobs.Count(x => x.Distance <= 1100 && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x) &&
                                                                          (x.UnitComponentInfo.SkinName.ToLower().Contains("dragon") ||
                                                                          x.UnitComponentInfo.SkinName.ToLower().Contains("baron") ||
                                                                          x.UnitComponentInfo.SkinName.ToLower().Contains("herald")))
                                >= 1) ||
                                (UnitManager.EnemyJungleMobs.Count(x => x.Distance <= 1100 && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x))
                                 >= 3) ||
                                (UnitManager.EnemyMinions.Count(x => x.Distance <= 1100 && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x))
                                >= 3);
                        }
                }
            }

            return false;
        }

        internal static float GetEDamage(GameObjectBase enemy)
        {
            var kalistaE = enemy.BuffManager.GetBuffByName("kalistaexpungemarker");
            if (kalistaE == null || !kalistaE.IsActive)
            {
                return 0;
            }
            var armorMod = DamageCalculator.GetArmorMod(UnitManager.MyChampion, enemy);
            var firstSpearDaamage = 10 + (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 10) + UnitManager.MyChampion.UnitStats.TotalAttackDamage * 0.7;

            var additionalSpearDamage = 4 + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 6 + UnitManager.MyChampion.UnitStats.TotalAttackDamage * GetAdditionalSpearLevelAttackDamageMod();
            var physicalDamage = firstSpearDaamage + additionalSpearDamage * (kalistaE.Stacks - 1);
            var skin = enemy.UnitComponentInfo.SkinName.ToLower();
            if (skin.Contains("dragon") ||
                skin.Contains("baron") ||
                skin.Contains("herald"))
            {
                physicalDamage *= 0.5;
            }
            physicalDamage *= armorMod;
            return (float)(physicalDamage - enemy.NeutralShield - enemy.PhysicalShield);
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
            _mode = Mode.Champs;
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            _mode = Mode.Everything;
            if (SpellE.ExecuteCastSpell())
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
            }
        }

        private int RHealthPercent
        {
            get => MenuTab.GetItem<Counter>("R Health Percent").Value;
            set => MenuTab.GetItem<Counter>("R Health Percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kalista)}"));

            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            foreach (var enemy in UnitManager.EnemyChampions)
            {
                MenuTab.AddItem(new Switch() { Title = "E - " + enemy.ModelName, IsOn = true });
            }

            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R Health Percent", MinValue = 0, MaxValue = 100, Value = 20, ValueFrequency = 5 });
        }
    }
}
