using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.Rendering;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;
using ColorConverter = Oasys.SDK.ColorConverter;

namespace SixAIO.Champions
{
    internal sealed class Karthus : Champion
    {
        private static bool IsEActive()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("KarthusDefile", false, true);
            return buff != null && buff.IsActive && buff.Stacks >= 1;
        }

        public Karthus()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 875,
                Speed = () => QSpeed == 0 ? float.Epsilon : QSpeed,
                Radius = () => 160,
                Delay = () => QDelay == 0 ? 0 : (float)((float)((float)QDelay) / 1000f),
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Range = () => 550f,
                Delay = () => 0f,
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    var usingDefile = IsEActive();
                    var targetInRange = UnitManager.EnemyChampions.Any(x => x.Distance < SpellE.Range() && TargetSelector.IsAttackable(x));

                    if (usingDefile && targetInRange)
                    {
                        return false;
                    }
                    else if (!usingDefile && targetInRange)
                    {
                        return true;
                    }
                    else if (usingDefile && !targetInRange)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
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

            var dmg = 50 + (150 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level) + (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.75f);
            return DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) * dmg;
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();

            if (DrawR)
            {
                var enemies = UnitManager.EnemyChampions.Where(x => x.IsAlive && x.IsTargetable &&
                                             !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false) &&
                                             RCanKill(x));
                if (enemies.Any())
                {
                    var killMessage = "Can kill: ";
                    for (int i = 0; i < enemies.Count(); i++)
                    {
                        var enemy = enemies.ElementAtOrDefault(i);
                        if (enemy != null)
                        {
                            if (i == enemies.Count() - 1)
                            {
                                killMessage += $"{enemy.ModelName} ";
                            }
                            else
                            {
                                killMessage += $"{enemy.ModelName}, ";
                            }
                        }
                    }

                    DrawAnnouncement(killMessage, Color.Black, Color.White, RDamageColor);
                }
            }

            if (DrawRDamage &&
                (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).IsSpellReady ||
                 UnitManager.MyChampion.GetCurrentCastingSpell()?.SpellSlot == SpellSlot.R))
            {
                foreach (var enemy in UnitManager.EnemyChampions.Where(x => x.IsAlive && x.W2S.IsValid()))
                {
                    RenderFactory.DrawHPBarDamage(enemy, GetRDamage(enemy), RDamageColor);
                }
            }
        }

        internal override void OnCoreMainInput()
        {
            SpellQ.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        private static Vector2 _middle = new Vector2() { X = LeagueNativeRendererManager.GetWindowsScreenResolution().X / 2 - 200, Y = NativeImport.GetWindowPosition().Y + 50 };
        private static Vector2 _barPos = new Vector2() { X = _middle.X - NativeImport.GetWindowPosition().X, Y = _middle.Y - NativeImport.GetWindowPosition().Y };
        private static Vector2 _endPos = new Vector2 { X = _barPos.X + 400, Y = _barPos.Y };

        private static void DrawAnnouncement(string text, Color background, Color border, Color font)
        {
            var width = Math.Min(LeagueNativeRendererManager.GetGameScreenResolution().X - 100, text.Length * 10);
            _middle = new Vector2() { X = LeagueNativeRendererManager.GetWindowsScreenResolution().X / 2 - width / 2, Y = NativeImport.GetWindowPosition().Y + 50 };
            _barPos = new Vector2() { X = _middle.X - NativeImport.GetWindowPosition().X, Y = _middle.Y - NativeImport.GetWindowPosition().Y };
            _endPos = new Vector2 { X = _barPos.X + width, Y = _barPos.Y };
            RenderFactoryProvider.DrawLine(_barPos.X, _barPos.Y, _endPos.X, _endPos.Y, 40, background);
            RenderFactoryProvider.DrawSpellBox(_barPos, _endPos, 40, border);
            var textPos = _barPos + NativeImport.GetWindowPosition();
            textPos.X += width / 2;
            //textPos.Y -= 20;

            RenderFactoryProvider.DrawText(text, textPos, font);
        }

        private int QSpeed
        {
            get => QSettings.GetItem<Counter>("Q Speed").Value;
            set => QSettings.GetItem<Counter>("Q Speed").Value = value;
        }

        private int QDelay
        {
            get => QSettings.GetItem<Counter>("Q Delay").Value;
            set => QSettings.GetItem<Counter>("Q Delay").Value = value;
        }

        private bool DrawR
        {
            get => RSettings.GetItem<Switch>("Draw R").IsOn;
            set => RSettings.GetItem<Switch>("Draw R").IsOn = value;
        }

        private bool DrawRDamage
        {
            get => RSettings.GetItem<Switch>("Draw R Damage").IsOn;
            set => RSettings.GetItem<Switch>("Draw R Damage").IsOn = value;
        }

        public Color RDamageColor => ColorConverter.GetColor(RSettings.GetItem<ModeDisplay>("R Damage Color").SelectedModeName, 255);

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Karthus)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            QSettings.AddItem(new Counter() { Title = "Q Speed", MinValue = 0, MaxValue = 100_000, Value = 1200, ValueFrequency = 50 });
            QSettings.AddItem(new Counter() { Title = "Q Delay", MinValue = 0, MaxValue = 5_000, Value = 250, ValueFrequency = 50 });

            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Draw R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Draw R Damage", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R Damage Color", ModeNames = ColorConverter.GetColors(), SelectedModeName = "White" });


            MenuTab.AddDrawOptions(SpellSlot.Q);
        }
    }
}
