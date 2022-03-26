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
            SDKSpell.OnSpellCast += Spell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            ShouldQ() &&
                            UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && TargetSelector.IsAttackable(x)),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Delay = () => 0f,
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                            !HasWBuff() &&
                            UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => TargetSelector.IsAttackable(Orbwalker.TargetHero) && Orbwalker.TargetHero.Distance <= ERange()
                            ? Orbwalker.TargetHero
                            : UnitManager.EnemyChampions.FirstOrDefault(x => x.IsAlive && x.Distance <= ERange() && TargetSelector.IsAttackable(x) && !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false))
            };
        }

        private bool ShouldQ() => Qonlyifhaswbuff ? HasWBuff() : true;

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
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
            get => (DashMode)Enum.Parse(typeof(DashMode), QSettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => QSettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        private bool Qonlyifhaswbuff
        {
            get => QSettings.GetItem<Switch>("Q only if has w buff").IsOn;
            set => QSettings.GetItem<Switch>("Q only if has w buff").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kindred)}"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });
            QSettings.AddItem(new Switch() { Title = "Q only if has w buff", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            //RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
