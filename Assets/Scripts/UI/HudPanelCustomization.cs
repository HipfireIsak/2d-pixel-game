using System;
using System.Collections.Generic;
using UnityEngine;

namespace AetherEcho.UI
{
    public static class HudPanelCustomization
    {
        public struct MenuItem
        {
            public string Label;
            public Action OnClick;

            public MenuItem(string label, Action onClick)
            {
                Label = label;
                OnClick = onClick;
            }
        }

        private static readonly List<MenuItem> ActiveItems = new List<MenuItem>();
        private static Rect menuRect;
        private static bool menuOpen;
        private static Vector2 dragStartMouse;
        private static Vector2 dragStartPanel;
        private static int activeDragControlId;

        public static bool IsMenuOpen => menuOpen;

        public static void TryOpenContextMenu(Rect panelRect, MenuItem[] items)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 1 || !panelRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            OpenMenu(currentEvent.mousePosition, items);
            currentEvent.Use();
        }

        public static void OpenMenu(Vector2 position, MenuItem[] items)
        {
            ActiveItems.Clear();
            ActiveItems.AddRange(items);

            const float itemHeight = 22f;
            const float menuWidth = 190f;
            float menuHeight = ActiveItems.Count * itemHeight + 8f;
            float x = Mathf.Clamp(position.x, 4f, Screen.width - menuWidth - 4f);
            float y = Mathf.Clamp(position.y, 4f, Screen.height - menuHeight - 4f);
            menuRect = new Rect(x, y, menuWidth, menuHeight);
            menuOpen = true;
        }

        public static void DrawContextMenu()
        {
            if (!menuOpen)
            {
                return;
            }

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !menuRect.Contains(currentEvent.mousePosition))
            {
                menuOpen = false;
                return;
            }

            GUI.Box(menuRect, string.Empty);
            float y = menuRect.y + 4f;
            for (int i = 0; i < ActiveItems.Count; i++)
            {
                MenuItem item = ActiveItems[i];
                var itemRect = new Rect(menuRect.x + 4f, y, menuRect.width - 8f, 20f);
                if (GUI.Button(itemRect, item.Label))
                {
                    item.OnClick?.Invoke();
                    menuOpen = false;
                }

                y += 22f;
            }
        }

        public static void HandleDrag(ref Rect panelRect, bool unlocked, int controlId)
        {
            if (!unlocked)
            {
                if (activeDragControlId == controlId)
                {
                    activeDragControlId = 0;
                }

                return;
            }

            Event currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0 && panelRect.Contains(currentEvent.mousePosition))
                    {
                        activeDragControlId = controlId;
                        dragStartMouse = currentEvent.mousePosition;
                        dragStartPanel = panelRect.position;
                        currentEvent.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (activeDragControlId == controlId)
                    {
                        panelRect.position = dragStartPanel + (currentEvent.mousePosition - dragStartMouse);
                        currentEvent.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (activeDragControlId == controlId)
                    {
                        activeDragControlId = 0;
                        currentEvent.Use();
                    }

                    break;
            }
        }
    }
}
