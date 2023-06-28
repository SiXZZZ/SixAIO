using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.SDK;
using Oasys.SDK.Events;
using Oasys.SDK.Menu;
using Oasys.SDK.Tools;
using SixAIO.Models;
using SixAIO.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SixAIO
{
    internal sealed class Main
    {
        private static Champion _currentChampion;

        [OasysModuleEntryPoint]
        public static void Execute()
        {
            Logger.Log($"Initialize SiXAIO [{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}]");
            GameEvents.OnGameLoadComplete += GameEvents_OnGameLoadComplete;
            GameEvents.OnGameMatchComplete += GameEvents_OnGameMatchComplete;
            Oasys.Common.EventsProvider.GameEvents.OnCreateObject += GameEvents_OnCreateObject;
            Oasys.Common.EventsProvider.GameEvents.OnDeleteObject += GameEvents_OnDeleteObject;

            //utilities
            //GameEvents.OnGameLoadComplete += ChatCooldownAlerter.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AntiChat.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AntiAFK.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoComplimenter.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoTilter.GameEvents_OnGameLoadComplete;

            //items
            GameEvents.OnGameLoadComplete += AutoRanduins.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoIronSpikeWhip.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoGoreDrinker.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoEverfrost.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoGargoyleStoneplate.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoGaleforce.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoStrideBreaker.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoMikaelsBlessing.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoPotion.GameEvents_OnGameLoadComplete;

            //summoners
            GameEvents.OnGameLoadComplete += AutoSmite.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoHeal.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoCleanse.GameEvents_OnGameLoadComplete;
            GameEvents.OnGameLoadComplete += AutoExhaust.GameEvents_OnGameLoadComplete;
        }

        private static Task GameEvents_OnDeleteObject(List<AIBaseClient> callbackObjectList, AIBaseClient callbackObject, float callbackGameTime)
        {
            try
            {
                _currentChampion?.OnDeleteObject(callbackObject);
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
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        private static Task GameEvents_OnGameLoadComplete()
        {
            LoadChampion();

            if (_currentChampion is not null)
            {
                Logger.Log($"Initialize Menu [{_currentChampion?.GetType().Name}]");
                _currentChampion?.InitializeMenu();
            }

            MenuManager.AddTab(new Tab("SIXAIO - Summoners"));
            MenuManager.AddTab(new Tab("SIXAIO - Items"));
            MenuManager.AddTab(new Tab("SIXAIO - Utilities"));

            CoreEvents.OnCoreRender += CoreEvents_OnCoreRender;
            CoreEvents.OnCoreMainTick += CoreEvents_OnCoreMainTick;
            CoreEvents.OnCoreMainInputBeforeBasicAttackAsync += CoreEvents_OnCoreMainInputBeforeBasicAttackAsync;
            CoreEvents.OnCoreMainInputAsync += CoreEvents_OnCoreMainInputAsync;
            CoreEvents.OnCoreHarassInputAsync += CoreEvents_OnCoreHarassInputAsync;
            CoreEvents.OnCoreLaneclearInputAsync += CoreEvents_OnCoreLaneclearInputAsync;
            CoreEvents.OnCoreLasthitInputAsync += CoreEvents_OnCoreLasthitInputAsync;            
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

        private static Task CoreEvents_OnCoreMainInputBeforeBasicAttackAsync()
        {
            try
            {
                _currentChampion?.OnCoreMainInputBeforeBasicAttack();
            }
            catch (Exception)
            {
            }

            return Task.CompletedTask;
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
            try
            {
                _currentChampion?.OnCoreRender();
            }
            catch (Exception)
            {
            }
        }
    }
}
