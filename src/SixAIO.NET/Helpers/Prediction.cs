using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.SDK;
using SharpDX;
using System;

namespace SixAIO.Helpers
{
    public static class Prediction
    {
        public static int lastEnemyAA = 0;

        public static Vector3 LinePrediction(GameObjectBase target, int offset, float delay, float speed = -1f)
        {
            //if (target.IsCastingSpell && !target.IsMelee())
            //{
            //    if (target.GetCurrentCastingSpell().IsBasicAttack)
            //    {
            //        if (lastEnemyAA == 0 || target.GetCurrentCastingSpell().IsBasicAttack)
            //        {
            //            lastEnemyAA = Environment.TickCount;
            //        }
            //        if (Environment.TickCount < lastEnemyAA)
            //        {
            //            return Vector3.Zero;
            //        }
            //    }
            //}
            //if (lastEnemyAA == 0)
            //{
            //    lastEnemyAA = Environment.TickCount;
            //}
            //if (Environment.TickCount < lastEnemyAA + 20)
            //{
            //    return Vector3.Zero;
            //}
            var t = ((target.Position - UnitManager.MyChampion.Position).Length() / speed) + delay;

            var velocity = target.AIManager.Velocity;
            velocity.Y = 0f;

            var orientation = velocity; orientation.Normalize();

            var waypoint = Vector3.Zero;

            if (target.AIManager.GetNavPointCount() > 2)
            {
                waypoint = WaypointGrab(target, waypoint);
            }
            else
            {
                waypoint = ((target.AIManager.NavEndPosition - target.Position)).Normalized();
            }


            if (velocity.X == 0f && velocity.Y == 0f)
            {
                return target.Position;
            }

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
