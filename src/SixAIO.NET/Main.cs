using Oasys.Common;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.SDK;
using Oasys.SDK.Events;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Helpers;
using SixAIO.Models;
using SixAIO.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixAIO
{
    internal class Main
    {
        private static Champion _currentChampion;

        [Oasys.SDK.OasysModuleEntryPoint]
        public static void Execute()
        {
            GameEvents.OnGameLoadComplete += GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoSmite.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoHeal.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoCleanse.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += ChatCooldownAlerter.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameMatchComplete += GameEvents_OnGameMatchComplete;
            Oasys.Common.EventsProvider.GameEvents.OnCreateObject += GameEvents_OnCreateObject;
            Oasys.Common.EventsProvider.GameEvents.OnDeleteObject += GameEvents_OnDeleteObject;
        }
        //private static GameObjectBase _threshQ = null;
        private static Task GameEvents_OnDeleteObject(List<AIBaseClient> callbackObjectList, AIBaseClient callbackObject, float callbackGameTime)
        {
            try
            {
                _currentChampion?.OnDeleteObject(callbackObject);
                //if (callbackObject.Name.Contains("ThreshQMissile", StringComparison.OrdinalIgnoreCase))
                //{
                //    _threshQ = null;
                //}
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        private static Task GameEvents_OnCreateObject(List<AIBaseClient> callbackObjectList, AIBaseClient callbackObject, float callbackGameTime)
        {
            try
            {
                _currentChampion?.OnCreateObject(callbackObject);
                //if (callbackObject.Name.Contains("ThreshQMissile", StringComparison.OrdinalIgnoreCase))
                //{
                //    _threshQ = callbackObject;
                //}
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        private static Task GameEvents_OnGameLoadComplete()
        {
            //Prediction.Initialize();
            LoadChampion();
            _currentChampion?.InitializeMenu();
            CoreEvents.OnCoreRender += CoreEvents_OnCoreRender;
            CoreEvents.OnCoreMainTick += CoreEvents_OnCoreMainTick;
            CoreEvents.OnCoreMainInputAsync += CoreEvents_OnCoreMainInputAsync;
            CoreEvents.OnCoreHarassInputAsync += CoreEvents_OnCoreHarassInputAsync;
            CoreEvents.OnCoreLaneclearInputAsync += CoreEvents_OnCoreLaneclearInputAsync;
            CoreEvents.OnCoreLasthitInputAsync += CoreEvents_OnCoreLasthitInputAsync;
            CoreEvents.OnCoreMainTick += AutoSmite.OnCoreMainTick;
            CoreEvents.OnCoreLaneclearInputAsync += AutoSmite.OnCoreLaneclearInputAsync;
            CoreEvents.OnCoreLasthitInputAsync += AutoSmite.OnCoreLasthitInputAsync;
            CoreEvents.OnCoreMainTick += AutoHeal.OnCoreMainTick;
            CoreEvents.OnCoreMainInputAsync += AutoHeal.OnCoreMainInputAsync;
            CoreEvents.OnCoreMainInputRelease += CoreEvents_OnCoreMainInputRelease;

            return Task.CompletedTask;
        }

        private static void LoadChampion()
        {
            try
            {
                _currentChampion = Champion.GetChampion(UnitManager.MyChampion.ModelName);
            }
            catch (Exception)
            {
            }
        }

        private static Task CoreEvents_OnCoreMainInputRelease()
        {
            try
            {
                _currentChampion?.OnCoreMainInputRelease();
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        private static Task CoreEvents_OnCoreLasthitInputAsync()
        {
            try
            {
                _currentChampion?.OnCoreLastHitInput();
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        private static Task CoreEvents_OnCoreLaneclearInputAsync()
        {
            try
            {
                _currentChampion?.OnCoreLaneClearInput();
            }
            catch (Exception)
            {
            }

            return Task.CompletedTask;
        }

        private static Task CoreEvents_OnCoreMainInputAsync()
        {
            try
            {
                _currentChampion?.OnCoreMainInput();
            }
            catch (Exception)
            {
            }

            return Task.CompletedTask;
        }

        private static Task CoreEvents_OnCoreHarassInputAsync()
        {
            try
            {
                _currentChampion?.OnCoreHarassInput();
            }
            catch (Exception)
            {
            }

            return Task.CompletedTask;
        }

        private static Task CoreEvents_OnCoreMainTick()
        {
            try
            {
                _currentChampion?.OnCoreMainTick();
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        private static Task GameEvents_OnGameMatchComplete()
        {
            try
            {
                _currentChampion?.OnGameMatchComplete();
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        private static void CoreEvents_OnCoreRender()
        {
            //foreach (var missile in UnitManager.AllNativeObjects
            //                        .Where(x =>
            //                                !x.IsAlive &&
            //                                 x.IsObject(Oasys.Common.Enums.GameEnums.ObjectTypeFlag.AIMissileClient) &&
            //                                !x.IsObject(Oasys.Common.Enums.GameEnums.ObjectTypeFlag.AIMinionClient))
            //                        .Select(x => x.As<AIMissileClient>()))
            //{
            //    if (missile.Position != missile.EndPosition)
            //    {
            //        try
            //        {
            //            Oasys.SDK.Rendering.RenderFactory.DrawText(missile.Name, 12, missile.W2S, Color.Blue);
            //        }
            //        catch (Exception)
            //        {
            //        }
            //        Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(missile.Position, 60, Color.AliceBlue, 5);
            //    }
            //}

            //if (_threshQ is not null && _threshQ.IsAlive)
            //{
            //    var missile = _threshQ.As<AIMissileClient>();
            //    //try
            //    //{
            //    //    Oasys.SDK.Rendering.RenderFactory.DrawText(missile.Name, 12, _threshQ.W2S, Color.Blue);
            //    //}
            //    //catch (Exception)
            //    //{
            //    //}
            //    var w2s = LeagueNativeRendererManager.WorldToScreenSpell(missile.StartPosition);
            //    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(missile.Position, 60, Color.AliceBlue, 5);
            //    var missilew2s = LeagueNativeRendererManager.WorldToScreenSpell(missile.EndPosition);
            //    if (!missilew2s.IsZero)
            //    {
            //        Oasys.SDK.Rendering.RenderFactory.DrawLine(w2s.X, w2s.Y, missilew2s.X, missilew2s.Y, 60, Color.AliceBlue);
            //    }
            //}

            _currentChampion?.OnCoreRender();
        }
    }
}
