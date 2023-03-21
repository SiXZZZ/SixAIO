using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
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

namespace SixAIO.Champions
{
    internal sealed class Nilah : Champion
    {
        public Nilah()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 575,
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
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsTargetted = () => true,
                Range = () => 550,
                IsEnabled = () => UseE && (!EOnlyIfOutOfAARange || Orbwalker.TargetHero is null),
                MinimumCharges = () => 1,
                ShouldCast = (mode, target, spellClass, damage) => target is not null && ShouldE(target),
                TargetSelect = (mode) => EOnlyIfCanKill
                                        ? SpellE.GetTargets(mode, ShouldE).FirstOrDefault(x => x.Health <= EDamage(x))
                                        : SpellE.GetTargets(mode, ShouldE).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                IsEnabled = () => UseR,
                Range = () => REnemiesCloserThan,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear,
            };
        }

        private float EDamage(GameObjectBase target)
        {
            var dmg = 0f;
            var baseDmg = 40f + SpellE.SpellClass.Level * 25f;
            var scaleDmg = UnitManager.MyChampion.UnitStats.TotalAttackDamage * 0.2f;
            dmg += baseDmg;
            dmg += scaleDmg;
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, dmg);
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

        private static readonly Vector3 _orderNexusPos = new Vector3(405, 95, 425);
        private static readonly Vector3 _chaosNexusPos = new Vector3(14300, 90, 14400);

        private bool ShouldE(GameObjectBase target)
        {
            if (EIfCanKill && target.Health <= EDamage(target))
            {
                return true;
            }

            return AllowEInTowerRange ||
                (UnitManager.EnemyTowers.Where(x => x.IsAlive).All(x => x.Position.Distance(target.Position) >= 850) &&
                target.Position.Distance(_orderNexusPos) >= 1000 &&
                target.Position.Distance(_chaosNexusPos) >= 1000);
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
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

        internal bool EOnlyIfCanKill
        {
            get => ESettings.GetItem<Switch>("E Only If Can Kill").IsOn;
            set => ESettings.GetItem<Switch>("E Only If Can Kill").IsOn = value;
        }

        internal bool EIfCanKill
        {
            get => ESettings.GetItem<Switch>("E If Can Kill").IsOn;
            set => ESettings.GetItem<Switch>("E If Can Kill").IsOn = value;
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
            ESettings.AddItem(new Switch() { Title = "E If Can Kill", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Only If Can Kill", IsOn = false });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 600, Value = 450, ValueFrequency = 50 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.E, SpellSlot.R);

        }
    }
}
