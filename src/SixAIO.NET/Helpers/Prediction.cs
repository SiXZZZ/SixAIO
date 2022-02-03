using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using SharpDX;
using System;

namespace SixAIO.Helpers
{
    public static class Prediction
    {
        internal static Tab MenuTab => MenuManagerProvider.GetTab($"SIXAIO - {nameof(Prediction)}");

        public static void Initialize()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Prediction)}"));
            MenuTab.AddItem(new Switch() { Title = "Use", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Offset", MinValue = -1000, MaxValue = 1000, Value = 0, ValueFrequency = 5 });
        }

        public static bool Use
        {
            get => MenuTab.GetItem<Switch>("Use").IsOn;
            set => MenuTab.GetItem<Switch>("Use").IsOn = value;
        }

        public static int PredictionOffset
        {
            get => MenuTab.GetItem<Counter>("Offset").Value;
            set => MenuTab.GetItem<Counter>("Offset").Value = value;
        }

        public static Vector3 LinePrediction(GameObjectBase target, float delay, float speed = -1f)
        {
            var waypoint = target.AIManager.GetNavPointCount() > 2
                    ? WaypointGrab(target)
                    : (target.AIManager.NavEndPosition - target.Position).Normalized();

            var t = ((target.Position - UnitManager.MyChampion.Position).Length() / speed) + delay;
            var result = target.Position + (waypoint * (target.UnitStats.MoveSpeed * t));
            if ((result - target.Position).Length() > 400)
            {
                result = target.Position + (waypoint * target.UnitStats.MoveSpeed * delay + PredictionOffset);
            }
            return result;
        }

        private static Vector3 WaypointGrab(GameObjectBase target)
        {
            var waypoint = Vector3.Zero;
            var navList = target.AIManager.GetNavPoints();
            foreach (var nav in navList)
            {
                if (target.AIManager.ServerPosition.Distance(target.AIManager.NavEndPosition) <
                    nav.Distance(target.AIManager.NavEndPosition))
                {
                    continue;
                }

                waypoint = ((nav - target.Position)).Normalized();
            }

            return waypoint;
        }
    }
}
