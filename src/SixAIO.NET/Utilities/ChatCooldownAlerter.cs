using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools.Devices;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SixAIO.Utilities
{
    internal class ChatCooldownAlerter
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        private static Tab OrbInputTab => MenuManagerProvider.GetTab("Orbwalker Input");

        public static Keys GetOverrideKey()
        {
            return OrbInputTab.GetItem<KeyBinding>("Target Override Key").SelectedKey;
        }

        public static Keys GetComboKey()
        {
            return OrbInputTab.GetItem<KeyBinding>("Combo Key").SelectedKey;
        }

        public static Keys GetHarassKey()
        {
            return OrbInputTab.GetItem<KeyBinding>("Harass Key").SelectedKey;
        }

        public static Keys GetLaneclearKey()
        {
            return OrbInputTab.GetItem<KeyBinding>("Laneclear Key").SelectedKey;
        }

        public static Keys GetLasthitKey()
        {
            return OrbInputTab.GetItem<KeyBinding>("Lasthit Key").SelectedKey;
        }

        public static Keyboard.KeyBoardScanCodes GetTargetChampsOnlyKey()
        {
            return Keyboard.GetKeyBoardScanCode(OrbInputTab.GetItem<KeyBinding>("Target Champs Only Key").SelectedKey);
        }

        private static int _index;
        private static Tab _menuTab => MenuManagerProvider.GetTab(_index);

        private static bool Use
        {
            get => _menuTab.GetItem<Switch>("Use").IsOn;
            set => _menuTab.GetItem<Switch>("Use").IsOn = value;
        }

        public static Keys GetKeybinding()
        {
            return _menuTab.GetItem<KeyBinding>("Key binding").SelectedKey;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            _index = MenuManager.AddTab(new Tab($"SIXAIO - Chat CD Alerter"));
            _menuTab.AddItem(new Switch() { Title = "Use", IsOn = false });
            _menuTab.AddItem(new KeyBinding() { Title = "Key binding", SelectedKey = Keys.M });

            _menuTab.AddItem(new InfoDisplay() { Title = "-Only alert enabled summoners-" });
            foreach (var enemy in UnitManager.EnemyChampions.Where(x => !x.UnitComponentInfo.SkinName.Contains("TargetDummy", StringComparison.OrdinalIgnoreCase)))
            {
                var spellBook = enemy.GetSpellBook();
                var summoner1 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner1);
                var summoner2 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner2);

                var sum1 = enemy.ModelName + " " + GetSummonerText(summoner1.SpellData.SpellName);
                var sum2 = enemy.ModelName + " " + GetSummonerText(summoner2.SpellData.SpellName);
                _menuTab.AddItem(new InfoDisplay() { Title = $"-{enemy.ModelName}-" });
                if (!string.IsNullOrEmpty(sum1))
                {
                    _menuTab.AddItem(new Switch() { Title = sum1, IsOn = !string.IsNullOrEmpty(sum1) });
                }
                if (!string.IsNullOrEmpty(sum2))
                {
                    _menuTab.AddItem(new Switch() { Title = sum2, IsOn = !string.IsNullOrEmpty(sum2) });
                }
            }
            return Task.CompletedTask;
        }

        private static float _lastMessage = 0f;
        internal static Task OnCoreMainTick()
        {
            if (Use &&
                !AnyKeysPressed() &&
                !GameEngine.ChatBox.IsChatBoxOpen &&
                GetForegroundWindow() == System.Diagnostics.Process.GetProcessesByName("League of Legends").FirstOrDefault().MainWindowHandle &&
                Keyboard.IsKeyPressed(GetKeybinding()) &&
                _lastMessage + 10 < GameEngine.GameTime)
            {
                var message = "";
                foreach (var enemy in UnitManager.EnemyChampions.Where(x => !x.UnitComponentInfo.SkinName.Contains("TargetDummy", StringComparison.OrdinalIgnoreCase)))
                {
                    var spellBook = enemy.GetSpellBook();
                    var summoner1 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner1);
                    var summoner2 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner2);

                    if (ShouldSend(enemy, summoner1))
                    {
                        message += GetMessage(enemy, summoner1);
                    }
                    if (ShouldSend(enemy, summoner2))
                    {
                        message += GetMessage(enemy, summoner2);
                    }
                }

                Send(message.ToLowerInvariant());
                _lastMessage = GameEngine.GameTime;
                Keyboard.SendKeyUp(Keyboard.GetKeyBoardScanCode(GetKeybinding()));
            }
            else
            {
                Keyboard.SendKeyUp(Keyboard.GetKeyBoardScanCode(GetKeybinding()));
            }

            return Task.CompletedTask;
        }

        private static bool AnyKeysPressed()
        {
            return Keyboard.IsKeyPressed(GetOverrideKey()) ||
                   Keyboard.IsKeyPressed(GetComboKey()) ||
                   Keyboard.IsKeyPressed(GetHarassKey()) ||
                   Keyboard.IsKeyPressed(GetLaneclearKey()) ||
                   Keyboard.IsKeyPressed(GetLasthitKey());
        }

        private static string GetSummonerText(string summoner)
        {
            try
            {
                return summoner.ToLowerInvariant() switch
                {
                    "summonerflash" => "flash",
                    "summonerteleport" => "tp",
                    "summonerboost" => "cleanse",
                    "summonersmite" => "smite",
                    "summonerbarrier" => "barrier",
                    "summonermana" => "clarity",
                    "summonerexhaust" => "exhaust",
                    "summonerdot" => "ignite",
                    "summonerheal" => "heal",
                    "summonerhaste" => "ghost",
                    _ => summoner.ToLowerInvariant().Replace("summoner", "")
                };
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string GetMessage(Hero hero, Oasys.Common.GameObject.Clients.ExtendedInstances.Spells.SpellClass spellClass)
        {
            var expire = new TimeSpan(0, 0, (int)spellClass.CooldownExpire).ToString("mmss");

            return $"{hero.ModelName.ToLowerInvariant()} {GetSummonerText(spellClass.SpellData.SpellName)} {expire} ";
        }

        private static bool ShouldSend(Hero hero, Oasys.Common.GameObject.Clients.ExtendedInstances.Spells.SpellClass spellClass)
        {
            return !spellClass.IsSpellReady && _menuTab.GetItem<Switch>(hero.ModelName + " " + GetSummonerText(spellClass.SpellData.SpellName)).IsOn;
        }

        private static void Send(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Oasys.SDK.InputProviders.KeyboardProvider.PressKey(Oasys.SDK.InputProviders.KeyboardProvider.KeyBoardScanCodes.KEY_ENTER);
                SendKeys.SendWait(message + "{ENTER}");
                SendKeys.Flush();
            }
        }
    }
}
