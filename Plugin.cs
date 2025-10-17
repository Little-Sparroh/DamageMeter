using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Pigeon.Movement;
using TMPro;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class DPSMeter : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.damagemeter";
    public const string PluginName = "DamageMeter";
    public const string PluginVersion = "1.1.0";

    private Harmony harmony;
    public static Queue<(float time, float damage)> damageQueue = new Queue<(float, float)>();
    private bool uiVisible = true;

    private ConfigEntry<float> dpsWindowSeconds;

    private GameObject hudContainer;
    private TextMeshProUGUI totalDamageText;
    private TextMeshProUGUI fiveSecDamageText;
    private TextMeshProUGUI totalKillsText;
    private TextMeshProUGUI totalCoresText;

    public static float totalDamage = 0f;
    public static int totalKills = 0;
    public static int totalCoresKilled = 0;
    public static float missionStartTime = 0f;

    private void Awake()
    {
        dpsWindowSeconds = Config.Bind("General", "DPSWindowSeconds", 5f, "Time window (in seconds) for calculating DPS. Higher values smooth out spikes.");

        var harmony = new Harmony(PluginGUID);
        harmony.PatchAll(typeof(DPSPatches));
        harmony.PatchAll(typeof(MissionPatches));
        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Start()
    {
        missionStartTime = Time.time;
    }

    private void CreateHUD()
    {
        if (hudContainer != null) return;

        if (Player.LocalPlayer.PlayerLook == null || Player.LocalPlayer.PlayerLook.Reticle == null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        hudContainer = new GameObject("DamageMeterHUD");
        hudContainer.transform.SetParent(parent, false);

        var containerRect = hudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.2f, 0.9f);
        containerRect.anchorMax = new Vector2(0.2f, 0.9f);
        containerRect.anchoredPosition = new Vector2(0f, 0f);
        containerRect.sizeDelta = new Vector2(300f, 100f);

        var totalDamageGO = new GameObject("TotalDamageText");
        totalDamageGO.transform.SetParent(hudContainer.transform, false);
        totalDamageText = totalDamageGO.AddComponent<TextMeshProUGUI>();
        totalDamageText.fontSize = 18;
        totalDamageText.color = Color.white;
        totalDamageText.enableWordWrapping = false;
        totalDamageText.alignment = TextAlignmentOptions.Left;
        totalDamageText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var rect1 = totalDamageGO.GetComponent<RectTransform>();
        rect1.anchorMin = Vector2.zero;
        rect1.anchorMax = Vector2.one;
        rect1.anchoredPosition = new Vector2(0f, 0f);

        var fiveSecGO = new GameObject("FiveSecDamageText");
        fiveSecGO.transform.SetParent(hudContainer.transform, false);
        fiveSecDamageText = fiveSecGO.AddComponent<TextMeshProUGUI>();
        fiveSecDamageText.fontSize = 18;
        fiveSecDamageText.color = Color.white;
        fiveSecDamageText.enableWordWrapping = false;
        fiveSecDamageText.alignment = TextAlignmentOptions.Left;
        fiveSecDamageText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var rect2 = fiveSecGO.GetComponent<RectTransform>();
        rect2.anchorMin = Vector2.zero;
        rect2.anchorMax = Vector2.one;
        rect2.anchoredPosition = new Vector2(0f, -25f);

        var killsGO = new GameObject("TotalKillsText");
        killsGO.transform.SetParent(hudContainer.transform, false);
        totalKillsText = killsGO.AddComponent<TextMeshProUGUI>();
        totalKillsText.fontSize = 18;
        totalKillsText.color = Color.white;
        totalKillsText.enableWordWrapping = false;
        totalKillsText.alignment = TextAlignmentOptions.Left;
        totalKillsText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var rect3 = killsGO.GetComponent<RectTransform>();
        rect3.anchorMin = Vector2.zero;
        rect3.anchorMax = Vector2.one;
        rect3.anchoredPosition = new Vector2(0f, -50f);

        var coresGO = new GameObject("TotalCoresText");
        coresGO.transform.SetParent(hudContainer.transform, false);
        totalCoresText = coresGO.AddComponent<TextMeshProUGUI>();
        totalCoresText.fontSize = 18;
        totalCoresText.color = Color.white;
        totalCoresText.enableWordWrapping = false;
        totalCoresText.alignment = TextAlignmentOptions.Left;
        totalCoresText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var rect4 = coresGO.GetComponent<RectTransform>();
        rect4.anchorMin = Vector2.zero;
        rect4.anchorMax = Vector2.one;
        rect4.anchoredPosition = new Vector2(0f, -75f);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            uiVisible = !uiVisible;
            if (hudContainer != null)
            {
                hudContainer.SetActive(uiVisible);
            }
        }

        if (Player.LocalPlayer == null) return;

        if (hudContainer == null)
        {
            CreateHUD();
            return;
        }

        float now = Time.time;
        float window = dpsWindowSeconds.Value;

        while (damageQueue.Count > 0 && damageQueue.Peek().time < now - window)
        {
            damageQueue.Dequeue();
        }

        float recentDamage = 0f;
        foreach (var entry in damageQueue)
        {
            recentDamage += entry.damage;
        }

        float recentDPS = (damageQueue.Count > 0) ? (recentDamage / window) : 0f;

        float missionTime = Time.time - missionStartTime;
        float totalDPS = (missionTime > 0) ? (totalDamage / missionTime) : 0f;
        float killsPerSec = (missionTime > 0) ? ((float)totalKills / missionTime) : 0f;
        float coresPerSec = (missionTime > 0) ? ((float)totalCoresKilled / missionTime) : 0f;

        totalDamageText.text = $"Total Damage: <color=red>{totalDamage:F0}</color> (<color=red>{totalDPS:F1}</color>/s)";
        fiveSecDamageText.text = $"Last 5sec Damage: <color=red>{recentDamage:F0}</color> (<color=red>{recentDPS:F1}</color>/s)";
        totalKillsText.text = $"Targets Killed: <color=red>{totalKills}</color> (<color=red>{killsPerSec:F1}</color>/s)";
        totalCoresText.text = $"Cores Killed: <color=red>{totalCoresKilled}</color> (<color=red>{coresPerSec:F1}</color>/s)";
    }

    private void OnDestroy()
    {
        if (hudContainer != null) Destroy(hudContainer);
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
            DPSMeter.totalDamage += data.damageData.damage;

            TargetType type = data.target.Type;
            if ((type & TargetType.Enemy) != 0 || (type & TargetType.Player) != 0)
            {
                DPSMeter.damageQueue.Enqueue((Time.time, data.damageData.damage));
            }

            if (data.KilledTarget)
            {
                DPSMeter.totalKills++;
                if (typeof(EnemyCore).IsAssignableFrom(data.target.GetType()))
                {
                    DPSMeter.totalCoresKilled++;
                }
            }
        }
    }
}

internal class MissionPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MissionManager), "SpawnHUD")]
    private static void MissionStart()
    {
        DPSMeter.totalDamage = 0f;
        DPSMeter.totalKills = 0;
        DPSMeter.totalCoresKilled = 0;
        DPSMeter.damageQueue.Clear();
        DPSMeter.missionStartTime = Time.time;
    }
}
