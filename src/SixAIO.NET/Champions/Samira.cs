using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
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
    internal sealed class Samira : Champion
    {
        public Samira()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => target.IsObject(ObjectTypeFlag.AIMinionClient)
                                                        ? QAllowLaneclearMinionCollision
                                                        : !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 950,
                Radius = () => 130,
                Speed = () => 2600,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsCharge = () => true,
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance < 325),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsTargetted = () => true,
                Range = () => 600,
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => ShouldE(target),
                TargetSelect = (mode) => SpellE.GetTargets(mode, ShouldE).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsEnabled = () => UseR,
                Range = () => 600,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < 600) > 0,
            };
        }


        private static readonly Vector3 _orderNexusPos = new Vector3(405, 95, 425);
        private static readonly Vector3 _chaosNexusPos = new Vector3(14300, 90, 14400);

        private bool ShouldE(GameObjectBase target)
        {
            if (target == null)
            {
                return false;
            }

            return AllowEInTowerRange ||
                (UnitManager.EnemyTowers.All(x => !x.IsAlive || x.Position.Distance(target.Position) >= 850) &&
                target.Position.Distance(_orderNexusPos) >= 1000 &&
                target.Position.Distance(_chaosNexusPos) >= 1000);
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            SpellW.ExecuteCastSpell();
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
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

        internal bool AllowEInTowerRange
        {
            get => ESettings.GetItem<Switch>("Allow E in tower range").IsOn;
            set => ESettings.GetItem<Switch>("Allow E in tower range").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Samira)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q Allow Laneclear minion collision", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 40, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Allow E in tower range", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.E, SpellSlot.R);

        }
    }
}
