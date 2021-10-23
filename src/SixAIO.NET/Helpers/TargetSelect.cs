using Oasys.Common;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.SDK;
using SharpDX;
using SixAIO.Models;

namespace SixAIO.Helpers
{
    public static class TargetSelect
    {
        public static Hero GetClosestTarget(float dist)
        {
            Hero target = null;
            foreach (var enemyChampion in UnitManager.EnemyChampions)
            {
                if (!enemyChampion.IsAlive || !enemyChampion.IsTargetable || !enemyChampion.IsVisible)
                {
                    continue;
                }

                var currentTargetDistance = enemyChampion.DistanceTo(UnitManager.MyChampion.Position);
                if (currentTargetDistance < dist)
                {
                    if (target == null)
                    {
                        target = enemyChampion;
                    }
                    else
                    {
                        if (currentTargetDistance < target.DistanceTo(UnitManager.MyChampion.Position))
                        {
                            target = enemyChampion;
                        }
                    }
                }
            }

            return target;
        }
        public static Hero GetLowestHealthTarget(float dist)
        {
            Hero target = null;
            foreach (var enemyChampion in UnitManager.EnemyChampions)
            {
                if (!enemyChampion.IsAlive || !enemyChampion.IsTargetable || !enemyChampion.IsVisible)
                {
                    continue;
                }

                if (enemyChampion.DistanceTo(UnitManager.MyChampion.Position) > dist - 30)
                {
                    continue;
                }

                var currentHealth = enemyChampion.Health;
                if (currentHealth > 0)
                {
                    if (target == null)
                    {
                        target = enemyChampion;
                    }
                    else
                    {
                        if (currentHealth < target.Health)
                        {
                            target = enemyChampion;
                        }
                    }
                }
            }
            return target;
        }

        public static bool IsATargetInRange(float dist)
        {
            foreach (var enemyChampion in UnitManager.EnemyChampions)
            {
                if (!enemyChampion.IsAlive || !enemyChampion.IsTargetable || !enemyChampion.IsVisible)
                {
                    continue;
                }

                var currentTargetDistance = enemyChampion.DistanceTo(UnitManager.MyChampion.Position);
                if (currentTargetDistance < dist)
                {
                    return true;
                }
            }

            return false;
        }

        public static Hero GetAnyTargetWithoutCollision(Vector3 prediction, Spell spell, float dist)
        {
            foreach (var enemyChampion in UnitManager.EnemyChampions)
            {
                if (!enemyChampion.IsAlive || !enemyChampion.IsTargetable || !enemyChampion.IsVisible)
                {
                    continue;
                }

                var currentTargetDistance = enemyChampion.DistanceTo(UnitManager.MyChampion.Position);
                if (currentTargetDistance < dist)
                {
                    if (LeagueNativeRendererManager.WorldToScreen(enemyChampion.Position) == Vector2.Zero)
                    {
                        continue;
                    }

                    if (!Collision.MinionCollision(prediction, spell.Width) &&
                        !Collision.MinionCollision(enemyChampion.Position, spell.Width))
                    {
                        return enemyChampion;
                    }
                }
            }

            return null;
        }
        public static bool CountTargetsInRange(float dist, float amount)
        {
            float i = 0;
            foreach (var enemyChampion in UnitManager.EnemyChampions)
            {
                if (!enemyChampion.IsAlive || !enemyChampion.IsTargetable || !enemyChampion.IsVisible)
                {
                    continue;
                }

                var currentTargetDistance = enemyChampion.DistanceTo(UnitManager.MyChampion.Position);
                if (currentTargetDistance < dist)
                {
                    i++;
                    if (i > amount)
                    {
                        return true;
                    }
                }
            }
            return false;
        }




    }
}
