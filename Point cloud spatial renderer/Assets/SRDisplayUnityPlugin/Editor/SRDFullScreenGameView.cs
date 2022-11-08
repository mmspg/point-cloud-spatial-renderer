/*
 * Copyright 2019,2020,2021 Sony Corporation
 */

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

using SRD.Core;
using SRD.Utils;
using SRD.Editor.AsssemblyWrapper;

namespace SRD.Editor
{
    internal class SRDFullScreenGameView
    {
        const string FullScreenMenuPath = "SpatialRealityDisplay/SRDisplay GameView (Full Screen)";
        const string SRDGameViewName = "SRD Game View";
        const string TemporalyGameViewName = "Temporary Game View";
        const string ForceCloseGameViewMessage = "Multiple GameViews cannot be open at the same time in Spatial Reality Display. Force closes the GameView tabs.";

        private static EditorApplication.CallbackFunction OnPostClosingTempGameView;

        private static IEnumerable<EditorWindow> EnumGameViews()
        {
            return GameView.GetGameViews().AsEnumerable();
        }

        private static IEnumerable<EditorWindow> EnumSRDGameViews()
        {
            return EnumGameViews().Where(w => w.name == SRDGameViewName);
        }

        private static IEnumerable<EditorWindow> EnumUnityGameViews()
        {
            return EnumGameViews().Where(w => w.name != SRDGameViewName);
        }

        private static void TakeOneUnityGameView()
        {
            var unityGameViews = EnumUnityGameViews()
                                 .Reverse()
                                 .Skip(1);

            foreach (var view in unityGameViews)
            {
                Debug.Log(ForceCloseGameViewMessage);
                view.Close();
            }
        }

        private static void CloseAllUnityGameView()
        {
            var unityGameViews = EnumUnityGameViews();

            foreach (var view in unityGameViews)
            {
                Debug.Log(ForceCloseGameViewMessage);
                view.Close();
            }
        }

        private static void CloseAllSRDGameView()
        {
            var srdGameViews = EnumSRDGameViews();

            foreach (var view in srdGameViews)
            {
                view.Close();
            }
        }

        private static void HideToolbarOfAllSRDGameView()
        {
            var srdGameViews = EnumSRDGameViews();

            foreach (var view in srdGameViews)
            {
                var gameView = new GameView(view);
                gameView.showToolbar = false;
            }
        }

#if UNITY_EDITOR_WIN
        [MenuItem(FullScreenMenuPath + " _F11", false, 2001)]
#endif
        public static void ExecuteFullScreen()
        {
            if(EditorApplication.isPlaying)
            {
                Debug.Log("SRDisplay GameView cannot be changed in Play Mode");
                return;
            }

            if (Menu.GetChecked(FullScreenMenuPath))
            {
                CloseAllSRDGameView();
                Menu.SetChecked(FullScreenMenuPath, false);
            }
            else
            {
                // check whether SDK is available or not.
                SrdXrDeviceInfo[] devices = { new SrdXrDeviceInfo(), };
                if (SRDCorePlugin.EnumerateDevices(devices, 1) == SrdXrResult.ERROR_SYSTEM_INVALID)
                {
                    SRDCorePlugin.ShowMessageBox("Confirm", SRDHelper.SRDMessages.DLLNotFoundError,
                                                 Debug.LogWarning);
                    return;
                }
                if(!SRDSettings.LoadScreenRect())
                {
                    SRDCorePlugin.ShowMessageBox("Confirm", SRDHelper.SRDMessages.DisplayConnectionError,
                                                 Debug.LogWarning);
                    return;
                }
                if(Prepare())
                {
                    CloseAllUnityGameView();
                    SetupGameView();
                }
                else
                {
                    OnPostClosingTempGameView += SetupGameViewAfterCloseTempGameView;
                }
                Menu.SetChecked(FullScreenMenuPath, true);
            }
        }

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += () =>
                {
                    SRDSettings.LoadScreenRect();

                    Prepare();
                };
            }

            EditorApplication.update += CloseUnityGameView;

            // GameView's showToolbar flag is restored to true when the script is recompiled, so set false again. 
            EditorApplication.delayCall += () =>
            {
                HideToolbarOfAllSRDGameView();
            };
        }

        private static bool Prepare()
        {
            var srdScreenRect = SRDSettings.DeviceInfo.ScreenRect;

            // Set the SRD screen size to project settings
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = srdScreenRect.Width;
            PlayerSettings.defaultScreenHeight = srdScreenRect.Height;

            bool ready = GameViewSizeList.IsReadyDestinationSize(srdScreenRect.Resolution);
            if (!ready)
            {
                // SRD Screen Size is added to GameViewSizes List by creating temporary GameView with Toolbar. 
                var gameView = new GameView(TemporalyGameViewName);
                gameView.position = Rect.zero;
                gameView.ShowWithPopupMenu();
                EditorApplication.update += CloseTemporaryGameView;
            }
            return ready;
        }

        private static void CloseUnityGameView()
        {
            // normal gameviews only
            if(!Menu.GetChecked(FullScreenMenuPath))
            {
                if(EditorApplication.isPlaying)
                {
                    TakeOneUnityGameView();
                }
            }
            // SRD GameView opened
            else
            {
                CloseAllUnityGameView();
            }
        }

        private static void CloseTemporaryGameView()
        {
            // Close Temporary GameView that finished the task of updating GameViewSizes.
            var tmpGameViews = EnumGameViews().Where(w => w.name == TemporalyGameViewName);
            foreach (var view in tmpGameViews)
            {
                view.Close();
            }

            var destinationSize = SRDSettings.DeviceInfo.ScreenRect.Resolution;
            if(GameViewSizeList.IsReadyDestinationSize(destinationSize))
            {
            }
            else
            {
                Debug.LogWarning("Fail to create destination size GameView. If you have a wrong size of SRDisplayGameView, please re-open SRDisplayGameView.");
            }

            EditorApplication.update -= CloseTemporaryGameView;
            if(OnPostClosingTempGameView != null)
            {
                OnPostClosingTempGameView.Invoke();
            }
        }

        private static void SetupGameView()
        {
            var gameView = new GameView(SRDGameViewName);
            gameView.scale = 1.0f;
            gameView.targetDisplay = 0;
            gameView.noCameraWarning = false;
            gameView.showToolbar = false;
            var srdScreenRect = SRDSettings.DeviceInfo.ScreenRect;
            gameView.rectangle = new Rect(srdScreenRect.Position, srdScreenRect.Resolution);
            gameView.ShowWithPopupMenu();
        }

        private static void SetupGameViewAfterCloseTempGameView()
        {
            CloseAllUnityGameView();
            SetupGameView();
            OnPostClosingTempGameView -= SetupGameViewAfterCloseTempGameView;
        }
    }
}
