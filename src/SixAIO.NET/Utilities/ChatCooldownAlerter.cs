using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools.Devices;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.Tools;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SixAIO.Utilities
{
    internal class ChatCooldownAlerter
    {
        static ChatCooldownAlerter()
        {
            Keyboard.OnKeyPress += Keyboard_OnKeyPress;
        }

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
            get => _menuTab?.GetItem<Switch>("Use")?.IsOn ?? false;
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

        private static void Keyboard_OnKeyPress(Keys keyBeingPressed, Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == Keys.None)
            {
                return;
            }
            Logger.Log($"{keyBeingPressed} {pressState}");
            if (Use &&
                !AnyKeysPressed() &&
                !GameEngine.ChatBox.IsChatBoxOpen &&
                GetForegroundWindow() == System.Diagnostics.Process.GetProcessesByName("League of Legends").FirstOrDefault().MainWindowHandle &&
                Keyboard.IsPressed(GetKeybinding()) &&
                _lastMessage + 10 < GameEngine.GameTime)
            {
                _lastMessage = GameEngine.GameTime;
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
                if (Keyboard.IsPressed(GetKeybinding()))
                {
                    Keyboard.SendKeyUp(Keyboard.GetKeyBoardScanCode(GetKeybinding()));
                }
            }
            else
            {
                if (Keyboard.IsPressed(GetKeybinding()))
                {
                    Keyboard.SendKeyUp(Keyboard.GetKeyBoardScanCode(GetKeybinding()));
                }
            }
        }

        private static bool AnyKeysPressed() => Keyboard.IsPressed(GetOverrideKey()) ||
                                                Keyboard.IsPressed(GetComboKey()) ||
                                                Keyboard.IsPressed(GetHarassKey()) ||
                                                Keyboard.IsPressed(GetLaneclearKey()) ||
                                                Keyboard.IsPressed(GetLasthitKey());

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
            }
        }
    }
}
