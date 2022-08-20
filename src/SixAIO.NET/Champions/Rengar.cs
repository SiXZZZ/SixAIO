using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
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
    internal class Rengar : Champion
    {
        private bool IsUltActive => UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "RengarR" && x.Stacks >= 1);
        private bool IsEmpowered => UnitManager.MyChampion.Mana == 4;

        public Rengar()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ && !IsUltActive && (!IsEmpowered || CanUseEmpoweredQ),
                ShouldCast = (mode, target, spellClass, damage) => TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW && !IsUltActive && (!IsEmpowered || CanUseEmpoweredW),
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.MyChampion.HealthPercent <= WIfHealthPercentBelow || UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance <= 450 && x.IsAlive),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => EMaximumRange,
                Radius = () => 140,
                Speed = () => 1500,
                IsEnabled = () => UseE && !IsUltActive && (!IsEmpowered || CanUseEmpoweredE) && (!OnlyEOutOfAARange || UnitManager.EnemyChampions.All(x => x.Distance >= 200)),
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            SpellQ.ExecuteCastSpell();
        }

        internal override void OnCoreMainInput()
        {
            if (UnitManager.MyChampion.AttackRange >= 350)
            {
                Orbwalker.SelectedTarget = UnitManager.EnemyChampions
                    .Where(x => x.Position.Distance(GameEngine.WorldMousePosition) <= 400)
                    .OrderBy(x => x.Position.Distance(GameEngine.WorldMousePosition))
                    .FirstOrDefault();
            }

            if (SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal bool CanUseEmpoweredQ
        {
            get => QSettings.GetItem<Switch>("Can Use Empowered Q").IsOn;
            set => QSettings.GetItem<Switch>("Can Use Empowered Q").IsOn = value;
        }

        internal bool CanUseEmpoweredW
        {
            get => WSettings.GetItem<Switch>("Can Use Empowered W").IsOn;
            set => WSettings.GetItem<Switch>("Can Use Empowered W").IsOn = value;
        }

        private int WIfHealthPercentBelow
        {
            get => WSettings.GetItem<Counter>("W If Health Percent Below").Value;
            set => WSettings.GetItem<Counter>("W If Health Percent Below").Value = value;
        }

        internal bool CanUseEmpoweredE
        {
            get => ESettings.GetItem<Switch>("Can Use Empowered E").IsOn;
            set => ESettings.GetItem<Switch>("Can Use Empowered E").IsOn = value;
        }

        internal bool OnlyEOutOfAARange
        {
            get => ESettings.GetItem<Switch>("Only E Out Of AA Range").IsOn;
            set => ESettings.GetItem<Switch>("Only E Out Of AA Range").IsOn = value;
        }

        private int EMaximumRange
        {
            get => ESettings.GetItem<Counter>("E Maximum Range").Value;
            set => ESettings.GetItem<Counter>("E Maximum Range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Rengar)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Can Use Empowered Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Can Use Empowered W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W If Health Percent Below", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Can Use Empowered E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Only E Out Of AA Range", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            ESettings.AddItem(new Counter() { Title = "E Maximum Range", MinValue = 0, MaxValue = 1000, Value = 950, ValueFrequency = 50 });

        }
    }
}
