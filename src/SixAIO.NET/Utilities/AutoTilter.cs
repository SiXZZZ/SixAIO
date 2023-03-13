using Oasys.Common;
using Oasys.Common.Logic.Helpers.GameData;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools;
using Oasys.SDK;
using Oasys.SDK.Events;
using Oasys.SDK.InputProviders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SixAIO.Utilities
{
    internal sealed class AutoTilter
    {
        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group UseAutoTilterGroup => Tab.GetGroup("Auto Tilter");

        private static bool UseAutoTilter
        {
            get => UseAutoTilterGroup.GetItem<Switch>("Use Auto Tilter").IsOn;
            set => UseAutoTilterGroup.GetItem<Switch>("Use Auto Tilter").IsOn = value;
        }

        private static int AutoTilterSafeZone
        {
            get => UseAutoTilterGroup.GetItem<Counter>("Safe Zone Range").Value;
            set => UseAutoTilterGroup.GetItem<Counter>("Safe Zone Range").Value = value;
        }

        private static string AllyGetsKill
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Gets Kill").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Gets Kill").Value = value;
        }

        private static string AllyGetsFirstKill
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Gets First Kill").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Gets First Kill").Value = value;
        }

        private static string AllyGetsDragon
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Gets Dragon").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Gets Dragon").Value = value;
        }

        private static string AllyGetsBaron
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Gets Baron").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Gets Baron").Value = value;
        }

        private static string AllyGetsHerald
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Gets Herald").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Gets Herald").Value = value;
        }

        private static string AllyStealDragon
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Steal Dragon").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Steal Dragon").Value = value;
        }

        private static string AllyStealBaron
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Steal Baron").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Steal Baron").Value = value;
        }

        private static string AllyStealHerald
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Steal Herald").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Steal Herald").Value = value;
        }

        private static string AllyGetsInhib
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Gets Inhib").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Gets Inhib").Value = value;
        }

        private static string AllyGetsTurret
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Gets Turret").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Gets Turret").Value = value;
        }

        private static string AllyGetsFirstTurret
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Gets First Turret").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Gets First Turret").Value = value;
        }

        private static string AllyGetsAce
        {
            get => UseAutoTilterGroup.GetItem<Text>("Ally Gets Ace").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Ally Gets Ace").Value = value;
        }

        private static string GameEnd
        {
            get => UseAutoTilterGroup.GetItem<Text>("Game End").Value;
            set => UseAutoTilterGroup.GetItem<Text>("Game End").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Auto Tilter"));
            UseAutoTilterGroup.AddItem(new Switch() { Title = "Use Auto Tilter", IsOn = false });
            UseAutoTilterGroup.AddItem(new Counter() { Title = "Safe Zone Range", Value = 2000, MinValue = 0, MaxValue = 15000, ValueFrequency = 50 });
            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Gets Kill", Value = "" });
            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Gets First Kill", Value = "" });

            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Gets Dragon", Value = "" });
            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Gets Baron", Value = "" });
            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Gets Herald", Value = "" });

            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Steal Dragon", Value = "" });
            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Steal Baron", Value = "" });
            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Steal Herald", Value = "" });

            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Gets Inhib", Value = "" });
            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Gets Turret", Value = "" });
            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Gets First Turret", Value = "" });

            UseAutoTilterGroup.AddItem(new Text() { Title = "Ally Gets Ace", Value = "" });

            UseAutoTilterGroup.AddItem(new Text() { Title = "Game End", Value = "" });

            GameEvents.OnGameEvent += GameEvents_OnGameEvent;

            return Task.CompletedTask;
        }

        private static DateTime _lastMessage;
        private static Task GameEvents_OnGameEvent(Event eventInfo)
        {
            if (UseAutoTilter &&
                EngineManager.IsGameWindowFocused &&
                !EngineManager.ChatClient.IsChatBoxOpen &&
                DateTime.UtcNow > _lastMessage.AddMilliseconds(5) &&
                UnitManager.EnemyChampions.Where(x => x.IsAlive).All(x => x.Distance >= AutoTilterSafeZone))
            {
                var victim = EngineManager.AllGameData.AllPlayers.FirstOrDefault(x => x.SummonerName == eventInfo.VictimName);
                var killer = EngineManager.AllGameData.AllPlayers.FirstOrDefault(x => x.SummonerName == eventInfo.KillerName);
                var killerIsAlly = killer is not null && killer.Team.ToLowerInvariant() == UnitManager.MyChampion.Team.ToString().ToLowerInvariant();
                var killerIsEnemy = !killerIsAlly;
                var victimIsAlly = victim is not null && victim.Team.ToLowerInvariant() == UnitManager.MyChampion.Team.ToString().ToLowerInvariant();
                var victimIsEnemy = !victimIsAlly;
                var killerIsMe = killerIsAlly && UnitManager.MyChampion.AllPlayerData.SummonerName == killer.SummonerName;
                var victimIsMe = victimIsAlly && UnitManager.MyChampion.AllPlayerData.SummonerName == victim.SummonerName;
                var message = eventInfo.Type switch
                {
                    Event.EventType.ChampionKill => killerIsAlly ? AllyGetsKill : string.Empty,
                    Event.EventType.FirstBlood => killerIsAlly ? AllyGetsFirstKill : string.Empty,
                    Event.EventType.Ace => killerIsAlly ? AllyGetsAce : string.Empty,
                    Event.EventType.TurretKilled => killerIsAlly ? AllyGetsTurret : string.Empty,
                    Event.EventType.FirstBrick => killerIsAlly ? AllyGetsFirstTurret : string.Empty,
                    Event.EventType.InhibKilled => killerIsAlly ? AllyGetsInhib : string.Empty,
                    Event.EventType.DragonKill => killerIsAlly
                                                ? eventInfo.Stolen.ToLowerInvariant() == "true"
                                                    ? AllyStealDragon
                                                    : AllyGetsDragon
                                                : string.Empty,
                    Event.EventType.HeraldKill => killerIsAlly
                                                ? eventInfo.Stolen.ToLowerInvariant() == "true"
                                                    ? AllyStealHerald
                                                    : AllyGetsHerald
                                                : string.Empty,
                    Event.EventType.BaronKill => killerIsAlly
                                                ? eventInfo.Stolen.ToLowerInvariant() == "true"
                                                    ? AllyStealBaron
                                                    : AllyGetsBaron
                                                : string.Empty,
                    Event.EventType.GameEnd => GameEnd,
                    _ => string.Empty,
                };

                SendMessage(message);
            }

            return Task.CompletedTask;
        }

        private static void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            message = $"/all {message}";

            NativeImport.BlockInput(true);
            KeyboardProvider.PressKey(KeyboardProvider.KeyBoardScanCodes.KEY_ENTER);
            Thread.Sleep(1);
            SendKeys.SendWait($"{message}" + "{ENTER}");
            Thread.Sleep(1);
            NativeImport.BlockInput(false);
            _lastMessage = DateTime.UtcNow;
        }
    }
}
