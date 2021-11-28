using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Kindred : Champion
    {
        private static int PassiveStacks()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("kindredmarkofthekindredstackcounter", false, true);
            return buff == null
                ? 0
                : buff.IsActive && buff.Stacks > 0
                    ? (int)buff.Stacks
                    : 0;
        }

        private static bool HasQBuff()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("kindredqasbuff", false, true);
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        private static bool HasWBuff()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("kindredwclonebuffvisible", false, true);
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        public Kindred()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            DashModeSelected == DashMode.ToMouse &&
                            ShouldQ() &&
                            UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && TargetSelector.IsAttackable(x)),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                CastTime = 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            target != null,
                TargetSelect = () => TargetSelector.IsAttackable(Orbwalker.TargetHero) && Orbwalker.TargetHero.Distance <= ERange()
                            ? Orbwalker.TargetHero
                            : UnitManager.EnemyChampions.FirstOrDefault(x => x.IsAlive && x.Distance <= ERange() && TargetSelector.IsAttackable(x) && !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false))
            };
        }

        private bool ShouldQ() => Qonlyifhaswbuff ? HasWBuff() : true;

        private void Spell_OnSpellCast(Spell spell)
        {
            if (spell != null)
            {
                if (spell.SpellSlot == SpellSlot.E)
                {
                    SpellW.ExecuteCastSpell();
                }
                if (spell.SpellSlot == SpellSlot.W)
                {
                    SpellQ.ExecuteCastSpell();
                }
            }
        }

        private static int ERange() => PassiveStacks() switch
        {
            < 4 => 500,
            < 7 => 575,
            < 10 => 600,
            < 13 => 625,
            < 16 => 650,
            < 19 => 675,
            < 22 => 700,
            < 25 => 725,
            >= 25 => 750
        };

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), MenuTab.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        private bool Qonlyifhaswbuff
        {
            get => MenuTab.GetItem<Switch>("Q only if has w buff").IsOn;
            set => MenuTab.GetItem<Switch>("Q only if has w buff").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kindred)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });
            MenuTab.AddItem(new Switch() { Title = "Q only if has w buff", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
