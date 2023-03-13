using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools.Devices;
using Oasys.SDK;
using Oasys.SDK.Events;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal sealed class AntiAFK
    {
        private static DateTime _lastMovement;
        private static int _lastAFKPositionIndex = 0;

        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group AntiAFKGroup => Tab.GetGroup("Anti AFK");

        private static bool UseAntiAFK
        {
            get => AntiAFKGroup.GetItem<Switch>("Use Anti AFK").IsOn;
            set => AntiAFKGroup.GetItem<Switch>("Use Anti AFK").IsOn = value;
        }

        private static int AntiAFKDelaySeconds
        {
            get => AntiAFKGroup.GetItem<Counter>("Anti AFK Delay Seconds").Value;
            set => AntiAFKGroup.GetItem<Counter>("Anti AFK Delay Seconds").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Anti AFK"));
            AntiAFKGroup.AddItem(new Switch() { Title = "Use Anti AFK", IsOn = false });
            AntiAFKGroup.AddItem(new Counter() { Title = "Anti AFK Delay Seconds", Value = 5, MinValue = 5, MaxValue = 30, ValueFrequency = 1 });

            CoreEvents.OnCoreMainTick += OnCoreMainTick;
            GameEvents.OnGameNewPath += GameEvents_OnGameNewPath;

            return Task.CompletedTask;
        }

        private static Task GameEvents_OnGameNewPath(AIBaseClient obj, IEnumerable<Vector3> oldPath, IEnumerable<Vector3> newPath)
        {
            if (obj.IsMe)
            {
                _lastMovement = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }

        private static readonly List<Vector3> _afkOrderPositions = new List<Vector3>()
        {
            new Vector3(310, 182, 610),
            new Vector3(610, 182, 310)
        };

        private static readonly List<Vector3> _afkChaosPositions = new List<Vector3>()
        {
            new Vector3(14096, 172, 14450),
            new Vector3(14402, 172, 14060)
        };

        internal static Task OnCoreMainTick()
        {
            if (UseAntiAFK &&
                EngineManager.IsGameWindowFocused &&
                EngineManager.MissionInfo.MapID == MapIDFlag.SummonersRift &&
                DateTime.UtcNow > _lastMovement.AddSeconds(AntiAFKDelaySeconds))
            {
                if (UnitManager.MyChampion.Team == TeamFlag.Order &&
                    UnitManager.MyChampion.Position.Distance(new Vector3(300, 182, 300)) <= 1000)
                {
                    var w2s = _afkOrderPositions[_lastAFKPositionIndex].ToWorldToMap();
                    Mouse.ClickAndBounce(w2s);
                    _lastMovement = DateTime.UtcNow;

                    _lastAFKPositionIndex = _lastAFKPositionIndex == 0
                        ? 1
                        : 0;
                }

                if (UnitManager.MyChampion.Team == TeamFlag.Chaos &&
                    UnitManager.MyChampion.Position.Distance(new Vector3(14296, 172, 14378)) <= 1000)
                {
                    var w2s = _afkChaosPositions[_lastAFKPositionIndex].ToWorldToMap();
                    Mouse.ClickAndBounce(w2s);
                    _lastMovement = DateTime.UtcNow;

                    _lastAFKPositionIndex = _lastAFKPositionIndex == 0
                        ? 1
                        : 0;
                }
            }

            return Task.CompletedTask;
        }
    }
}
