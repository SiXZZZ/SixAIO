using Oasys.Common;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools;
using Oasys.SDK.Events;
using Oasys.SDK.InputProviders;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal sealed class AntiChat
    {
        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group AntiChatGroup => Tab.GetGroup("Anti Chat");

        private static bool UseAntiChat
        {
            get => AntiChatGroup.GetItem<Switch>("Use Anti Chat").IsOn;
            set => AntiChatGroup.GetItem<Switch>("Use Anti Chat").IsOn = value;
        }

        private static int AntiChatSleepTimerMS
        {
            get => AntiChatGroup.GetItem<Counter>("Anti Chat Sleep Timer MS").Value;
            set => AntiChatGroup.GetItem<Counter>("Anti Chat Sleep Timer MS").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Anti Chat"));
            AntiChatGroup.AddItem(new Switch() { Title = "Use Anti Chat", IsOn = false });
            AntiChatGroup.AddItem(new Counter() { Title = "Anti Chat Sleep Timer MS", Value = 1000, MinValue = 0, MaxValue = 2000, ValueFrequency = 50 });

            CoreEvents.OnCoreMainTick += OnCoreMainTick;

            return Task.CompletedTask;
        }

        private static DateTime _lastCheck;
        internal static Task OnCoreMainTick()
        {
            if (UseAntiChat &&
                EngineManager.IsGameWindowFocused &&
                EngineManager.ChatClient.IsChatBoxOpen &&
                DateTime.UtcNow > _lastCheck.AddMilliseconds(10))
            {
                if (AntiChatSleepTimerMS > 0)
                {
                    NativeImport.BlockInput(true);
                    Thread.Sleep(AntiChatSleepTimerMS);
                    if (EngineManager.ChatClient.IsChatBoxOpen)
                    {
                        KeyboardProvider.PressKey(System.Windows.Forms.Keys.Escape);
                    }
                    NativeImport.BlockInput(false);
                }
                else
                {
                    NativeImport.BlockInput(true);
                    if (EngineManager.ChatClient.IsChatBoxOpen)
                    {
                        KeyboardProvider.PressKey(System.Windows.Forms.Keys.Escape);
                    }
                    NativeImport.BlockInput(false);
                }

                _lastCheck = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }
    }
}
