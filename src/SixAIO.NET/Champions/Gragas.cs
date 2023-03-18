using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using Oasys.Common.Menu;
using Oasys.SDK;
using Oasys.Common.GameObject;
using Oasys.Common.Tools.Devices;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Extensions;

namespace SixAIO.Champions
{
    internal sealed class Gragas : Champion
    {
        internal Spell SpellQ2;

        private static GameObjectBase QObject { get; set; }

        public Gragas()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 850,
                Speed = () => 1000,
                Radius = () => 250,
                IsEnabled = () => UseQ && SpellQ.SpellClass.SpellData.MissileName == "GragasQ",
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellQ2 = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ &&
                                  SpellQ.SpellClass.SpellData.MissileName != "GragasQ" &&
                                  IsQObject(QObject),
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.DistanceTo(QObject.Position) <= 300 && x.IsAlive),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 300 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Delay = () => 0f,
                Range = () => 600,
                Radius = () => 160,
                Speed = () => 900,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 1000,
                Speed = () => 1800,
                Radius = () => 400,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.Health < RDamage(x)).FirstOrDefault()
            };
        }

        private float RDamage(GameObjectBase target)
        {
            if (target is not null)
            {
                var baseDamage = 100 + 100 * SpellR.SpellClass.Level;
                var scaleDamage = 0.8f * UnitManager.MyChampion.UnitStats.TotalAbilityPower;
                var damage = baseDamage + scaleDamage;
                return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, 0, damage, 0);
            }

            return 0;
        }

        private bool IsQObject(GameObjectBase obj)
        {
            return obj is not null && obj.IsAlive && obj.Name.Contains("Gragas_") && obj.Name.Contains("_Q_Ally") && obj.Position.IsValid();
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsQObject(obj))
            {
                QObject = obj;
            }
        }

        internal override void OnCoreMainTick()
        {
            if (!IsQObject(QObject))
            {
                QObject = null;
            }
        }

        internal override void OnCoreMainInput()
        {
            SpellQ.ExecuteCastSpell();
            SpellQ2.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Gragas)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
