using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SixAIO.Utilities
{
    internal class ChatCooldownAlerter
    {
        private static int _index;
        private static Tab _menuTab => MenuManagerProvider.GetTab(_index);

        private static bool Use
        {
            get => _menuTab.GetItem<Switch>("Use").IsOn;
            set => _menuTab.GetItem<Switch>("Use").IsOn = value;
        }

        private static int Interval
        {
            get => _menuTab.GetItem<Counter>("Interval").Value;
            set => _menuTab.GetItem<Counter>("Interval").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            _index = MenuManager.AddTab(new Tab($"SIXAIO - Chat CD Alerter"));
            _menuTab.AddItem(new Switch() { Title = "Use", IsOn = false });
            _menuTab.AddItem(new Counter() { Title = "Interval", MinValue = 10, MaxValue = 300, Value = 60, ValueFrequency = 10 });

            _menuTab.AddItem(new InfoDisplay() { Title = "-Only alert enabled summoners-" });
            foreach (var enemy in UnitManager.EnemyChampions)
            {
                var spellBook = enemy.GetSpellBook();
                var summoner1 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner1);
                var summoner2 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner2);

                _menuTab.AddItem(new InfoDisplay() { Title = $"-{enemy.ModelName}-" });
                _menuTab.AddItem(new Switch() { Title = summoner1.SpellData.SpellName, IsOn = false });
                _menuTab.AddItem(new Switch() { Title = summoner2.SpellData.SpellName, IsOn = false });
            }
            return Task.CompletedTask;
        }

        private static float _lastAlertTime = 0f;
        internal static Task OnCoreMainTick()
        {
            if (_lastAlertTime + (float)Interval < GameEngine.GameTime)
            {
                var message = "";
                foreach (var enemy in UnitManager.EnemyChampions)
                {
                    var spellBook = enemy.GetSpellBook();
                    var summoner1 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner1);
                    var summoner2 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner2);

                    if (ShouldSend(summoner1))
                    {
                        message += GetMessage(enemy, summoner1);
                    }
                    if (ShouldSend(summoner2))
                    {
                        message += GetMessage(enemy, summoner2);
                    }
                }

                Send(message);

                _lastAlertTime = GameEngine.GameTime;
            }

            return Task.CompletedTask;
        }

        private static string GetMessage(Hero hero, Oasys.Common.GameObject.Clients.ExtendedInstances.Spells.SpellClass spellClass)
        {
            var expire = new TimeSpan(0, 0, (int)spellClass.CooldownExpire).ToString("mmss");

            return $"{hero.ModelName.ToLower()} {spellClass.SpellData.SpellName} {expire}";
        }

        private static bool ShouldSend(Oasys.Common.GameObject.Clients.ExtendedInstances.Spells.SpellClass spellClass)
        {
            return !spellClass.IsSpellReady && _menuTab.GetItem<Switch>(spellClass.SpellData.SpellName).IsOn;
        }

        private static void Send(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Oasys.SDK.InputProviders.KeyboardProvider.PressKey(Oasys.SDK.InputProviders.KeyboardProvider.KeyBoardScanCodes.KEY_ENTER);
                SendKeys.Send(message);
                Oasys.SDK.InputProviders.KeyboardProvider.PressKey(Oasys.SDK.InputProviders.KeyboardProvider.KeyBoardScanCodes.KEY_ENTER);
            }
        }
    }
}
