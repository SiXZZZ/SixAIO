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
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Extensions;
using SixAIO.Extensions;

namespace SixAIO.Champions
{
    internal sealed class Anivia : Champion
    {
        internal Spell SpellQ2;
        internal Spell SpellR2;

        private static GameObjectBase RObject { get; set; }

        private static GameObjectBase QObject { get; set; }

        public Anivia()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 1100,
                Speed = () => 950,
                Radius = () => 220,
                IsEnabled = () => UseQ && !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "FlashFrost" && x.Stacks >= 1),
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellQ2 = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ && IsQObject(QObject) && UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "FlashFrost" && x.Stacks >= 1),
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.DistanceTo(QObject.Position) <= 220 && x.IsAlive),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsTargetted = () => true,
                Range = () => 600f,
                IsEnabled = () => UseE,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LastHit)
                    {
                        return SpellE.GetTargets(mode, x => x.Health <= EDamage(x)).FirstOrDefault();
                    }
                    if (mode == Orbwalker.OrbWalkingModeType.Mixed ||
                        mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        var lasthit = SpellE.GetTargets(mode, x => x.Health <= EDamage(x)).FirstOrDefault();
                        if (lasthit != null)
                        {
                            return lasthit;
                        }
                    }
                    return SpellE.GetTargets(mode).FirstOrDefault();
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 750,
                Speed = () => 1800,
                Radius = () => 400,
                IsEnabled = () => UseR && !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "GlacialStorm" && x.Stacks >= 1),
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
            SpellR2 = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR && IsRObject(RObject) && UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "GlacialStorm" && x.Stacks >= 1),
                ShouldCast = (mode, target, spellClass, damage) => !UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.DistanceTo(RObject.Position) <= 400 && x.IsAlive),
            };
        }

        private float EDamage(GameObjectBase target)
        {
            var baseDmg = 25f + SpellE.SpellClass.Level * 25f;
            var scaleDmg = UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.6f;
            var dmg = baseDmg + scaleDmg;
            if (target.BuffManager.ActiveBuffs.Any(x => x.Name == "aniviaiced" && x.Stacks >= 1))
            {
                dmg *= 2;
            }
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, 0, dmg, 0);
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            SpellQ.ExecuteCastSpell();
            SpellQ2.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
            SpellR2.ExecuteCastSpell();
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsQObject(obj))
            {
                QObject = obj;
            }
            if (IsRObject(obj))
            {
                RObject = obj;
            }
        }

        internal override void OnCoreMainTick()
        {
            if (!IsQObject(QObject))
            {
                QObject = null;
            }
            if (!IsRObject(RObject))
            {
                RObject = null;
            }
        }

        private bool IsRObject(GameObjectBase obj)
        {
            return obj is not null && obj.Name.Contains("Anivia_") && obj.Name.Contains("_R_AOE_");
        }

        private bool IsQObject(GameObjectBase obj)
        {
            return obj is not null && obj.IsAlive && obj.Name == "FlashFrostSpell" && obj.Position.IsValid();
        }

        //internal override void OnCoreRender()
        //{
        //    if (IsQObject(QObject))
        //    {
        //        var w2s = LeagueNativeRendererManager.WorldToScreenSpell(QObject.Position);
        //        Oasys.SDK.Rendering.RenderFactory.DrawText(QObject.Name, 100, w2s, Color.Blue);
        //    }
        //    if (IsRObject(RObject))
        //    {
        //        var w2s = LeagueNativeRendererManager.WorldToScreenSpell(RObject.Position);
        //        Oasys.SDK.Rendering.RenderFactory.DrawText(RObject.Name, 100, w2s, Color.Blue);
        //    }
        //}

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Anivia)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.E, SpellSlot.R);

        }
    }
}
