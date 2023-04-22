using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Logic;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SharpDX.DXGI;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Xayah : Champion
    {
        private List<AIBaseClient> _feathers = new List<AIBaseClient>();

        private List<AIBaseClient> Feathers => _feathers.Where(IsFeather).ToList();

        public Xayah()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1100,
                Radius = () => 150,
                Speed = () => 4000,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                            Oasys.Common.Logic.TargetSelector.IsAttackable(Oasys.SDK.Orbwalker.TargetHero) &&
                            Oasys.Common.Logic.TargetSelector.IsInRange(Oasys.SDK.Orbwalker.TargetHero),
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
            var champions = UnitManager.EnemyChampions.Where(x => Oasys.Common.Logic.TargetSelector.IsAttackable(x) &&
                                                                  !Oasys.Common.Logic.TargetSelector.IsInvulnerable(x, DamageType.Physical, false));
            return champions.Any(x => GetFeathersBetweenMeAndEnemy(x) >= FeathersToHitChampions) ||
                   champions.Count(x => GetFeathersBetweenMeAndEnemy(x) >= 3) >= ChampionsCanRoot ||
                   champions.Count(x => x.Health <= GetEDamage(x)) >= ChampionsCanKill;
        }

        private int GetFeathersBetweenMeAndEnemy(AIBaseClient enemy)
        {
            return Feathers.Count(feather =>
                    Oasys.SDK.Geometry.DistanceFromPointToLine(enemy.Position.To2D(), new Vector2[] { UnitManager.MyChampion.Position.To2D(), feather.Position.To2D() }) <= enemy.BoundingRadius + 40 &&
                    feather.Distance > enemy.Distance && enemy.DistanceTo(feather.Position) < feather.Distance);
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
            SpellQ.DrawRange();

            if (UnitManager.MyChampion.IsAlive && UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Charges >= 1)
            {
                var color = Oasys.Common.Tools.ColorConverter.GetColor(DrawColor);
                if (DrawFeathers && DrawThickness > 0)
                {
                    var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);

                    foreach (var feather in Feathers)
                    {
                        var featherW2s = LeagueNativeRendererManager.WorldToScreenSpell(feather.Position);
                        if (!featherW2s.IsZero)
                        {
                            Oasys.SDK.Rendering.RenderFactory.DrawLine(w2s.X, w2s.Y, featherW2s.X, featherW2s.Y, DrawThickness, color);
                        }
                    }
                }

                if (DrawFeatherCountCanHit)
                {
                    foreach (var enemy in UnitManager.EnemyChampions.Where(x => x.Distance <= 2000 && x.W2S.IsValid() && x.IsAlive))
                    {
                        var feathersCanHit = GetFeathersBetweenMeAndEnemy(enemy);
                        Oasys.SDK.Rendering.RenderFactory.DrawText(feathersCanHit.ToString(), 72, enemy.W2S, color);
                    }
                }
            }

            //foreach (var obj in UnitManager.AllNativeObjects.Where(x => x.Distance <= 1500 && IsFeather(x)))
            //{
            //    var w2s = LeagueNativeRendererManager.WorldToScreenSpell(obj.Position);
            //    Oasys.SDK.Rendering.RenderFactory.DrawText(obj.Health + " " + obj.Mana + " " + obj.UnitComponentInfo.SkinName + " " + obj.Name + " " + obj.ModelName, 18, w2s, Color.Blue);
            //}
        }

        internal float GetEDamage(AIBaseClient enemy)
        {
            if (enemy is null)
            {
                return 0f;
            }

            var feathers = GetFeathersBetweenMeAndEnemy(enemy);
            if (feathers <= 0)
            {
                return 0f;
            }

            var perFeatherDamage = 40f + SpellE.SpellClass.Level * 10f;
            perFeatherDamage += UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.6f;
            perFeatherDamage *= 1 + (0.75f * UnitManager.MyChampion.UnitStats.Crit);

            var totalDamage = 0f;
            for (int i = 0; i < feathers; i++)
            {
                var featherMod = Math.Max(10f, 100f - (5f * i));
                totalDamage += featherMod / 100f * perFeatherDamage;
            }

            var physicalDamage = Oasys.SDK.DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, enemy, totalDamage);

            if (!enemy.IsObject(ObjectTypeFlag.AIHeroClient))
            {
                physicalDamage *= 0.5f;
            }

            //Logger.Log($"{enemy.ModelName} - dmg: {physicalDamage} - totaldmg: {totalDamage} - per feather:{perFeatherDamage} - feathers:{feathers}");
            return physicalDamage;
        }

        private bool DrawFeathers
        {
            get => MenuTab.GetItem<Switch>("Draw Feathers").IsOn;
            set => MenuTab.GetItem<Switch>("Draw Feathers").IsOn = value;
        }

        private bool DrawFeatherCountCanHit
        {
            get => MenuTab.GetItem<Switch>("Draw Feather Count Hit").IsOn;
            set => MenuTab.GetItem<Switch>("Draw Feather Count Hit").IsOn = value;
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

        private int FeathersToHitChampions
        {
            get => ESettings.GetItem<Counter>("Feathers To Hit Champion").Value;
            set => ESettings.GetItem<Counter>("Feathers To Hit Champion").Value = value;
        }

        private int ChampionsCanRoot
        {
            get => ESettings.GetItem<Counter>("Champions Can Root").Value;
            set => ESettings.GetItem<Counter>("Champions Can Root").Value = value;
        }

        private int ChampionsCanKill
        {
            get => ESettings.GetItem<Counter>("Champions Can Kill").Value;
            set => ESettings.GetItem<Counter>("Champions Can Kill").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Xayah)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            MenuTab.AddItem(new Switch() { Title = "Draw Feathers", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Draw Feather Count Hit", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Draw Thickness", MinValue = 0, MaxValue = 250, Value = 5, ValueFrequency = 1 });
            MenuTab.AddItem(new ModeDisplay() { Title = "Draw Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Blue" });

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "Feathers To Hit Champion", MinValue = 1, MaxValue = 50, Value = 3, ValueFrequency = 1 });
            ESettings.AddItem(new Counter() { Title = "Champions Can Root", MinValue = 1, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            ESettings.AddItem(new Counter() { Title = "Champions Can Kill", MinValue = 1, MaxValue = 5, Value = 2, ValueFrequency = 1 });


            MenuTab.AddDrawOptions(SpellSlot.Q);

        }
    }
}
