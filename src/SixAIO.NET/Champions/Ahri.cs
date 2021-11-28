﻿using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Ahri : Champion
    {
        public Ahri()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Range = 900,
                Width = 200,
                Speed = 1550,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 85 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 1000 && x.IsAlive &&
                                                 TargetSelector.IsAttackable(x) && x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed) && 
                                                 !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 700 && x.IsAlive && TargetSelector.IsAttackable(x)) != null,
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Range = 1000,
                Speed = 1550,
                Width = 120,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 70 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 850 && x.IsAlive &&
                                                 TargetSelector.IsAttackable(x) && !Collision.MinionCollision(x.W2S, 140) &&
                                                 !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                CastTime = 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            DashModeSelected == DashMode.ToMouse &&
                            UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= x.TrueAttackRange + 500 && x.IsAlive && TargetSelector.IsAttackable(x)) != null,
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), MenuTab.GetItem<ModeDisplay>("R Dash Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("R Dash Mode").SelectedModeName = value.ToString();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Ahri)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "R Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });
        }
    }
}