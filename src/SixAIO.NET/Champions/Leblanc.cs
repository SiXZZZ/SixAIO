using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Leblanc : Champion
    {
        internal Spell SpellRQ;
        internal Spell SpellRW;
        internal Spell SpellRE;

        public Leblanc()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                Range = () => 700,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).OrderByDescending(HasQMark).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 600,
                Speed = () => 1450,
                Radius = () => 240,
                Delay = () => 0f,
                IsEnabled = () => UseW && IsWFirstCast,
                TargetSelect = (mode) => SpellW.GetTargets(mode).OrderByDescending(HasQMark).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 900,
                Radius = () => 110,
                Speed = () => 1750,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).OrderByDescending(HasQMark).FirstOrDefault()
            };


            SpellRQ = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Range = () => 700,
                IsEnabled = () => UseR && AllowRQ && UltSpell == CastSlot.Q,
                TargetSelect = (mode) => SpellRQ.GetTargets(mode).OrderByDescending(HasQMark).FirstOrDefault()
            };
            SpellRW = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 600,
                Speed = () => 1450,
                Radius = () => 240,
                Delay = () => 0f,
                IsEnabled = () => UseR && AllowRW && UltSpell == CastSlot.W && IsRWFirstCast,
                TargetSelect = (mode) => SpellRW.GetTargets(mode).OrderByDescending(HasQMark).FirstOrDefault()
            };
            SpellRE = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 900,
                Radius = () => 110,
                Speed = () => 1750,
                IsEnabled = () => UseR && AllowRE && UltSpell == CastSlot.E,
                TargetSelect = (mode) => SpellRE.GetTargets(mode).OrderByDescending(HasQMark).FirstOrDefault()
            };
        }

        private bool HasQMark(GameObjectBase target) => target.BuffManager.ActiveBuffs.Any(x => x.Stacks >= 1 && (x.Name == "LeblancQMark" || x.Name == "LeblancRQMark"));

        private bool IsWFirstCast => SpellW.SpellClass.SpellData.SpellName != "LeblancWReturn";
        private bool IsRWFirstCast => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).SpellData.SpellName != "LeblancRWReturn";

        private CastSlot UltSpell
        {
            get
            {
                var name = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).SpellData.SpellName;
                var slotName = name.Replace("LeblancR", "");
                var slot = (CastSlot)Enum.Parse(typeof(CastSlot), slotName);
                return slot;
            }
        }
        //spells
        //LeblancRWReturn
        //LeblancWReturn
        //LeblancRW
        //LeblancQ
        //LeblancW
        //LeblancE

        //buffs
        //LeblancQMark
        //LeblancRQMark

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
            }
            if (SpellRQ.ExecuteCastSpell() || SpellRE.ExecuteCastSpell() || SpellRW.ExecuteCastSpell())
            {
            }
        }

        internal bool AllowRQ
        {
            get => RSettings.GetItem<Switch>("Allow RQ").IsOn;
            set => RSettings.GetItem<Switch>("Allow RQ").IsOn = value;
        }

        internal bool AllowRW
        {
            get => RSettings.GetItem<Switch>("Allow RW").IsOn;
            set => RSettings.GetItem<Switch>("Allow RW").IsOn = value;
        }

        internal bool AllowRE
        {
            get => RSettings.GetItem<Switch>("Allow RE").IsOn;
            set => RSettings.GetItem<Switch>("Allow RE").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Leblanc)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Allow RQ", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Allow RW", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Allow RE", IsOn = true });
        }
    }
}
