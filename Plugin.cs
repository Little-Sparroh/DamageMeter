using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Pigeon.Movement; // Assuming this is needed for Player access, as in the speedometer

[BepInPlugin("com.yourname.mycopunk.damagemeter", "DamageMeter", "1.0.0")]
[MycoMod(null, ModFlags.IsClientSide)] // Added to match the speedometer mod
public class DPSMeter : BaseUnityPlugin
{
    private Harmony harmony;
    public static Queue<(float time, float damage)> damageQueue = new Queue<(float, float)>();
    private static float currentDPS = 0f;
    private bool uiVisible = true;

    // Configurable settings
    private ConfigEntry<float> dpsWindowSeconds;

    // UI elements
    private Text dpsText;
    private Image backgroundImage;
    private Canvas uiCanvas;

    private void Awake()
    {
        // Load config
        dpsWindowSeconds = Config.Bind("General", "DPSWindowSeconds", 5f, "Time window (in seconds) for calculating DPS. Higher values smooth out spikes.");

        // Patch the game
        var harmony = new Harmony("com.yourname.mycopunk.damagemeter");
        harmony.PatchAll(typeof(DPSPatches));

        Logger.LogInfo($"{harmony.Id} loaded!");
    }

    private void Start()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        if (uiCanvas != null) return; // Avoid duplicates

        GameObject canvasGO = new GameObject("DPSMeterCanvas");
        uiCanvas = canvasGO.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 9999;
        uiCanvas.pixelPerfect = true;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.scaleFactor = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Background (semi-transparent black)
        GameObject bgGO = new GameObject("DPSBg");
        bgGO.transform.SetParent(canvasGO.transform, false);
        backgroundImage = bgGO.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.6f);
        backgroundImage.raycastTarget = false;
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.2f, 1f); // Top-center anchor (corrected from 0.11f in speedometer for true center)
        bgRect.anchorMax = new Vector2(0.2f, 1f);
        bgRect.anchoredPosition = new Vector2(0f, -15f);
        bgRect.sizeDelta = new Vector2(150f, 25f); // Matching speedometer size

        // Text
        GameObject textGO = new GameObject("DPSText");
        textGO.transform.SetParent(bgGO.transform, false);
        dpsText = textGO.AddComponent<Text>();
        dpsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        dpsText.text = "DPS: Loading...";
        dpsText.fontSize = 15;
        dpsText.color = Color.white;
        dpsText.alignment = TextAnchor.MiddleCenter;
        dpsText.raycastTarget = false;
        dpsText.verticalOverflow = VerticalWrapMode.Overflow;
        dpsText.supportRichText = true; // Explicitly enable rich text
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(1f, 1f); // Padding
        textRect.offsetMax = new Vector2(-1f, -1f);

        DontDestroyOnLoad(canvasGO);
    }

    private void Update()
    {
        // Toggle with F5 (using Input System to match speedometer)
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            uiVisible = !uiVisible;
            if (backgroundImage != null) backgroundImage.enabled = uiVisible;
            if (dpsText != null) dpsText.enabled = uiVisible;
        }

        if (!uiVisible || dpsText == null || Player.LocalPlayer == null)
        {
            if (dpsText != null && uiVisible) dpsText.text = "No Player";
            return;
        }

        float now = Time.time;
        float window = dpsWindowSeconds.Value;

        // Remove old damage events outside the window
        while (damageQueue.Count > 0 && damageQueue.Peek().time < now - window)
        {
            damageQueue.Dequeue();
        }

        // Calculate total damage in the window
        float totalDamage = 0f;
        foreach (var entry in damageQueue)
        {
            totalDamage += entry.damage;
        }

        // DPS = total damage / window size (or 0 if no damage)
        currentDPS = (damageQueue.Count > 0) ? (totalDamage / window) : 0f;

        dpsText.text = $"DPS: <color=#E3242B>{currentDPS:F2}</color>";
    }

    private void OnDestroy()
    {
        if (uiCanvas != null) Destroy(uiCanvas.gameObject);
    }
}

internal class DPSPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerData), "OnLocalPlayerDamageTarget")]
    private static void PostDamage(in DamageCallbackData data)
    {
        // Only track positive damage (ignore heals, zero-damage, etc.)
        if (data.damageData.damage > 0f)
        {
            // Optional: Filter to only enemy damage (based on the code snippet)
            TargetType type = data.target.Type;
            if ((type & TargetType.Enemy) != 0 || (type & TargetType.Player) != 0)
            {
                // Record the damage event with current time
                DPSMeter.damageQueue.Enqueue((Time.time, data.damageData.damage));
            }
        }
    }
}