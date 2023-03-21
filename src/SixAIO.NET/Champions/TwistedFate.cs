using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal sealed class TwistedFate : Champion
    {
        internal Spell YellowCard;
        internal Spell RedCard;
        internal Spell BlueCard;
        internal Spell ManualCard;

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
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
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
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
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
            ManualCard = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => !_castRed && !_castBlue && !_castYellow,
            };
            YellowCard = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => GetCard() switch
                {
                    Card.Gold => true,
                    _ => false,
                }
            };
            RedCard = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => GetCard() switch
                {
                    Card.Red => true,
                    _ => false,
                }
            };
            BlueCard = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => GetCard() switch
                {
                    Card.Blue => true,
                    _ => false,
                }
            };
        }

        private bool _castRed;
        private bool _castYellow;
        private bool _castBlue;

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (!SpellW.SpellClass.IsSpellReady)
            {
                return;
            }
            if (keyBeingPressed == YellowCardKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                if (ManualCard.ExecuteCastSpell())
                {
                    _castYellow = true;
                }
            }
            if (keyBeingPressed == RedCardKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                if (ManualCard.ExecuteCastSpell())
                {
                    _castRed = true;
                }
            }
            if (keyBeingPressed == BlueCardKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                if (ManualCard.ExecuteCastSpell())
                {
                    _castBlue = true;
                }
            }
        }

        internal override void OnCoreMainTick()
        {
            if (_castRed && RedCard.ExecuteCastSpell())
            {
                _castRed = false;
            }
            if (_castYellow && YellowCard.ExecuteCastSpell())
            {
                _castYellow = false;
            }
            if (_castBlue && BlueCard.ExecuteCastSpell())
            {
                _castBlue = false;
            }
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        public Keys YellowCardKey => WSettings.GetItem<KeyBinding>("Yellow Card").SelectedKey;
        public Keys BlueCardKey => WSettings.GetItem<KeyBinding>("Blue Card").SelectedKey;
        public Keys RedCardKey => WSettings.GetItem<KeyBinding>("Red Card").SelectedKey;

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(TwistedFate)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new KeyBinding() { Title = "Yellow Card", SelectedKey = Keys.W });
            WSettings.AddItem(new KeyBinding() { Title = "Blue Card", SelectedKey = Keys.E });
            WSettings.AddItem(new KeyBinding() { Title = "Red Card", SelectedKey = Keys.T });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);
        }
    }
}
