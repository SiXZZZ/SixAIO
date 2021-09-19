namespace SixAIO.Champions
{
    //internal static class Xerath
    //{
    //    private static float _castTimeQ = 0.5f;
    //    private static float _castTime = 0.1f;
    //    private static float _lastQTime = 0;
    //    private static float _lastWTime = 0;
    //    private static float _lastETime = 0;
    //    private static GameObjectBase _cachedQTarget;
    //    private static Vector3 _previousPosition;
    //    private static float _cacheTime;

    //    internal static void OnCoreMainInput()
    //    {
    //        var mySpellBook = UnitManager.MyChampion.GetSpellBook();

    //        var enemies = UnitManager.EnemyChampions
    //            .Where(x => IsInRange(x, 2000) && TargetSelector.IsAttackable(x))
    //            .OrderBy(x => x.Distance)
    //            .AsEnumerable<GameObjectBase>();

    //        if (enemies != null)
    //        {
    //            //CastE(mySpellBook, enemies);
    //            CastW(mySpellBook, enemies);
    //            CastQ(mySpellBook, enemies);
    //        }
    //    }

    //    private static void CastQ(SpellBook mySpellBook, IEnumerable<GameObjectBase> enemies)
    //    {
    //        var qTarget = enemies.Where(x => IsInRange(x, 1500)).FirstOrDefault();
    //        if (mySpellBook.GetSpellClass(SpellSlot.Q).IsSpellReady &&
    //            UnitManager.MyChampion.Mana > 120 &&
    //            qTarget != null &&
    //            GameEngine.GameTime > _lastQTime + _castTimeQ)
    //        {
    //            if (_lastQTime == 0)
    //            {
    //                _cachedQTarget = qTarget;
    //                SpellCastProvider.StartChargeSpell(SpellCastSlot.Q);
    //                Orbwalker.OrbwalkingMode = Oasys.Common.Logic.OrbwalkingMode.Move;
    //                _lastQTime = GameEngine.GameTime;
    //            }
    //            if (GameEngine.GameTime >= _lastQTime + CalculateQExtendableTime(_cachedQTarget.Distance))
    //            {
    //                ReleaseQ(_cachedQTarget);
    //                _lastQTime = 0;
    //            }
    //        }
    //    }

    //    private static void CastW(SpellBook mySpellBook, IEnumerable<GameObjectBase> enemies)
    //    {
    //        var wTarget = enemies.Where(x => IsInRange(x, mySpellBook.GetSpellClass(SpellSlot.W).SpellData.CastRange)).FirstOrDefault();
    //        if (mySpellBook.GetSpellClass(SpellSlot.W).IsSpellReady &&
    //            UnitManager.MyChampion.Mana > 110 &&
    //            wTarget != null &&
    //            GameEngine.GameTime > _lastWTime + _castTime)
    //        {
    //            SpellCastProvider.CastSpell(CastSlot.W, wTarget.Position, _castTime);
    //            _lastWTime = GameEngine.GameTime;
    //        }
    //    }

    //    private static void CastE(SpellBook mySpellBook, IEnumerable<GameObjectBase> enemies)
    //    {
    //        var eTarget = enemies.Where(x => IsInRange(x, mySpellBook.GetSpellClass(SpellSlot.E).SpellData.CastRange)).FirstOrDefault();
    //        if (mySpellBook.GetSpellClass(SpellSlot.E).IsSpellReady &&
    //            UnitManager.MyChampion.Mana > 80 &&
    //            eTarget != null &&
    //            GameEngine.GameTime > _lastETime + _castTime)
    //        {
    //            SpellCastProvider.CastSpell(CastSlot.E, eTarget.Position, _castTime);
    //            _lastETime = GameEngine.GameTime;
    //        }
    //    }

    //    private static void ReleaseQ(GameObjectBase qTarget)
    //    {
    //        var predictPos = GetPrediction(qTarget);
    //        SpellCastProvider.ReleaseChargeSpell(SpellCastSlot.Q, predictPos, _castTimeQ);
    //        Orbwalker.OrbwalkingMode = Oasys.Common.Logic.OrbwalkingMode.Combo;
    //    }

    //    internal static Vector2 GetPrediction(GameObjectBase target, float addMoveDeltaTime = 0f)
    //    {
    //        var moveDelta = Vector3.Normalize(target.Position - _previousPosition);
    //        var movePredictPos = moveDelta * target.UnitStats.MoveSpeed * addMoveDeltaTime;
    //        Vector2 v = LeagueNativeRendererManager.WorldToScreen(target.Position/* + (addMoveDeltaTime > 0f ? movePredictPos : Vector3.Zero)*/);
    //        return v;
    //    }

    //    internal static void OnCoreMainTick()
    //    {
    //        if (_cachedQTarget != null && EngineManager.GameTime > _cacheTime + 0.01f)
    //        {
    //            _cacheTime = EngineManager.GameTime;
    //            _previousPosition = _cachedQTarget.Position;
    //        }
    //    }

    //    internal static void OnCoreMainInputRelease()
    //    {
    //    }

    //    private static float CalculateQExtendableTime(float distance)
    //    {
    //        return (distance - 735) / 4.0856f / 100;
    //    }

    //    internal static void OnCoreLaneClearInput()
    //    {
    //    }

    //    private static bool IsInRange(GameObjectBase targHero, float range)
    //    {
    //        return targHero.Distance-100 <= range + targHero.UnitComponentInfo.UnitBoundingRadius + 5;
    //    }
    //}
}

