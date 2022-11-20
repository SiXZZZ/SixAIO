using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.Rendering;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Twitch : Champion
    {
        public Twitch()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                            SpellQ.SpellClass.CooldownExpire < _lastQUse &&
                            !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "TwitchHideInShadows" && x.Stacks >= 1) &&
                            UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < QEnemiesCloserThan) > QIfMoreThanEnemiesNear,
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 950,
                Speed = () => 1400,
                Radius = () => 260,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => ShouldCastE(mode),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear,
            };
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            SpellW.ExecuteCastSpell();
        }

        internal bool ShouldCastE(Orbwalker.OrbWalkingModeType mode)
        {
            return mode switch
            {
                Orbwalker.OrbWalkingModeType.Combo => ShouldEOnCombo(),
                Orbwalker.OrbWalkingModeType.LaneClear =>
                            ShouldEOnCombo() ||
                            (UnitManager.EnemyJungleMobs.Count(x => IsValidTarget(x) && IsEpicJungleMonster(x) && CanKill(x)) >= 1) ||
                            (UnitManager.EnemyJungleMobs.Count(x => IsValidTarget(x) && CanKill(x)) >= 3) ||
                            (UnitManager.EnemyMinions.Count(x => IsValidTarget(x) && CanKill(x)) >= 6),
                _ => false
            };
        }

        private bool ShouldEOnCombo()
        {
            return EWhenCanKill
                ? UnitManager.EnemyChampions.Count(x => IsValidTarget(x) && CanKill(x)) >= 1
                : UnitManager.EnemyChampions.Count(x => IsValidTarget(x) && TwitchEStacks(x) >= MinimumStacks) >= TargetsWithStacks;
        }

        private static bool IsEpicJungleMonster(JungleMob target)
        {
            return target.UnitComponentInfo.SkinName.ToLower().Contains("dragon") ||
                   target.UnitComponentInfo.SkinName.ToLower().Contains("baron") ||
                   target.UnitComponentInfo.SkinName.ToLower().Contains("herald");
        }

        private static bool IsValidTarget(GameObjectBase target)
        {
            return target.Distance <= 1200 && target.IsAlive && TargetSelector.IsAttackable(target);
        }

        private static bool CanKill(GameObjectBase target)
        {
            var dmg = GetEDamage(target);
            return target.Health <= dmg ||
                   (UnitManager.MyChampion.Inventory.HasItem(ItemID.The_Collector) && (target.Health - dmg) / target.MaxHealth * 100f < 5f);
        }

        private float _lastQUse;
        internal override void OnCoreMainTick()
        {
            if (!SpellQ.SpellClass.IsSpellReady)
            {
                _lastQUse = SpellQ.SpellClass.CooldownExpire;
            }
        }

        internal override void OnCoreRender()
        {
            if (DrawQTime)
            {
                var qBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name == "TwitchHideInShadows" && x.Stacks >= 1);
                if (qBuff != null)
                {
                    var qTimeRemaining = qBuff.RemainingDurationMs / 1000;
                    var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);
                    w2s.Y -= 20;
                    RenderFactory.DrawText($"Q:{qTimeRemaining:0.##}", 18, w2s, Color.Blue);
                }
            }
            if (DrawRTime)
            {
                var rBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name == "TwitchFullAutomatic" && x.Stacks >= 1);
                if (rBuff != null)
                {
                    var rTimeRemaining = rBuff.RemainingDurationMs / 1000;
                    var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);
                    RenderFactory.DrawText($"R:{rTimeRemaining:0.##}", 18, w2s, Color.Blue);
                }
            }
            if (SpellE.SpellClass.IsSpellReady)
            {
                if (DrawEDamageChampions)
                {
                    foreach (var enemy in UnitManager.EnemyChampions.ToList().Where(x => x.IsAlive && x.Distance <= 2000 && x.W2S.IsValid()).ToList())
                    {
                        RenderFactory.DrawHPBarDamage(enemy, GetEDamage(enemy), EDamageColor);
                    }
                }
                if (DrawEDamageMinions)
                {
                    foreach (var enemy in UnitManager.EnemyMinions.ToList().Where(x => x.IsAlive && x.Distance <= 2000 && x.W2S.IsValid()).ToList())
                    {
                        RenderFactory.DrawHPBarDamage(enemy, GetEDamage(enemy), EDamageColor);
                    }
                }
                if (DrawEDamageMonsters)
                {
                    foreach (var enemy in UnitManager.EnemyJungleMobs.ToList().Where(x => x.IsAlive && x.Distance <= 2000 && x.W2S.IsValid()).ToList())
                    {
                        RenderFactory.DrawHPBarDamage(enemy, GetEDamage(enemy), EDamageColor);
                    }
                }
            }
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseELaneclear && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        private static float TwitchEStacks(GameObjectBase enemy)
        {
            return enemy.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name == "TwitchDeadlyVenom")?.Stacks ?? 0;
        }

        internal static float GetEDamage(GameObjectBase enemy)
        {
            var stacks = TwitchEStacks(enemy);
            if (stacks == 0)
            {
                return 0;
            }
            var eLevel = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level;
            var armorMod = DamageCalculator.GetArmorMod(UnitManager.MyChampion, enemy);
            var magicResistMod = DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, enemy);
            var physicalBaseDamage = 10 + eLevel * 10;
            var physicalDamage = armorMod * (physicalBaseDamage +
                                            ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.35f + (10 + eLevel * 5)) * stacks));
            var magicDamage = magicResistMod * ((0.3f * UnitManager.MyChampion.UnitStats.TotalAbilityPower) * stacks);
            //Logger.Log($"Physical: {physicalDamage} - Magic: {magicDamage}");
            return (float)(((physicalDamage - enemy.PhysicalShield) + (magicDamage - enemy.MagicalShield)) - enemy.NeutralShield);
        }

        private bool DrawQTime
        {
            get => QSettings.GetItem<Switch>("Draw Q Time").IsOn;
            set => QSettings.GetItem<Switch>("Draw Q Time").IsOn = value;
        }

        private bool DrawRTime
        {
            get => RSettings.GetItem<Switch>("Draw R Time").IsOn;
            set => RSettings.GetItem<Switch>("Draw R Time").IsOn = value;
        }

        private int QIfMoreThanEnemiesNear
        {
            get => QSettings.GetItem<Counter>("Q If More Than Enemies Near").Value;
            set => QSettings.GetItem<Counter>("Q If More Than Enemies Near").Value = value;
        }

        private int QEnemiesCloserThan
        {
            get => QSettings.GetItem<Counter>("Q Enemies Closer Than").Value;
            set => QSettings.GetItem<Counter>("Q Enemies Closer Than").Value = value;
        }

        private bool EWhenCanKill
        {
            get => ESettings.GetItem<Switch>("E when can kill").IsOn;
            set => ESettings.GetItem<Switch>("E when can kill").IsOn = value;
        }

        private bool DrawEDamageChampions
        {
            get => ESettings.GetItem<Switch>("Draw E Damage Champions").IsOn;
            set => ESettings.GetItem<Switch>("Draw E Damage Champions").IsOn = value;
        }

        private bool DrawEDamageMinions
        {
            get => ESettings.GetItem<Switch>("Draw E Damage Minions").IsOn;
            set => ESettings.GetItem<Switch>("Draw E Damage Minions").IsOn = value;
        }

        private bool DrawEDamageMonsters
        {
            get => ESettings.GetItem<Switch>("Draw E Damage Monsters").IsOn;
            set => ESettings.GetItem<Switch>("Draw E Damage Monsters").IsOn = value;
        }

        public Color EDamageColor => ColorConverter.GetColor(ESettings.GetItem<ModeDisplay>("E Damage Color").SelectedModeName, 255);


        private int TargetsWithStacks
        {
            get => ESettings.GetItem<Counter>("Targets with stacks").Value;
            set => ESettings.GetItem<Counter>("Targets with stacks").Value = value;
        }

        private int MinimumStacks
        {
            get => ESettings.GetItem<Counter>("Minimum stacks").Value;
            set => ESettings.GetItem<Counter>("Minimum stacks").Value = value;
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Twitch)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Draw Q Time", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 0, ValueFrequency = 1 });
            QSettings.AddItem(new Counter() { Title = "Q Enemies Closer Than", MinValue = 50, MaxValue = 1500, Value = 800, ValueFrequency = 25 });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "Targets with stacks", MinValue = 1, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            ESettings.AddItem(new Counter() { Title = "Minimum stacks", MinValue = 1, MaxValue = 6, Value = 6, ValueFrequency = 1 });
            ESettings.AddItem(new Switch() { Title = "E when can kill", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Draw E Damage Champions", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Draw E Damage Minions", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Draw E Damage Monsters", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E Damage Color", ModeNames = ColorConverter.GetColors(), SelectedModeName = "White" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Draw R Time", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 1500, Value = 1100, ValueFrequency = 50 });
        }
    }
}
