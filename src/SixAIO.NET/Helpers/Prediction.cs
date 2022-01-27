using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.SDK;
using SharpDX;
using System;

namespace SixAIO.Helpers
{
    public static class Prediction
    {
        public static Vector3 LinePrediction(GameObjectBase target, int offset, float delay, float speed = -1f)
        {
            var velocity = target.AIManager.Velocity;
            velocity.Y = 0f;

            var orientation = velocity;
            orientation.Normalize();

            var waypoint = Vector3.Zero;

            waypoint = target.AIManager.GetNavPointCount() > 2
                        ? WaypointGrab(target, waypoint)
                        : (target.AIManager.NavEndPosition - target.Position).Normalized();

            if (velocity.X == 0f &&
                velocity.Y == 0f)
            {
                return target.Position;
            }

            var t = ((target.Position - UnitManager.MyChampion.Position).Length() / speed) + delay;
            var result = target.Position + (waypoint * (target.UnitStats.MoveSpeed * t));
            if ((result - target.Position).Length() > 400)
            {
                result = target.Position + (waypoint * target.UnitStats.MoveSpeed * delay + offset);
            }
            return result;
        }

        private static Vector3 WaypointGrab(GameObjectBase target, Vector3 waypoint)
        {
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
