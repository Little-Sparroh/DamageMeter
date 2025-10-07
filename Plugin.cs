using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Pigeon.Movement;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class DPSMeter : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.damagemeter";
    public const string PluginName = "DamageMeter";
    public const string PluginVersion = "1.0.1";

    private Harmony harmony;
    public static Queue<(float time, float damage)> damageQueue = new Queue<(float, float)>();
    private static float currentDPS = 0f;
    private bool uiVisible = true;

    private ConfigEntry<float> dpsWindowSeconds;

    private Text dpsText;
    private Image backgroundImage;
    private Canvas uiCanvas;

    private void Awake()
    {
        dpsWindowSeconds = Config.Bind("General", "DPSWindowSeconds", 5f, "Time window (in seconds) for calculating DPS. Higher values smooth out spikes.");

        var harmony = new Harmony(PluginGUID);
        harmony.PatchAll(typeof(DPSPatches));
        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Start()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        if (uiCanvas != null) return;

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

        GameObject bgGO = new GameObject("DPSBg");
        bgGO.transform.SetParent(canvasGO.transform, false);
        backgroundImage = bgGO.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.6f);
        backgroundImage.raycastTarget = false;
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.2f, 1f);
        bgRect.anchorMax = new Vector2(0.2f, 1f);
        bgRect.anchoredPosition = new Vector2(0f, -15f);
        bgRect.sizeDelta = new Vector2(150f, 25f);

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
        dpsText.supportRichText = true;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(1f, 1f);
        textRect.offsetMax = new Vector2(-1f, -1f);

        DontDestroyOnLoad(canvasGO);
    }

    private void Update()
    {
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

        while (damageQueue.Count > 0 && damageQueue.Peek().time < now - window)
        {
            damageQueue.Dequeue();
        }

        float totalDamage = 0f;
        foreach (var entry in damageQueue)
        {
            totalDamage += entry.damage;
        }

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
        if (data.damageData.damage > 0f)
        {
            TargetType type = data.target.Type;
            if ((type & TargetType.Enemy) != 0 || (type & TargetType.Player) != 0)
            {
                DPSMeter.damageQueue.Enqueue((Time.time, data.damageData.damage));
            }
        }
    }
}