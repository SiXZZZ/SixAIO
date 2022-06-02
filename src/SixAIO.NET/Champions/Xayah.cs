using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Xayah : Champion
    {
        private List<AIBaseClient> _feathers = new List<AIBaseClient>();

        private List<AIBaseClient> Feathers => _feathers.Where(IsFeather).ToList();

        public Xayah()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1100,
                Radius = () => 150,
                Speed = () => 4000,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            (25 + spellClass.Level * 25 +
                            (UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.5f))
                            : 0,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                MinimumCharges = () => 1,
                ShouldCast = (mode, target, spellClass, damage) => ShouldCastE(),
            };
        }

        private bool ShouldCastE()
        {
            return UnitManager.EnemyChampions.Where(x => TargetSelector.IsAttackable(x) &&
                                                         !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false))
                                             .Any(x => GetFeathersBetweenMeAndEnemy(x) >= FeathersToHitChampions);
        }

        private int GetFeathersBetweenMeAndEnemy(AIBaseClient enemy)
        {
            return Feathers.Count(feather =>
                    Geometry.DistanceFromPointToLine(enemy.W2S, new Vector2[] { UnitManager.MyChampion.W2S, feather.W2S }) <= enemy.UnitComponentInfo.UnitBoundingRadius &&
                    feather.Distance > enemy.Distance);
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsFeather(obj))
            {
                _feathers.Add(obj);
            }
        }

        private static bool IsFeather(AIBaseClient obj)
        {
            return obj is not null && obj.IsAlive && obj.Health == 100 && obj.Mana == 500 && obj.OnMyTeam && obj.Position.IsValid() &&
                (obj.Name.Contains("Feather", StringComparison.OrdinalIgnoreCase));
        }

        internal override void OnDeleteObject(AIBaseClient obj)
        {
            if (!IsFeather(obj))
            {
                _feathers.Remove(obj);
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreMainTick()
        {
            foreach (var feather in _feathers.ToList().Where(x => !IsFeather(x)))
            {
                _feathers.Remove(feather);
            }
        }

        internal override void OnCoreRender()
        {
            if (DrawFeathers && DrawThickness > 0 && UnitManager.MyChampion.IsAlive && UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Charges >= 1)
            {
                var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);
                var color = Oasys.Common.Tools.ColorConverter.GetColor(DrawColor);

                foreach (var feather in Feathers)
                {
                    var featherW2s = LeagueNativeRendererManager.WorldToScreenSpell(feather.Position);
                    if (!featherW2s.IsZero)
                    {
                        Oasys.SDK.Rendering.RenderFactory.DrawLine(w2s.X, w2s.Y, featherW2s.X, featherW2s.Y, DrawThickness, color);
                    }
                }
            }

            //foreach (var obj in UnitManager.AllNativeObjects.Where(x => x.Distance <= 1500 && IsFeather(x)))
            //{
            //    var w2s = LeagueNativeRendererManager.WorldToScreenSpell(obj.Position);
            //    Oasys.SDK.Rendering.RenderFactory.DrawText(obj.Health + " " + obj.Mana + " " + obj.UnitComponentInfo.SkinName + " " + obj.Name + " " + obj.ModelName, 12, w2s, Color.Blue);
            //}
        }

        internal float GetEDamage(AIBaseClient enemy)
        {
            var feathers = GetFeathersBetweenMeAndEnemy(enemy);
            var armorMod = DamageCalculator.GetArmorMod(UnitManager.MyChampion, enemy);
            var physicalDamage = armorMod * ((45 + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 10 + UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.60f) * feathers);
            if (!enemy.IsObject(ObjectTypeFlag.AIHeroClient))
            {
                physicalDamage *= 0.5f;
            }
            return (float)physicalDamage;
        }

        private enum FeatherMode
        {
            //Kill,
            //Stun,
            //Damage,
            Stacks
        }

        private List<string> ConstructFeatherModeTable()
        {
            var keyTable = Enum.GetNames(typeof(FeatherMode)).ToList();
            return keyTable;
        }

        private bool DrawFeathers
        {
            get => MenuTab.GetItem<Switch>("Draw Feathers").IsOn;
            set => MenuTab.GetItem<Switch>("Draw Feathers").IsOn = value;
        }

        private int DrawThickness
        {
            get => MenuTab.GetItem<Counter>("Draw Thickness").Value;
            set => MenuTab.GetItem<Counter>("Draw Thickness").Value = value;
        }

        private string DrawColor
        {
            get => MenuTab.GetItem<ModeDisplay>("Draw Color").SelectedModeName;
            set => MenuTab.GetItem<ModeDisplay>("Draw Color").SelectedModeName = value;
        }

        private FeatherMode ChampionMode
        {
            get => (FeatherMode)Enum.Parse(typeof(FeatherMode), ESettings.GetItem<ModeDisplay>("Champion Mode").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("Champion Mode").SelectedModeName = value.ToString();
        }

        private int FeathersToHitChampions
        {
            get => ESettings.GetItem<Counter>("Feathers To Hit Champions").Value;
            set => ESettings.GetItem<Counter>("Feathers To Hit Champions").Value = value;
        }

        //private FeatherMode EpicMonsterMode
        //{
        //    get => (FeatherMode)Enum.Parse(typeof(FeatherMode), MenuTab.GetItem<ModeDisplay>("Epic Monster Mode").SelectedModeName);
        //    set => MenuTab.GetItem<ModeDisplay>("Champions Mode").SelectedModeName = value.ToString();
        //}

        //private int FeathersToHitEpicMonster
        //{
        //    get => MenuTab.GetItem<Counter>("Feathers To Hit Epic Monster").Value;
        //    set => MenuTab.GetItem<Counter>("Feathers To Hit Epic Monster").Value = value;
        //}

        //private FeatherMode TargetsMode
        //{
        //    get => (FeatherMode)Enum.Parse(typeof(FeatherMode), MenuTab.GetItem<ModeDisplay>("Targets Mode").SelectedModeName);
        //    set => MenuTab.GetItem<ModeDisplay>("Targets Mode").SelectedModeName = value.ToString();
        //}

        //private int FeathersToHitTargets
        //{
        //    get => MenuTab.GetItem<Counter>("Feathers To Hit Targets").Value;
        //    set => MenuTab.GetItem<Counter>("Feathers To Hit Targets").Value = value;
        //}

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Xayah)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            MenuTab.AddItem(new Switch() { Title = "Draw Feathers", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Draw Thickness", MinValue = 0, MaxValue = 250, Value = 5, ValueFrequency = 1 });
            MenuTab.AddItem(new ModeDisplay() { Title = "Draw Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Blue" });

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "Champion Mode", ModeNames = ConstructFeatherModeTable(), SelectedModeName = "Stacks" });
            ESettings.AddItem(new Counter() { Title = "Feathers To Hit Champions", MinValue = 1, MaxValue = 25, Value = 3, ValueFrequency = 1 });
            //MenuTab.AddItem(new ModeDisplay() { Title = "Epic Monster Mode", ModeNames = ConstructFeatherModeTable(), SelectedModeName = "Stacks" });
            //MenuTab.AddItem(new Counter() { Title = "Feathers To Hit Epic Monster", MinValue = 1, MaxValue = 25, Value = 10, ValueFrequency = 1 });
            //MenuTab.AddItem(new ModeDisplay() { Title = "Targets Mode", ModeNames = ConstructFeatherModeTable(), SelectedModeName = "Stacks" });
            //MenuTab.AddItem(new Counter() { Title = "Feathers To Hit Targets", MinValue = 1, MaxValue = 25, Value = 10, ValueFrequency = 1 });

        }
    }
}
