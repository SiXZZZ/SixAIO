﻿using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools.Devices;
using Oasys.SDK;
using Oasys.SDK.Tools;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SixAIO.Utilities
{
    internal sealed class ChatCooldownAlerter
    {
        static ChatCooldownAlerter()
        {
            Keyboard.OnKeyPress += Keyboard_OnKeyPress;
        }

        private static Tab OrbTab => MenuManagerProvider.GetTab("Orbwalker");
        private static Group OrbInputTab => OrbTab.GetGroup("Input");

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

        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group ChatCDAlerterGroup => Tab.GetGroup("Chat CD Alerter");

        private static bool Use
        {
            get => ChatCDAlerterGroup?.GetItem<Switch>("Use")?.IsOn ?? false;
            set => ChatCDAlerterGroup.GetItem<Switch>("Use").IsOn = value;
        }

        public static Keys GetKeybinding()
        {
            return ChatCDAlerterGroup.GetItem<KeyBinding>("Key binding").SelectedKey;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Chat CD Alerter"));
            ChatCDAlerterGroup.AddItem(new Switch() { Title = "Use", IsOn = false });
            ChatCDAlerterGroup.AddItem(new KeyBinding() { Title = "Key binding", SelectedKey = Keys.M });

            ChatCDAlerterGroup.AddItem(new InfoDisplay() { Title = "-Only alert enabled summoners-" });
            foreach (var enemy in UnitManager.EnemyChampions.Where(x => !x.IsTargetDummy))
            {
                var spellBook = enemy.GetSpellBook();
                var summoner1 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner1);
                var summoner2 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner2);

                var sum1 = enemy.ModelName + " " + GetSummonerText(summoner1.SpellData.SpellName);
                var sum2 = enemy.ModelName + " " + GetSummonerText(summoner2.SpellData.SpellName);
                ChatCDAlerterGroup.AddItem(new InfoDisplay() { Title = $"-{enemy.ModelName}-" });
                if (!string.IsNullOrEmpty(sum1))
                {
                    ChatCDAlerterGroup.AddItem(new Switch() { Title = sum1, IsOn = !string.IsNullOrEmpty(sum1) });
                }
                if (!string.IsNullOrEmpty(sum2))
                {
                    ChatCDAlerterGroup.AddItem(new Switch() { Title = sum2, IsOn = !string.IsNullOrEmpty(sum2) });
                }
            }

            return Task.CompletedTask;
        }

        private static float _lastMessage = 0f;

        private static void Keyboard_OnKeyPress(Keys keyBeingPressed, Keyboard.KeyPressState pressState)
        {
            try
            {
                if (keyBeingPressed == Keys.None)
                {
                    return;
                }

                var gameTime = GameEngine.GameTime;
                //Logger.Log($"{keyBeingPressed} {pressState}");
                if (Use &&
                    !AnyKeysPressed() &&
                    !GameEngine.ChatBox.IsChatBoxOpen &&
                    GameEngine.IsGameWindowFocused &&
                    Keyboard.IsPressed(GetKeybinding()) &&
                    _lastMessage + 10 < gameTime)
                {
                    _lastMessage = gameTime;
                    foreach (var enemy in UnitManager.EnemyChampions.Where(x => !x.IsTargetDummy))
                    {
                        var spellBook = enemy.GetSpellBook();
                        var summoner1 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner1);
                        var summoner2 = spellBook.GetSpellClass(Oasys.Common.Enums.GameEnums.SpellSlot.Summoner2);

                        var message = $"{enemy.ModelName.ToLowerInvariant()} ";
                        if (ShouldSend(enemy, summoner1))
                        {
                            message += GetMessage(summoner1).ToLowerInvariant();
                        }
                        if (ShouldSend(enemy, summoner2))
                        {
                            message += GetMessage(summoner2).ToLowerInvariant();
                        }
                        if (message != $"{enemy.ModelName.ToLowerInvariant()} ")
                        {
                            Send(message.ToLowerInvariant());
                            Keyboard.SendKeyUp(Keyboard.GetKeyBoardScanCode(GetKeybinding()));
                        }
                    }
                }
                else
                {
                    Keyboard.SendKeyUp(Keyboard.GetKeyBoardScanCode(GetKeybinding()));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
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
                if (summoner.Contains("smite", StringComparison.InvariantCultureIgnoreCase))
                {
                    return "smite";
                }
                return summoner.ToLowerInvariant() switch
                {
                    "summonerflash" => "flash",
                    "summonerteleport" => "tp",
                    "summonerteleportupgrade" => "tp",
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
            catch (Exception ex)
            {
                Logger.Log(ex);
                return string.Empty;
            }
        }

        private static string GetMessage(SpellClass spellClass)
        {
            try
            {
                var expire = new TimeSpan(0, 0, (int)spellClass.CooldownExpire).ToString("mmss");

                return $"{GetSummonerText(spellClass.SpellData.SpellName)} {expire} ";
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return string.Empty;
            }
        }

        private static bool ShouldSend(Hero hero, SpellClass spellClass)
        {
            try
            {
                return !spellClass.IsSpellReady && ChatCDAlerterGroup.GetItem<Switch>(hero.ModelName + " " + GetSummonerText(spellClass.SpellData.SpellName)).IsOn;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
        }

        private static void Send(string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    Oasys.SDK.InputProviders.KeyboardProvider.PressKey(Oasys.SDK.InputProviders.KeyboardProvider.KeyBoardScanCodes.KEY_ENTER);
                    Thread.Sleep(5);
                    SendKeys.SendWait(" " + message + "{ENTER}");
                    Thread.Sleep(5);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
