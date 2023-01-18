using Newtonsoft.Json;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using SixAIO.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Garen : Champion
    {
        private static TargetSelection _targetSelection;
        private float _lastAATime = 0f;
        private float _lastQTime = 0f;

        private bool IsQActive => UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.IsActive && x.Name.Equals("GarenQ", StringComparison.OrdinalIgnoreCase) && x.Stacks >= 1);

        private bool IsEActive => SpellE.SpellClass.SpellData.SpellName.Equals("GarenECancel", StringComparison.OrdinalIgnoreCase);

        public Garen()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsEnabled = () => UseQ && !IsQActive,
                ShouldCast = (mode, target, spellClass, damage) =>
                            _lastAATime > _lastQTime &&
                            !Orbwalker.CanBasicAttack &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsEnabled = () => UseE && !IsEActive && !IsQActive,
                ShouldCast = (mode, target, spellClass, damage) =>
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsTargetted = () => true,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => GetPrioritizationTarget(),
                ShouldCast = (mode, target, spellClass, damage) => target is not null && target.Distance <= 400 && RCanKill(target)
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
            var levelMod = 0.20f + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level * 0.05f;
            var extraDamage = (target.MaxHealth - target.Health) * levelMod;
            var baseDamage = 150 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level;
            return extraDamage + baseDamage;
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            _lastAATime = gameTime;
            if (target != null)
            {
                SpellQ.ExecuteCastSpell();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreMainTick()
        {
            if (UseROnTick)
            {
                SpellR.ExecuteCastSpell();
            }
        }

        private int RTargetRange
        {
            get => RSettings.GetItem<Counter>("R target range").Value;
            set => RSettings.GetItem<Counter>("R target range").Value = value;
        }

        internal bool UseROnTick
        {
            get => RSettings.GetItem<Switch>("Use R On Tick").IsOn;
            set => RSettings.GetItem<Switch>("Use R On Tick").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Garen)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Use R On Tick", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R target range", Value = 1000, MinValue = 0, MaxValue = 2000, ValueFrequency = 50 });
            LoadTargetPrioValues();
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
                    RSettings.AddItem(new InfoDisplay() { Title = "-R target prio-" });
                }
                foreach (var targetPrioritization in targetPrioritizations)
                {
                    RSettings.AddItem(new Counter() { Title = targetPrioritization.Champion, MinValue = 0, MaxValue = 5, Value = targetPrioritization.Prioritization, ValueFrequency = 1 });
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
                foreach (var hero in UnitManager.EnemyChampions.Where(x => x.Distance <= RTargetRange && TargetSelector.IsAttackable(x)))
                {
                    try
                    {
                        var targetPrio = RSettings.GetItem<Counter>(x => x.Title == hero.ModelName)?.Value ?? 1;
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
    }
}
