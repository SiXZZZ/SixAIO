using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Katarina : Champion
    {
        private const string KatarinaQDagger = "Dagger";
        internal Spell PokeSpellQ;
        internal Spell PokeSpellW;

        private static List<GameObjectBase> _daggers = new();

        public Katarina()
        {
            SDKSpell.OnSpellCast += Spell_OnSpellCast;
            PokeSpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsTargetted = () => true,
                Range = () => 625,
                IsEnabled = () => UseQ && !SpellE.SpellClass.IsSpellReady,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            PokeSpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => !SpellE.SpellClass.IsSpellReady && UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan),
            };
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsTargetted = () => true,
                Range = () => 625,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsTargetted = () => true,
                Range = () => 775,
                Delay = () => 0.5f,
                IsEnabled = () => UseE,
                TargetSelect = (mode) =>
                {
                    if (AllowEOnDaggers)
                    {
                        var dagger = _daggers.FirstOrDefault(x => x.Distance <= 775 && x.IsAlive && UnitManager.EnemyChampions.Any(enemy => enemy.DistanceTo(x.Position) <= 500));
                        if (dagger is not null)
                        {
                            return dagger;
                        }
                    }
                    if (AllowEOnChamps)
                    {
                        return SpellE.GetTargets(mode).FirstOrDefault();
                    }

                    return null;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                Range = () => 775,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    if (OnlyRIfCantE)
                    {
                        var daggerAvailable = _daggers.Any(x => x.Distance <= 775 && x.IsAlive && UnitManager.EnemyChampions.Any(enemy => enemy.DistanceTo(x.Position) <= 440));
                        return !daggerAvailable && UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear;
                    }
                    else
                    {
                        return UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear;
                    }
                },
            };
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.CastSlot == CastSlot.E)
            {
                _daggers.Remove(target);
                SpellW.ExecuteCastSpell();
                SpellQ.ExecuteCastSpell();
                SpellR.ExecuteCastSpell();
            }
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (PokeSpellQ.ExecuteCastSpell() || PokeSpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
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

        internal override void OnCreateObject(AIBaseClient x)
        {
            if (x is not null && x.IsAlive && x.Name.Contains(KatarinaQDagger, StringComparison.OrdinalIgnoreCase))
            {
                _daggers.Add(x);
            }
        }

        private static bool IsDagger(GameObjectBase x)
        {
            return x is not null && x.IsAlive && x.Name.Contains(KatarinaQDagger, StringComparison.OrdinalIgnoreCase);
        }

        internal override void OnDeleteObject(AIBaseClient obj)
        {
            _daggers.Remove(obj);
        }

        internal override void OnCoreMainTick()
        {
            foreach (var item in _daggers)
            {
                if (!IsDagger(item))
                {
                    _daggers.Remove(item);
                }
            }
        }

        //internal override void OnCoreRender()
        //{
        //    try
        //    {
        //        if (UnitManager.MyChampion.IsAlive)
        //        {
        //            var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);
        //            var color = Color.Blue;

        //            foreach (var dagger in _daggers)
        //            {
        //                var orbW2S = LeagueNativeRendererManager.WorldToScreenSpell(dagger.Position);
        //                if (!orbW2S.IsZero)
        //                {
        //                    RenderFactory.DrawLine(w2s.X, w2s.Y, orbW2S.X, orbW2S.Y, 2, color);
        //                    RenderFactory.DrawText(dagger.Name, 18, orbW2S, color);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        internal bool AllowEOnDaggers
        {
            get => ESettings.GetItem<Switch>("Allow E on daggers").IsOn;
            set => ESettings.GetItem<Switch>("Allow E on daggers").IsOn = value;
        }

        internal bool AllowEOnChamps
        {
            get => ESettings.GetItem<Switch>("Allow E on champs").IsOn;
            set => ESettings.GetItem<Switch>("Allow E on champs").IsOn = value;
        }

        internal bool OnlyRIfCantE
        {
            get => RSettings.GetItem<Switch>("Only R if cant E").IsOn;
            set => RSettings.GetItem<Switch>("Only R if cant E").IsOn = value;
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Katarina)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Allow E on daggers", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Allow E on champs", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Only R if cant E", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 0, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 550, Value = 550, ValueFrequency = 50 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.E, SpellSlot.R);
        }
    }
}
