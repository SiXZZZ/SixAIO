using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Alistar : Champion
    {
        public Alistar()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 350 && TargetSelector.IsAttackable(x))
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                Range = () => 650,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 350 && TargetSelector.IsAttackable(x))
            };
        }

        internal override void OnCoreRender()
        {
            if (UnitManager.MyChampion.IsAlive)
            {
                bool drawQ = DrawSettings.GetItem<Switch>("Draw Q Range").IsOn;
                var qColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw Q Color").SelectedModeName);
                float qRange = 375;

                if (drawQ)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, qRange, qColor, 3);
                }

                bool drawW = DrawSettings.GetItem<Switch>("Draw W Range").IsOn;
                var wColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw W Color").SelectedModeName);
                float wRange = 650;

                if (drawW)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, wRange, wColor, 3);
                }

                bool drawE = DrawSettings.GetItem<Switch>("Draw E Range").IsOn;
                var eColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw E Color").SelectedModeName);
                float eRange = 350;

                if (drawE)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, eRange, eColor, 3);
                }
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Alistar)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            MenuTab.AddGroup(new Group("Draw Settings"));
            DrawSettings.AddItem(new Switch() { Title = "Draw Q Range", IsOn = true });
            DrawSettings.AddItem(new ModeDisplay() { Title = "Draw Q Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Blue" });
            DrawSettings.AddItem(new Switch() { Title = "Draw W Range", IsOn = true });
            DrawSettings.AddItem(new ModeDisplay() { Title = "Draw W Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Orange" });
            DrawSettings.AddItem(new Switch() { Title = "Draw E Range", IsOn = true });
            DrawSettings.AddItem(new ModeDisplay() { Title = "Draw E Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Green" });
        }
    }
}
