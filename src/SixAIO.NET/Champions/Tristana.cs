using Newtonsoft.Json;
using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SixAIO.Champions
{
    internal sealed class Tristana : Champion
    {
        private static TargetSelection _targetSelection;

        private float GetRDamage(GameObjectBase target)
        {
            if (target is null)
            {
                return 0;
            }

            var eTarget = GetETarget(UnitManager.EnemyChampions);
            var extraDamage = 0f;
            if (eTarget is not null && eTarget.NetworkID == target.NetworkID)
            {
                var bombBuff = target.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name == "tristanaecharge" && x.Stacks >= 1);
                //Logger.Log(bombBuff);
                var damage = 60f + 10f * SpellE.SpellClass.Level;
                damage += 0.5f * UnitManager.MyChampion.UnitStats.TotalAbilityPower;
                var bonusADScale = (25 + 25 * SpellE.SpellClass.Level) / 100;
                damage += bonusADScale * UnitManager.MyChampion.UnitStats.BonusAttackDamage;

                var damagePerStack = 18f + 3f * SpellE.SpellClass.Level;
                var bonusADScalePerStack = (7.5f + 7.5f * SpellE.SpellClass.Level) / 100;
                damagePerStack += bonusADScalePerStack * UnitManager.MyChampion.UnitStats.BonusAttackDamage;
                damagePerStack += 0.15f * UnitManager.MyChampion.UnitStats.TotalAbilityPower;

                damage += damagePerStack * bombBuff.Stacks + 1;
                extraDamage = DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) * damage;
            }

            var ultDamage = DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                   (UnitManager.MyChampion.UnitStats.TotalAbilityPower + 200 + 100 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level);
            return ultDamage + extraDamage;
        }

        public Tristana()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            Oasys.Common.EventsProvider.GameEvents.OnGameProcessSpell += GameEvents_OnGameProcessSpell;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsEnabled = () => UseQ && !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "TristanaQ" && x.Stacks >= 1),
                ShouldCast = (mode, target, spellClass, damage) => GetETarget(UnitManager.EnemyChampions) is not null
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCancelBasicAttack = () => true,
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                MinimumMana = () => 70f,
                TargetSelect = (mode) => UseTargetselector
                ? Oasys.Common.Logic.TargetSelector.GetBestHeroTarget(Orbwalker.SelectedHero, x => x.Distance <= ETargetRange && !x.BuffManager.ActiveBuffs.Any(x => x.Name == "tristanaecharge" && x.Stacks >= 1))
                : GetPrioritizationTarget(),
                ShouldCast = (mode, target, spellClass, damage) => TargetSelector.IsAttackable(target) && TargetSelector.IsInRange(target)
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseR,
                MinimumMana = () => 100f,
                TargetSelect = (mode) => TargetSelectR()
            };
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.E)
            {
                Orbwalker.AllowMoving = false;
                Oasys.Common.Logic.Orbwalker.OrbSettings.LastBasicAttack = EngineManager.GameTime + UnitManager.MyChampion.GetAttackCastDelay();
                DelayAction.Add(250 + (int)(UnitManager.MyChampion.GetAttackCastDelay() * 1000), () =>
                {
                    Orbwalker.AllowMoving = true;
                });
            }
        }

        private Task GameEvents_OnGameProcessSpell(AIBaseClient objectProcessingSpell, SpellActiveEntry processingSpellEntry)
        {
            if (Orbwalker.OrbwalkingMode == Orbwalker.OrbWalkingModeType.Combo && objectProcessingSpell.IsMe && processingSpellEntry.SpellSlot == SpellSlot.W)
            {
                SpellE.ExecuteCastSpell();
            }

            return Task.CompletedTask;
        }

        private Hero TargetSelectR()
        {
            var targets = UnitManager.EnemyChampions.Where(x => x is not null)
                                                    .Where(x => !x.IsTargetDummy)
                                                    .Where(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange &&
                                                                TargetSelector.IsAttackable(x) &&
                                                                !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false) &&
                                                                (RSettings?.GetItem<Switch>("R - " + x.ModelName)?.IsOn ?? false))
                                                    .OrderBy(x => x.Health);

            if (!targets.Any())
            {
                targets = UnitManager.EnemyChampions.Where(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && TargetSelector.IsAttackable(x)).OrderBy(x => x.Health);
            }

            var target = targets.FirstOrDefault(x => DamageCalculator.GetTargetHealthAfterBasicAttack(UnitManager.MyChampion, x) + x.NeutralShield + x.MagicalShield + 100 < GetRDamage(x));
            if (target != null)
            {
                return target;
            }

            if (UsePushAway && UnitManager.MyChampion.HealthPercent <= OnlyPushAwayBelowHPPercent)
            {
                return PushAwayModeSelected switch
                {
                    PushAwayMode.Melee => targets.FirstOrDefault(x => x.CombatType == CombatTypes.Melee && x.Distance < PushAwayRange),
                    PushAwayMode.LowerThanMyRange => targets.FirstOrDefault(x => x.AttackRange < UnitManager.MyChampion.AttackRange && x.Distance < PushAwayRange),
                    PushAwayMode.DashNearMe => targets.FirstOrDefault(x => x.AIManager.IsDashing && UnitManager.MyChampion.DistanceTo(x.AIManager.NavEndPosition) < 300 && x.Distance < PushAwayRange),
                    PushAwayMode.Everything => targets.FirstOrDefault(x => x.Distance < PushAwayRange),
                    _ => null,
                };
            }

            return null;
        }

        internal override void OnCoreMainInput()
        {
            Orbwalker.SelectedTarget = GetETarget(UnitManager.EnemyChampions);

            SpellQ.ExecuteCastSpell();

            if (SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            Orbwalker.SelectedTarget = GetETarget(UnitManager.EnemyChampions);
            if (Orbwalker.SelectedTarget is null)
            {
                Orbwalker.SelectedTarget = GetETarget(UnitManager.EnemyMinions);
                if (Orbwalker.SelectedTarget is null)
                {
                    Orbwalker.SelectedTarget = GetETarget(UnitManager.EnemyJungleMobs);
                    Orbwalker.SelectedTarget ??= GetETarget(UnitManager.EnemyTowers);
                }
            }

            SpellQ.ExecuteCastSpell();
        }

        private static GameObjectBase GetETarget<T>(List<T> enemies) where T : GameObjectBase
        {
            return enemies.FirstOrDefault(enemy =>
                                TargetSelector.IsAttackable(enemy) &&
                                TargetSelector.IsInRange(enemy) &&
                                enemy.BuffManager.ActiveBuffs.Any(buff => buff.Name == "tristanaecharge" && buff.Stacks >= 1));
        }

        private bool UsePushAway
        {
            get => RSettings.GetItem<Switch>("Use Push Away").IsOn;
            set => RSettings.GetItem<Switch>("Use Push Away").IsOn = value;
        }

        private int PushAwayRange
        {
            get => RSettings.GetItem<Counter>("Push Away Range").Value;
            set => RSettings.GetItem<Counter>("Push Away Range").Value = value;
        }

        private int OnlyPushAwayBelowHPPercent
        {
            get => RSettings.GetItem<Counter>("Only Push Away Below HP Percent").Value;
            set => RSettings.GetItem<Counter>("Only Push Away Below HP Percent").Value = value;
        }

        private PushAwayMode PushAwayModeSelected
        {
            get => (PushAwayMode)Enum.Parse(typeof(PushAwayMode), RSettings.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName);
            set => RSettings.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName = value.ToString();
        }

        internal void LoadTargetPrioValues()
        {
            try
            {
                using var stream = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "Oasys.Core").GetManifestResourceStream("Oasys.Core.Dependencies.TargetSelection.json");
                using var reader = new StreamReader(stream);
                var jsonText = reader.ReadToEnd();

                _targetSelection = JsonConvert.DeserializeObject<TargetSelection>(jsonText);
                var enemies = UnitManager.EnemyChampions.Where(x => !x.IsTargetDummy);

                InitializeSettings(_targetSelection.TargetPrioritizations.Where(x => enemies.Any(e => e.ModelName.Equals(x.Champion, StringComparison.OrdinalIgnoreCase))));
            }
            catch (Exception)
            {
            }
        }

        internal void InitializeSettings(IEnumerable<TargetPrioritization> targetPrioritizations)
        {
            try
            {
                if (targetPrioritizations.Any())
                {
                    ESettings.AddItem(new InfoDisplay() { Title = "-E target prio-" });
                }
                foreach (var targetPrioritization in targetPrioritizations)
                {
                    ESettings.AddItem(new Counter() { Title = targetPrioritization.Champion, MinValue = 0, MaxValue = 5, Value = targetPrioritization.Prioritization, ValueFrequency = 1 });
                }
            }
            catch (Exception)
            {
            }
        }

        private GameObjectBase GetPrioritizationTarget()
        {
            try
            {
                GameObjectBase tempTarget = null;
                var tempPrio = 0;

                foreach (var hero in UnitManager.EnemyChampions
                                                .Where(x => x.Distance <= ETargetRange &&
                                                            TargetSelector.IsAttackable(x) &&
                                                            !x.BuffManager.ActiveBuffs.Any(x => x.Name == "tristanaecharge" && x.Stacks >= 1)))
                {
                    try
                    {
                        var targetPrio = ESettings.GetItem<Counter>(x => x.Title == hero.ModelName)?.Value ?? 1;
                        if (targetPrio > tempPrio)
                        {
                            tempPrio = targetPrio;
                            tempTarget = hero;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                return tempTarget;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool UseTargetselector
        {
            get => ESettings.GetItem<Switch>("Use Targetselector").IsOn;
            set => ESettings.GetItem<Switch>("Use Targetselector").IsOn = value;
        }

        private int ETargetRange
        {
            get => ESettings.GetItem<Counter>("E target range").Value;
            set => ESettings.GetItem<Counter>("E target range").Value = value;
        }


        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Tristana)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use Targetselector", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "E target range", Value = 1000, MinValue = 0, MaxValue = 2000, ValueFrequency = 50 });
            LoadTargetPrioValues();

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            foreach (var enemy in UnitManager.EnemyChampions.Where(x => !x.IsTargetDummy))
            {
                RSettings.AddItem(new Switch() { Title = "R - " + enemy.ModelName, IsOn = true });
            }

            RSettings.AddItem(new Switch() { Title = "Use Push Away", IsOn = false });
            RSettings.AddItem(new Counter() { Title = "Only Push Away Below HP Percent", MinValue = 0, MaxValue = 100, Value = 30, ValueFrequency = 5 });
            RSettings.AddItem(new Counter() { Title = "Push Away Range", MinValue = 50, MaxValue = 500, Value = 150, ValueFrequency = 25 });
            RSettings.AddItem(new ModeDisplay() { Title = "Push Away Mode", ModeNames = PushAwayHelper.ConstructPushAwayModeTable(), SelectedModeName = "Melee" });
        }
    }
}
