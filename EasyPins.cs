using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Splatform;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Windows;
using static Minimap;

namespace RCGaming
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class EasyPins : BaseUnityPlugin
    {
        public const string PluginGUID = "RCGaming.EasyPins";
        public const string PluginName = "EasyPins";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<KeyCode> configMapKey;
        private static ConfigEntry<bool> modEnabled;
        public static string originalHoverText = "";
        private GameObject lastHoverObject;

        private void Awake()
        {
            configMapKey = Config.Bind("Hotkeys", "HotKey", KeyCode.F8, "Key to press to map the hovered pickable");
            modEnabled = Config.Bind("General", "ModEnabled", true, "Enable or disable the mod.");
            if (modEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(EasyPins).Assembly, PluginGUID);
            }
        }

        private void Update()
        {
            if (modEnabled.Value)
            {
                // Always check for Hotkey press, regardless of hover state
                if (ZInput.GetKeyDown(configMapKey.Value)) // Use GetKeyDown to avoid repeated triggers
                {
                    // Only proceed if hovering over a valid pickable object
                    Player player = Player.m_localPlayer;
                    if (player == null) return;

                    GameObject hoverObject = player.GetHoverObject();
                    if (hoverObject == null) return;

                    Pickable pickable = hoverObject.GetComponentInParent<Pickable>();
                    if (pickable == null) return;

                    // If we have a valid pickable, add it to the map
                    AddPickableToMap(pickable, originalHoverText);

                }

                // Rest of the hover text logic
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer == null) return;

                GameObject currentHover = localPlayer.GetHoverObject();
                if (currentHover == null)
                {
                    lastHoverObject = null;
                    return;
                }

                Pickable currentPickable = currentHover.GetComponentInParent<Pickable>();
                if (currentPickable == null) return;

                if (currentHover != lastHoverObject)
                {
                    originalHoverText = GetOriginalHoverText(currentPickable);
                    lastHoverObject = currentHover;
                }
            }
        }

        private bool pinExistsAtPosition(Vector3 position, float maxDistance = 1.0f)
        {
            List<Minimap.PinData> pins = getMinimapPins();
            foreach (Minimap.PinData pin in pins)
            {
                if (Vector3.Distance(pin.m_pos, position) < maxDistance)
                {
                    return true; // Pin already exists nearby
                }
            }
            return false;
        }

        private List<Minimap.PinData> getMinimapPins()
        {
            FieldInfo pinsField = typeof(Minimap).GetField("m_pins", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pinsField != null)
            {
                return (List<Minimap.PinData>)pinsField.GetValue(Minimap.instance);
            }
            return new List<Minimap.PinData>(); // Fallback: empty list
        }

        private string GetOriginalHoverText(Pickable pickable)
        {
            // Check for custom hover text
            var hoverText = pickable.GetHoverText();
            if (!string.IsNullOrEmpty(hoverText))
            {
                var split = hoverText.Split('\n');
                if (split.Length > 1)
                {
                    return split[0];
                }
            }
            return "Resource";
        }

        private void AddPickableToMap(Pickable pickable, string pinName)
        {
            Minimap minimap = Minimap.instance;
            if (minimap == null || pickable == null) return;

            Vector3 position = pickable.transform.position;

            if (pinExistsAtPosition(position))
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, "A pin already exists here!");
                return;
            }

            //Player.m_localPlayer.GetPlayerID()

            PinData pin = minimap.AddPin(position, Minimap.PinType.Icon3, pinName, true, false);

            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, $"Pin `{pinName}` added!");

        }
    }
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    public static class PickableGetHoverTextPatch
    {
        public static void Postfix(Pickable __instance, ref string __result)
        {
            // Modify the hover text here
            __result = __result + $"\n[<color=yellow>{EasyPins.configMapKey.Value}</color>] Map";
        }
    }

}
