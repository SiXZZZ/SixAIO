using Oasys.Common;
using Oasys.Common.Extensions;
using Oasys.SDK;
using SharpDX;

namespace SixAIO.Helpers
{
    public static class Collision
    {
        public static bool IsLineCollision(Vector3 positionToTest, Vector3 targetPosition, float targetRadius, float spellWidth)
        {
            positionToTest = (Vector3)LeagueNativeRendererManager.WorldToScreen(positionToTest);
            targetPosition = (Vector3)LeagueNativeRendererManager.WorldToScreen(targetPosition);
            var sourcePos = (Vector3)UnitManager.MyChampion.W2S;
            var vecEndStart = targetPosition - sourcePos;
            var vecCircleStart = positionToTest - sourcePos;
            var vecProjectionOn = ProjectionOn(vecCircleStart, vecEndStart);
            vecProjectionOn = vecEndStart.Normalized() * vecProjectionOn.Length();
            if (vecProjectionOn.Length() > vecEndStart.Length())
            {
                vecProjectionOn = vecEndStart;
                var angleBetween = positionToTest.AngleBetween(targetPosition);
                return (targetPosition.Distance(positionToTest)) < targetRadius;
            }
            else
            {
                var vecCircleToProjection = (vecCircleStart - vecProjectionOn);
                var distFromLineToCircle = vecCircleToProjection.Length();
                return 1 + targetRadius >= distFromLineToCircle;
            }
        }

        public static bool IsLineCollision(Vector3 positionToTest, Vector3 targetPosition, Vector3 startPosition, float targetRadius, float spellWidth)
        {
            positionToTest = (Vector3)LeagueNativeRendererManager.WorldToScreen(positionToTest);
            targetPosition = (Vector3)LeagueNativeRendererManager.WorldToScreen(targetPosition);
            var sourcePos = (Vector3)LeagueNativeRendererManager.WorldToScreen(startPosition);
            var vecEndStart = targetPosition - sourcePos;
            var vecCircleStart = positionToTest - sourcePos;
            var vecProjectionOn = ProjectionOn(vecCircleStart, vecEndStart);
            vecProjectionOn = vecEndStart.Normalized() * vecProjectionOn.Length();
            if (vecProjectionOn.Length() > vecEndStart.Length())
            {
                vecProjectionOn = vecEndStart;
                var angleBetween = positionToTest.AngleBetween(targetPosition);
                return (targetPosition.Distance(positionToTest)) < targetRadius;
            }
            else
            {
                var vecCircleToProjection = (vecCircleStart - vecProjectionOn);
                var distFromLineToCircle = vecCircleToProjection.Length();
                return 5 + targetRadius >= distFromLineToCircle;
            }
        }

        private static Vector3 ProjectionOn(Vector3 source, Vector3 vOther)
        {
            var dot = Vector3.Dot(source, vOther);
            var dot2 = Vector3.Dot(vOther, vOther);
            return vOther * (dot / dot2);
        }

        private static float DotProduct(Vector3 source, Vector3 other)
        {
            return source.X * other.X + source.Y * other.Y + source.Z * other.Z;
        }

        public static bool MinionCollision(Vector3 targetPosition, float dist, float width)
        {
            foreach (var enemyMinion in UnitManager.EnemyMinions)
            {
                if (LeagueNativeRendererManager.WorldToScreen(enemyMinion.Position) == Vector2.Zero || !enemyMinion.IsAlive)
                {
                    continue;
                }

                if (IsLineCollision(enemyMinion.Position, targetPosition,
                    enemyMinion.UnitComponentInfo.UnitBoundingRadius, width))
                {
                    return true;
                }
            }

            return false;

        }

        public static bool AllyMinionCollision(Vector3 targetPosition, Vector3 startPosition, float dist, float width)
        {
            foreach (var allyMinions in UnitManager.AllyMinions)
            {
                if (LeagueNativeRendererManager.WorldToScreen(allyMinions.Position) == Vector2.Zero || !allyMinions.IsAlive)
                {
                    continue;
                }

                if (IsLineCollision(allyMinions.Position, targetPosition, startPosition,
                    allyMinions.UnitComponentInfo.UnitBoundingRadius, width))
                {
                    return true;
                }
            }

            return false;

        }

        public static bool AllyHeroCollision(Vector3 targetPosition, Vector3 startPosition, float dist, float width)
        {
            foreach (var hero in UnitManager.AllyChampions)
            {
                if (LeagueNativeRendererManager.WorldToScreen(hero.Position) == Vector2.Zero || !hero.IsAlive)
                {
                    continue;
                }

                if (hero.ModelName == UnitManager.MyChampion.ModelName)
                {
                    continue;
                }

                if (IsLineCollision(hero.Position, targetPosition, startPosition,
                    hero.UnitComponentInfo.UnitBoundingRadius, width))
                {
                    return true;
                }
            }

            return false;

        }

    }
}
