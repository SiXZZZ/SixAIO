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
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Nilah : Champion
    {
        public Nilah()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 600,
                Radius = () => 150,
                Speed = () => 2600,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(IsBasicAttackingMe)
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                Range = () => 550,
                IsEnabled = () => UseE && (!EOnlyIfOutOfAARange || Orbwalker.TargetHero is null),
                MinimumCharges = () => 1,
                ShouldCast = (mode, target, spellClass, damage) => ShouldE(target),
                TargetSelect = (mode) => SpellE.GetTargets(mode, ShouldE).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear,
            };
        }

        private bool IsBasicAttackingMe(Hero enemy)
        {
            if (enemy.IsCastingSpell)
            {
                var spell = enemy.GetCurrentCastingSpell();
                if (spell != null && spell.IsBasicAttack && spell.Targets.Any(x => x.NetworkID == UnitManager.MyChampion.NetworkID))
                {
                    return true;
                }
            }

            return false;
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.CastSlot == CastSlot.E && target is not null)
            {
                Orbwalker.AttackReset();
            }
        }

        private bool ShouldE(GameObjectBase target)
        {
            return AllowEInTowerRange || UnitManager.EnemyTowers.All(x => x.Position.Distance(target.Position) >= 850);
        }

        internal override void OnCoreMainInput()
        {
            SpellW.ExecuteCastSpell();

            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
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

        internal bool QAllowLaneclearMinionCollision
        {
            get => QSettings.GetItem<Switch>("Q Allow Laneclear minion collision").IsOn;
            set => QSettings.GetItem<Switch>("Q Allow Laneclear minion collision").IsOn = value;
        }

        internal bool EOnlyIfOutOfAARange
        {
            get => ESettings.GetItem<Switch>("E Only If Out Of AA Range").IsOn;
            set => ESettings.GetItem<Switch>("E Only If Out Of AA Range").IsOn = value;
        }

        internal bool AllowEInTowerRange
        {
            get => ESettings.GetItem<Switch>("Allow E in tower range").IsOn;
            set => ESettings.GetItem<Switch>("Allow E in tower range").IsOn = value;
        }

        private int RIfMoreThanEnemiesNear
        {
            get => RSettings.GetItem<Counter>("R If More Than Enemies Near").Value;
            set => RSettings.GetItem<Counter>("R If More Than Enemies Near").Value = value;
        }

        private int REnemiesCloserThan
        {
            get => RSettings.GetItem<Counter>("R Enemies Closer Than").Value;
            set => RSettings.GetItem<Counter>("R Enemies Closer Than").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Nilah)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Only If Out Of AA Range", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Allow E in tower range", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 600, Value = 450, ValueFrequency = 50 });

        }
    }
}
