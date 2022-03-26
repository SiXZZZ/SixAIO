using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class TwistedFate : Champion
    {
        private enum Card
        {
            None,
            Blue,
            Red,
            Gold
        }

        private static Card GetCard() => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W).SpellData.SpellName switch
        {
            "GoldCardLock" => Card.Gold,
            "RedCardLock" => Card.Red,
            "BlueCardLock" => Card.Blue,
            _ => Card.None
        };

        public TwistedFate()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1450,
                Radius = () => 100,
                Speed = () => 1000,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => GetCard() switch
                {
                    Card.None => UnitManager.MyChampion.Mana > 100 && TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero),
                    Card.Blue => UnitManager.MyChampion.Mana <= 100,
                    Card.Red => false,
                    Card.Gold => TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero),
                    _ => false,
                }
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(TwistedFate)}"));
            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
        }
    }
}
