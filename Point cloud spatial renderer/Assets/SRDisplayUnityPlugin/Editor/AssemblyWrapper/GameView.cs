/*
 * Copyright 2019,2020,2021 Sony Corporation
 */


using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace SRD.Editor.AsssemblyWrapper
{
    internal class GameViewSizeList
    {
        public static bool IsReadyDestinationSize(Vector2 destinationSize)
        {
            return FindDestinationIndex(destinationSize) != -1;
        }

        public static int FindDestinationIndex(Vector2 destinationSize)
        {
            var sizes = GetSizes();
            return sizes.FindIndex(size => size.width == destinationSize.x && size.height == destinationSize.y);
        }

        private static List<GameViewSnapshot.Size> GetSizes()
        {
            var source = typeof(UnityEditor.Editor).Assembly;
            // class GameViewSizes : ScriptableSingleton<GameViewSizes>
            var gameViewSizesType = source.GetType("UnityEditor.GameViewSizes");
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
            var gameViewSizes = singletonType.GetProperty("instance").GetValue(null);

            var currentGroupType = (GameViewSizeGroupType)gameViewSizes.GetType()
                                   .GetProperty("currentGroupType")
                                   .GetValue(gameViewSizes);
            var sizeGroup = gameViewSizes.GetType()
                            .GetMethod("GetGroup")
                            .Invoke(gameViewSizes, new object[] { (int)currentGroupType });
            var totalCount = (int)sizeGroup.GetType()
                             .GetMethod("GetTotalCount")
                             .Invoke(sizeGroup, new object[] { });

            var displayTexts = sizeGroup.GetType()
                               .GetMethod("GetDisplayTexts")
                               .Invoke(sizeGroup, null) as string[];
            Debug.Assert(totalCount == displayTexts.Length);
            var sizes = Enumerable.Range(0, totalCount)
                        .Select(i => ToGameViewSize(sizeGroup, i, displayTexts[i]))
                        .ToList();

            return sizes;
        }

        private static GameViewSnapshot.Size ToGameViewSize(object sizeGroup, int index, string name)
        {
            var gameViewSize = sizeGroup.GetType()
                               .GetMethod("GetGameViewSize")
                               .Invoke(sizeGroup, new object[] { index });
            var width = (int)gameViewSize.GetType()
                        .GetProperty("width")
                        .GetValue(gameViewSize);
            var height = (int)gameViewSize.GetType()
                         .GetProperty("height")
                         .GetValue(gameViewSize);
            return new GameViewSnapshot.Size(width, height, name);
        }
    }

    internal class GameView
    {
        const BindingFlags nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        //consty BindingFlags publicStatic = BindingFlags.Static | BindingFlags.Public;
#if UNITY_2019_3_OR_NEWER
        const BindingFlags publicInstance = BindingFlags.Instance | BindingFlags.Public;
        const BindingFlags nonPublicStatic = BindingFlags.Static | BindingFlags.NonPublic;
#endif

        static readonly Type gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
        static readonly PropertyInfo showToolbarProperty = gameViewType.GetProperty("showToolbar", nonPublicInstance);
        static readonly PropertyInfo viewInWindowProperty = gameViewType.GetProperty("viewInWindow", nonPublicInstance);

        private EditorWindow gameView;

        // keep values because assembly refererence values are different befor Apply
        private Rect applyRectangle;

        public static EditorWindow[] GetGameViews()
        {
            return Resources.FindObjectsOfTypeAll(gameViewType) as EditorWindow[];
        }

        public GameView(EditorWindow gameView)
        {
            this.gameView = gameView;
            this.applyRectangle = Rect.zero;
        }

        public GameView()
            : this(ScriptableObject.CreateInstance(gameViewType) as EditorWindow)
        {
        }

        public GameView(string gameViewName)
            : this()
        {
            this.gameView.name = gameViewName;
        }

        private int windowToolbarHeight
        {
            get
            {
                var source = typeof(UnityEditor.Editor).Assembly;
                var editorGUI = source.GetType("UnityEditor.EditorGUI");
#if UNITY_2019_3_OR_NEWER
                var windowToolbarHeightObj = editorGUI.GetField("kWindowToolbarHeight", nonPublicStatic).GetValue(source);
                var windowToolbarHeightObjType = source.GetType("UnityEditor.StyleSheets.SVC`1[System.Single]");
                var windowToolbarHeight = (float)(windowToolbarHeightObjType.GetProperty("value", publicInstance).GetValue(windowToolbarHeightObj));
                return (int)windowToolbarHeight;
#else
                return (int)editorGUI.GetField("kWindowToolbarHeight", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
#endif
            }
        }

        private int selectedSizeIndex
        {
            get
            {
                return (int)gameViewType
                       .GetProperty("selectedSizeIndex", nonPublicInstance)
                       .GetValue(gameView);
            }
            set
            {
                gameViewType
                .GetProperty("selectedSizeIndex", nonPublicInstance)
                .SetValue(gameView, value);
            }
        }

        public int targetDisplay
        {
            set
            {
#if UNITY_2019_3_OR_NEWER
                gameViewType
                .GetProperty("targetDisplay", nonPublicInstance)
                .SetValue(gameView, value);
#else
                gameViewType
                .GetField("m_TargetDisplay", nonPublicInstance)
                .SetValue(gameView, value);
#endif
            }
        }

        public float scale
        {
            set
            {
                var zoomArea = gameViewType
                               .GetField("m_ZoomArea", nonPublicInstance)
                               .GetValue(gameView);
                zoomArea.GetType()
                .GetField("m_Scale", nonPublicInstance)
                .SetValue(zoomArea, new Vector2(value, value));
            }
        }

        public bool noCameraWarning
        {
            set
            {
                gameViewType
                .GetField("m_NoCameraWarning", nonPublicInstance)
                .SetValue(gameView, value);
            }
        }

        public bool showToolbar
        {
            // showToolbar property exists since 2019.3.0
            get
            {
                return (showToolbarProperty != null) ? (bool)showToolbarProperty.GetValue(gameView) : true;
            }
            set
            {
                showToolbarProperty?.SetValue(gameView, value);
            }
        }

        public Rect viewInWindow
        {
            get
            {
                return (Rect)viewInWindowProperty.GetValue(gameView);
            }
        }

        public Rect position
        {
            get
            {
                return gameView.position;
            }
            set
            {
                gameView.position = value;
            }
        }

        public Rect rectangle
        {
            set
            {
                applyRectangle = value;
            }
        }

        public void ShowWithPopupMenu()
        {
            var source = typeof(UnityEditor.Editor).Assembly;
            var showModeType = source.GetType("UnityEditor.ShowMode");
            var popupMenu = Enum.ToObject(showModeType, 1);
            var showWithMode = gameViewType.GetMethod("ShowWithMode", nonPublicInstance);
            showWithMode.Invoke(gameView, new[] { popupMenu });

            if (this.applyRectangle != Rect.zero)
            {
                var currWindow = this.gameView.position;
                var currViewInWindow = this.viewInWindow;
                var newWindow = this.applyRectangle;
                newWindow.position -= currViewInWindow.position;
                newWindow.size += currWindow.size - currViewInWindow.size;
                this.gameView.maxSize = newWindow.size;
                this.gameView.minSize = newWindow.size;
                this.gameView.position = newWindow;

                var index = GameViewSizeList.FindDestinationIndex(this.applyRectangle.size);
                if (index >= 0)
                {
                    this.selectedSizeIndex = index;
                }
                else
                {
                    //Debug.Log("Show temporary game view for updating GameViewSizes");
                }
            }
        }
    }
}
