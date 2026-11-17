using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.InputSystem;
using TMPro;
using ZenGardenVR.Core;
using ZenGardenVR.Character;
using ZenGardenVR.Interactions;
using ZenGardenVR.Missions;
using ZenGardenVR.UI;
using ZenGardenVR.Locomotion;

public static class ZenGardenBuilder
{
    [MenuItem("ZenGarden/Build Main VR Scene")]
    public static void BuildMainVRScene()
    {
        EnsureURPPipelineConfigured();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Iluminación y Cielo de Atardecer Cálido
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.95f, 0.78f, 0.65f);
        RenderSettings.ambientEquatorColor = new Color(0.68f, 0.55f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.28f, 0.22f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.88f, 0.76f, 0.68f);
        RenderSettings.fogDensity = 0.006f;

        // Sol Poniente
        var sunGo = new GameObject("Sunset Directional Light");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1.0f, 0.88f, 0.72f);
        sun.intensity = 1.4f;
        sun.shadows = LightShadows.Soft;
        sunGo.transform.rotation = Quaternion.Euler(26f, -38f, 0f);

        // Materiales PBR optimizados para Universal Render Pipeline (URP)
        var matGrass = GetOrCreateMaterial("Assets/Art/Materials/ZenMossGrass.mat", new Color(0.26f, 0.42f, 0.20f), 0.1f);
        var matSand = GetOrCreateMaterial("Assets/Art/Materials/ZenSandGravel.mat", new Color(0.82f, 0.78f, 0.68f), 0.15f);
        var matStone = GetOrCreateMaterial("Assets/Art/Materials/ZenPebbleGrey.mat", new Color(0.40f, 0.42f, 0.44f), 0.2f);
        var matTorii = GetOrCreateMaterial("Assets/Art/Materials/ZenToriiVermilion.mat", new Color(0.85f, 0.22f, 0.14f), 0.45f);
        var matWood = GetOrCreateMaterial("Assets/Art/Materials/ZenWoodDark.mat", new Color(0.32f, 0.20f, 0.12f), 0.25f);
        var matRoof = GetOrCreateMaterial("Assets/Art/Materials/ZenRoofTiles.mat", new Color(0.22f, 0.24f, 0.26f), 0.35f);
        var matLanternLit = GetOrCreateMaterial("Assets/Art/Materials/ZenLanternLit.mat", new Color(1.0f, 0.9f, 0.5f), 0.1f, 0f, new Color(2.5f, 1.8f, 0.6f));
        var matLanternUnlit = GetOrCreateMaterial("Assets/Art/Materials/ZenLanternUnlit.mat", new Color(0.35f, 0.35f, 0.32f), 0.1f);
        var matCherry = GetOrCreateMaterial("Assets/Art/Materials/ZenCherryBlossomPink.mat", new Color(0.98f, 0.72f, 0.80f), 0.3f);
        var matCrystal = GetOrCreateMaterial("Assets/Art/Materials/ZenMagicCrystalCyan.mat", new Color(0.2f, 0.85f, 0.95f), 0.85f, 0.1f, new Color(0.4f, 1.8f, 2.2f));
        var matWater = GetOrCreateMaterial("Assets/Art/Materials/ZenWaterClear.mat", new Color(0.35f, 0.65f, 0.85f, 0.65f), 0.95f, 0.1f, null, true);
        var matGuardian = GetOrCreateMaterial("Assets/Art/Materials/ZenGuardianFabric.mat", new Color(0.92f, 0.92f, 0.95f), 0.2f);
        var matGuardianGold = GetOrCreateMaterial("Assets/Art/Materials/ZenGuardianGold.mat", new Color(0.95f, 0.78f, 0.25f), 0.65f, 0.7f, new Color(1.2f, 0.9f, 0.3f));

        // Audio Clips
        var sndChime = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/chime.wav");
        var sndWater = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/water.wav");
        var sndLantern = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/lantern.wav");
        var sndStoneGrab = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/stone_grab.wav");
        var sndMissionDone = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/mission_done.wav");

        // 2. Infraestructura XR
        var xrManagerGo = new GameObject("XR Interaction Manager");
        var xrInteractionManager = xrManagerGo.AddComponent<XRInteractionManager>();

        var inputManagerGo = new GameObject("Input Action Manager");
        var inputActionManager = inputManagerGo.AddComponent<InputActionManager>();
        var defaultActionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/XRI Default Input Actions.inputactions");
        if (defaultActionsAsset != null)
        {
            var list = new List<InputActionAsset> { defaultActionsAsset };
            inputActionManager.actionAssets = list;
        }

        // XR Origin
        var xrOriginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
        GameObject xrOriginInstance = null;
        if (xrOriginPrefab != null)
        {
            xrOriginInstance = (GameObject)PrefabUtility.InstantiatePrefab(xrOriginPrefab);
            xrOriginInstance.name = "XR Origin (VR Rig)";
            xrOriginInstance.transform.position = new Vector3(0, 0, 0);
            xrOriginInstance.transform.rotation = Quaternion.identity;
        }

        // XR Device Simulator
        var simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.6.1/XR Device Simulator/XR Device Simulator.prefab");
        if (simPrefab != null)
        {
            var simInstance = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
            simInstance.name = "XR Device Simulator";
        }

        // Detector de Gestos
        var gestureGo = new GameObject("Head Gesture Detector");
        var gestureDetector = gestureGo.AddComponent<HeadGestureDetector>();

        // Managers
        var gameManagerGo = new GameObject("Game Manager");
        var gameManager = gameManagerGo.AddComponent<GameManager>();

        var missionManagerGo = new GameObject("Mission Manager");
        var missionManager = missionManagerGo.AddComponent<MissionManager>();
        var mmAudio = missionManagerGo.AddComponent<AudioSource>();
        mmAudio.playOnAwake = false;
        var serializedMM = new SerializedObject(missionManager);
        serializedMM.FindProperty("audioSource").objectReferenceValue = mmAudio;
        serializedMM.FindProperty("stepCompleteSound").objectReferenceValue = sndChime;
        serializedMM.FindProperty("missionCompleteSound").objectReferenceValue = sndMissionDone;
        serializedMM.ApplyModifiedProperties();

        // 3. Interfaz VR (HUD Cómodo en World Space)
        var uiRoot = new GameObject("VR HUD & Canvas");
        uiRoot.transform.position = new Vector3(0, 1.05f, 3.2f);
        var canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = uiRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1.8f, 1.1f);
        rt.localScale = Vector3.one * 0.0016f;

        var vruiManager = uiRoot.AddComponent<VRUIManager>();
        var soUIInit = new SerializedObject(vruiManager);
        soUIInit.FindProperty("followDistance").floatValue = 3.2f;
        soUIInit.FindProperty("followHeight").floatValue = -0.45f;
        soUIInit.ApplyModifiedProperties();

        var panelGo = new GameObject("HUD Panel");
        panelGo.transform.SetParent(uiRoot.transform, false);
        var pImg = panelGo.AddComponent<UnityEngine.UI.Image>();
        pImg.color = new Color(0.06f, 0.08f, 0.10f, 0.82f);
        var pRt = panelGo.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;

        var missionPanelGo = new GameObject("Mission Container");
        missionPanelGo.transform.SetParent(panelGo.transform, false);
        var missionTitle = CreateTMPText("MissionTitle", missionPanelGo.transform, new Vector2(0, 220), new Vector2(800, 50), 32, "Misión: Secretos del Jardín Zen", Color.white, true);
        var missionProgress = CreateTMPText("MissionProgress", missionPanelGo.transform, new Vector2(0, 175), new Vector2(800, 40), 24, "Progreso Armónico: 0 / 5", new Color(0.9f, 0.8f, 0.4f), false);
        var missionSteps = CreateTMPText("MissionSteps", missionPanelGo.transform, new Vector2(-150, 40), new Vector2(480, 200), 22, "Cargando pasos...", Color.white, false);
        missionSteps.alignment = TextAlignmentOptions.TopLeft;

        var inspectPanelGo = new GameObject("Inspection Container");
        inspectPanelGo.transform.SetParent(panelGo.transform, false);
        var inspectTitle = CreateTMPText("InspectTitle", inspectPanelGo.transform, new Vector2(250, 110), new Vector2(360, 60), 24, "Examinar", new Color(0.5f, 0.9f, 1f), true);
        var inspectDesc = CreateTMPText("InspectDesc", inspectPanelGo.transform, new Vector2(250, 40), new Vector2(360, 80), 18, "", Color.white, false);
        var inspectAction = CreateTMPText("InspectAction", inspectPanelGo.transform, new Vector2(250, -30), new Vector2(360, 50), 18, "", new Color(1f, 0.9f, 0.3f), false);

        var dialPanelGo = new GameObject("Dialogue Container");
        dialPanelGo.transform.SetParent(panelGo.transform, false);
        var speakerText = CreateTMPText("SpeakerName", dialPanelGo.transform, new Vector2(0, -110), new Vector2(800, 40), 22, "Guardián del Jardín", new Color(1f, 0.75f, 0.3f), true);
        var dialogueText = CreateTMPText("DialogueContent", dialPanelGo.transform, new Vector2(0, -165), new Vector2(800, 70), 20, "", Color.white, false);

        var notifPanelGo = new GameObject("Notification Container");
        notifPanelGo.transform.SetParent(panelGo.transform, false);
        var notifText = CreateTMPText("NotificationText", notifPanelGo.transform, new Vector2(0, -225), new Vector2(800, 45), 22, "", new Color(0.4f, 1f, 0.5f), true);

        var soUI = new SerializedObject(vruiManager);
        soUI.FindProperty("worldCanvas").objectReferenceValue = canvas;
        soUI.FindProperty("inspectionPanel").objectReferenceValue = inspectPanelGo;
        soUI.FindProperty("inspectionTitleText").objectReferenceValue = inspectTitle;
        soUI.FindProperty("inspectionDescText").objectReferenceValue = inspectDesc;
        soUI.FindProperty("inspectionActionText").objectReferenceValue = inspectAction;
        soUI.FindProperty("missionPanel").objectReferenceValue = missionPanelGo;
        soUI.FindProperty("missionTitleText").objectReferenceValue = missionTitle;
        soUI.FindProperty("missionProgressText").objectReferenceValue = missionProgress;
        soUI.FindProperty("missionStepsText").objectReferenceValue = missionSteps;
        soUI.FindProperty("dialoguePanel").objectReferenceValue = dialPanelGo;
        soUI.FindProperty("speakerNameText").objectReferenceValue = speakerText;
        soUI.FindProperty("dialogueContentText").objectReferenceValue = dialogueText;
        soUI.FindProperty("notificationPanel").objectReferenceValue = notifPanelGo;
        soUI.FindProperty("notificationText").objectReferenceValue = notifText;
        soUI.ApplyModifiedProperties();

        // 4. ENTORNO GENERAL Y 5 ZONAS
        var envRoot = new GameObject("=== ZEN GARDEN ENVIRONMENT ===");

        // Suelo principal
        var masterGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
        masterGround.name = "Master Garden Ground (Moss & Earth)";
        masterGround.transform.SetParent(envRoot.transform);
        masterGround.transform.position = new Vector3(0, -0.5f, 16f);
        masterGround.transform.localScale = new Vector3(56f, 1.0f, 65f);
        if (matGrass != null) masterGround.GetComponent<MeshRenderer>().material = matGrass;
        masterGround.AddComponent<TeleportationArea>();

        // ZONA 1: ENTRADA JAPONESA (Z: 0 to 8)
        var zone1 = new GameObject("Zona 1: Entrada Japonesa (Torii & Stone Path)");
        zone1.transform.SetParent(envRoot.transform);
        CreateToriiGate(zone1.transform, new Vector3(0, 0, 3f), matTorii, matRoof);
        CreateTraditionalSignboard(zone1.transform, new Vector3(-2.8f, 0, 0.8f), matWood, matRoof, matSand);
        CreateStonePath(zone1.transform, new Vector3(0, 0.02f, 0), 9, matStone);
        CreateArtisticJapaneseLantern(zone1.transform, new Vector3(3.2f, 0, 2.5f), matStone, matRoof, matLanternLit, matLanternUnlit, sndLantern, true);
        CreateArtisticJapaneseLantern(zone1.transform, new Vector3(-3.2f, 0, 2.5f), matStone, matRoof, matLanternLit, matLanternUnlit, sndLantern, true);
        CreateCherryBlossomTree(zone1.transform, new Vector3(-7.0f, 0, 3.5f), matWood, matCherry, 1.15f);
        CreateCherryBlossomTree(zone1.transform, new Vector3(7.0f, 0, 3.5f), matWood, matCherry, 1.10f);

        // ZONA 2: JARDÍN DE CONTEMPLACIÓN (X: -14, Z: 16)
        var zone2 = new GameObject("Zona 2: Jardín de Contemplación (Karesansui & Bonsai)");
        zone2.transform.SetParent(envRoot.transform);
        zone2.transform.position = new Vector3(-14f, 0, 16f);
        CreateRakedSandGarden(zone2.transform, Vector3.zero, matSand, matWood, matStone);
        CreateBonsaiAltar(zone2.transform, new Vector3(0, 0, 0), matWood, matCherry, sndChime);
        CreateArtisticJapaneseLantern(zone2.transform, new Vector3(5.5f, 0, -5.5f), matStone, matRoof, matLanternLit, matLanternUnlit, sndLantern, true);
        CreateCherryBlossomTree(zone2.transform, new Vector3(-6.5f, 0, 5.0f), matWood, matCherry, 1.25f);
        CreateCherryBlossomTree(zone2.transform, new Vector3(-6.5f, 0, -4.5f), matWood, matCherry, 1.15f);
        CreateBambooGrove(zone2.transform, new Vector3(6.5f, 0, 4.0f), matGrass);

        // ZONA 3: JARDÍN MÁGICO (X: 14, Z: 16)
        var zone3 = new GameObject("Zona 3: Jardín Mágico (Spiritual Stones & Runes)");
        zone3.transform.SetParent(envRoot.transform);
        zone3.transform.position = new Vector3(14f, 0, 16f);
        CreateMagicGarden(zone3.transform, Vector3.zero, matStone, matCrystal, sndStoneGrab, sndChime);
        CreateArtisticJapaneseLantern(zone3.transform, new Vector3(-5.5f, 0, -5.5f), matStone, matRoof, matLanternLit, matLanternUnlit, sndLantern, true);
        CreateCherryBlossomTree(zone3.transform, new Vector3(6.5f, 0, 5.0f), matWood, matCherry, 1.3f);
        CreateCherryBlossomTree(zone3.transform, new Vector3(6.5f, 0, -4.5f), matWood, matCherry, 1.15f);

        // ZONA 4: RÍO Y PUENTE ARQUEADO TRADICIONAL (X: 0, Z: 20)
        var zone4 = new GameObject("Zona 4: Río Pintoresco y Puente Japonés Arqueado");
        zone4.transform.SetParent(envRoot.transform);
        zone4.transform.position = new Vector3(0, 0, 20f);
        CreateScenicRiverAndBridge(zone4.transform, Vector3.zero, matWater, matStone, matTorii, matWood, sndWater, matCherry);
        CreateArtisticJapaneseLantern(zone4.transform, new Vector3(4.0f, 0, -6.5f), matStone, matRoof, matLanternLit, matLanternUnlit, sndLantern, true);
        CreateArtisticJapaneseLantern(zone4.transform, new Vector3(-4.0f, 0, -6.5f), matStone, matRoof, matLanternLit, matLanternUnlit, sndLantern, true);
        CreateCherryBlossomTree(zone4.transform, new Vector3(-9.0f, 0, -1.5f), matWood, matCherry, 1.25f);
        CreateCherryBlossomTree(zone4.transform, new Vector3(9.0f, 0, -1.5f), matWood, matCherry, 1.25f);

        // ZONA 5: SANTUARIO Y GUARDIÁN HUMANOIDE (X: 0, Z: 34)
        var zone5 = new GameObject("Zona 5: Santuario Shinto & Guardián");
        zone5.transform.SetParent(envRoot.transform);
        zone5.transform.position = new Vector3(0, 0, 34f);
        CreateShintoSanctuary(zone5.transform, Vector3.zero, matTorii, matWood, matRoof, matStone);
        CreateArtisticJapaneseLantern(zone5.transform, new Vector3(3.5f, 0.80f, 1.5f), matStone, matRoof, matLanternLit, matLanternUnlit, sndLantern, true);
        CreateArtisticJapaneseLantern(zone5.transform, new Vector3(-3.5f, 0.80f, 1.5f), matStone, matRoof, matLanternLit, matLanternUnlit, sndLantern, true);
        // Guardián Humanoide 3D Zen Guardian (Mixamo Kachujin G Rosales)
        var zenGuardianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/ZenGuardian/Prefabs/ZenGuardian.prefab");
        if (zenGuardianPrefab == null)
        {
            zenGuardianPrefab = ZenGardenVR.EditorTools.ZenGuardianSetup.SetupAssetsAndPrefab();
        }

        if (zenGuardianPrefab != null)
        {
            var zgInstance = (GameObject)PrefabUtility.InstantiatePrefab(zenGuardianPrefab);
            zgInstance.name = "ZenGuardian";
            zgInstance.transform.SetParent(zone5.transform, false);
            zgInstance.transform.localPosition = new Vector3(0, 0.80f, 3.5f);
            zgInstance.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            zgInstance.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(zgInstance);
        }
        else
        {
            CreateHumanoidGuardian(zone5.transform, new Vector3(0, 0.80f, 3.5f), matGuardian, matGuardianGold, gestureDetector);
        }
        CreateCherryBlossomTree(zone5.transform, new Vector3(-9.0f, 0, 5.0f), matWood, matCherry, 1.45f);
        CreateCherryBlossomTree(zone5.transform, new Vector3(9.0f, 0, 5.0f), matWood, matCherry, 1.45f);

        // Guardar escena y confirmar todos los assets en disco
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainVRScene.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ZenGardenBuilder] ¡Escena MainVRScene.unity generada y configurada exitosamente!");
    }

    private static TextMeshProUGUI CreateTMPText(string name, Transform parent, Vector2 pos, Vector2 size, float fontSize, string initialText, Color color, bool isBold)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.text = initialText;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (isBold) tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    private static void CreateTraditionalSignboard(Transform parent, Vector3 localPos, Material matWood, Material matRoof, Material matBoard)
    {
        var boardRoot = new GameObject("Traditional Welcome Signboard (Komafuda)");
        boardRoot.transform.SetParent(parent, false);
        boardRoot.transform.localPosition = localPos;
        boardRoot.transform.localRotation = Quaternion.Euler(0, 25f, 0); // Ligeramente orientado hacia el camino de entrada

        // 1. Postes verticales gemelos de madera
        for (int x = -1; x <= 1; x += 2)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = $"SignPost_{x}";
            post.transform.SetParent(boardRoot.transform, false);
            post.transform.localPosition = new Vector3(x * 1.15f, 1.25f, 0);
            post.transform.localScale = new Vector3(0.12f, 1.25f, 0.12f);
            if (matWood) post.GetComponent<MeshRenderer>().material = matWood;
        }

        // 2. Tablón principal de madera
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Notice Board Panel";
        panel.transform.SetParent(boardRoot.transform, false);
        panel.transform.localPosition = new Vector3(0, 1.55f, 0);
        panel.transform.localScale = new Vector3(2.5f, 1.4f, 0.12f);
        if (matWood) panel.GetComponent<MeshRenderer>().material = matWood;

        // 3. Tejadillo tradicional japonés sobre el cartel
        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Board Roof";
        roof.transform.SetParent(boardRoot.transform, false);
        roof.transform.localPosition = new Vector3(0, 2.32f, 0);
        roof.transform.localScale = new Vector3(2.8f, 0.14f, 0.45f);
        if (matRoof) roof.GetComponent<MeshRenderer>().material = matRoof;

        // 4. Lienzo de texto World Space
        var canvasGo = new GameObject("Signboard Canvas");
        canvasGo.transform.SetParent(panel.transform, false);
        canvasGo.transform.localPosition = new Vector3(0, 0, -0.55f); // Delante de la cara frontal
        canvasGo.transform.localRotation = Quaternion.identity;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1000, 560);
        rt.localScale = Vector3.one * 0.0024f; // Ajustado a la escala física del tablón (2.4m x 1.34m)

        // Título del Santuario
        CreateTMPText("Title", canvasGo.transform, new Vector2(0, 210), new Vector2(950, 70), 36,
            "JARDÍN ZEN: EL ESPÍRITU DE LA ARMONÍA", new Color(0.95f, 0.85f, 0.45f), true);

        // Misiones e historia
        string infoText = "<b>OBJETIVO DEL PEREGRINO</b>\n" +
            "1. Contempla el jardín de arena Karesansui.\n" +
            "2. Cruza el río sagrado por el puente tradicional.\n" +
            "3. Enciende los 4 faroles votivos (Clic sobre ellos).\n" +
            "4. Coloca la gema en el altar de contemplación.\n" +
            "5. Encuentra al <b>Zen Guardian</b> en el santuario y conversa con él.\n\n" +
            "<b>CONTROLES DE RV & SIMULADOR</b>\n" +
            "• <b>Moverse:</b> Teclas W, A, S, D  |  <b>Mirar:</b> Clic Derecho sostenido\n" +
            "• <b>Teletransporte:</b> Mantener <i>Espacio</i> + Clic Izquierdo al suelo\n" +
            "• <b>Interactuar / Clic:</b> Clic Izquierdo sobre faroles / gemas\n" +
            "• <b>Conversar con Guardián:</b> Espacio / E  (Y = Asentir, N = Reflexionar)\n" +
            "• <b>Recentrar controles/vista:</b> Tecla V  |  <b>Alternar panel:</b> Tecla M o H";

        var bodyTMP = CreateTMPText("BodyText", canvasGo.transform, new Vector2(0, -35), new Vector2(940, 420), 22,
            infoText, new Color(0.95f, 0.95f, 0.95f), false);
        bodyTMP.alignment = TextAlignmentOptions.TopLeft;
    }

    private static void CreateToriiGate(Transform parent, Vector3 localPos, Material matTorii, Material matRoof)
    {
        var torii = new GameObject("Torii Gate");
        torii.transform.SetParent(parent, false);
        torii.transform.localPosition = localPos;

        var pL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pL.name = "Pillar_Left";
        pL.transform.SetParent(torii.transform, false);
        pL.transform.localPosition = new Vector3(-2.4f, 2.8f, 0);
        pL.transform.localScale = new Vector3(0.48f, 2.8f, 0.48f);
        if (matTorii) pL.GetComponent<MeshRenderer>().material = matTorii;

        var pR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pR.name = "Pillar_Right";
        pR.transform.SetParent(torii.transform, false);
        pR.transform.localPosition = new Vector3(2.4f, 2.8f, 0);
        pR.transform.localScale = new Vector3(0.48f, 2.8f, 0.48f);
        if (matTorii) pR.GetComponent<MeshRenderer>().material = matTorii;

        var beamTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beamTop.name = "Kasagi_TopBeam";
        beamTop.transform.SetParent(torii.transform, false);
        beamTop.transform.localPosition = new Vector3(0, 5.7f, 0);
        beamTop.transform.localScale = new Vector3(6.5f, 0.5f, 0.65f);
        if (matRoof) beamTop.GetComponent<MeshRenderer>().material = matRoof;

        var beamSub = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beamSub.name = "Nuki_SubBeam";
        beamSub.transform.SetParent(torii.transform, false);
        beamSub.transform.localPosition = new Vector3(0, 4.7f, 0);
        beamSub.transform.localScale = new Vector3(5.5f, 0.35f, 0.45f);
        if (matTorii) beamSub.GetComponent<MeshRenderer>().material = matTorii;
    }

    private static void CreateStonePath(Transform parent, Vector3 startPos, int count, Material matStone)
    {
        var pathRoot = new GameObject("Stone Stepping Path");
        pathRoot.transform.SetParent(parent, false);
        pathRoot.transform.localPosition = startPos;

        for (int i = 0; i < count; i++)
        {
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stone.name = $"SteppingStone_{i}";
            stone.transform.SetParent(pathRoot.transform, false);
            stone.transform.localPosition = new Vector3(Mathf.Sin(i * 0.9f) * 0.35f, 0.03f, i * 1.5f);
            stone.transform.localScale = new Vector3(1.15f, 0.05f, 0.95f);
            if (matStone) stone.GetComponent<MeshRenderer>().material = matStone;
            stone.AddComponent<TeleportationArea>();
        }
    }

    /// <summary>
    /// Farol Japonés (Tōrō) de alta calidad artesanal: base escalonada (kiso), fuste octagonal (sao),
    /// plataforma de loto (chūdai), caja de luz con celosía y techo pagoda curvado con remate sagrado (hōju).
    /// </summary>
    private static void CreateArtisticJapaneseLantern(Transform parent, Vector3 localPos, Material matStone, Material matRoof, Material matLit, Material matUnlit, AudioClip snd, bool startLit)
    {
        var lanternGo = new GameObject("Traditional Stone Lantern (Tōrō)");
        lanternGo.transform.SetParent(parent, false);
        lanternGo.transform.localPosition = localPos;

        // 1. Base escalonada doble (Kiso)
        var baseBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBottom.name = "Kiso_Bottom";
        baseBottom.transform.SetParent(lanternGo.transform, false);
        baseBottom.transform.localPosition = new Vector3(0, 0.1f, 0);
        baseBottom.transform.localScale = new Vector3(0.9f, 0.2f, 0.9f);
        if (matStone) baseBottom.GetComponent<MeshRenderer>().material = matStone;

        var baseTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseTop.name = "Kiso_Top";
        baseTop.transform.SetParent(lanternGo.transform, false);
        baseTop.transform.localPosition = new Vector3(0, 0.25f, 0);
        baseTop.transform.localScale = new Vector3(0.72f, 0.15f, 0.72f);
        if (matStone) baseTop.GetComponent<MeshRenderer>().material = matStone;

        // 2. Fuste o columna cilíndrica de piedra (Sao)
        var sao = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sao.name = "Sao_Pillar";
        sao.transform.SetParent(lanternGo.transform, false);
        sao.transform.localPosition = new Vector3(0, 0.8f, 0);
        sao.transform.localScale = new Vector3(0.32f, 0.45f, 0.32f);
        if (matStone) sao.GetComponent<MeshRenderer>().material = matStone;

        // 3. Plataforma del medio (Chūdai)
        var chudai = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chudai.name = "Chudai_Platform";
        chudai.transform.SetParent(lanternGo.transform, false);
        chudai.transform.localPosition = new Vector3(0, 1.3f, 0);
        chudai.transform.localScale = new Vector3(0.75f, 0.15f, 0.75f);
        if (matStone) chudai.GetComponent<MeshRenderer>().material = matStone;

        // 4. Cámara de Luz (Hibukuro)
        var fireChamber = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fireChamber.name = "Hibukuro_FireChamber";
        fireChamber.transform.SetParent(lanternGo.transform, false);
        fireChamber.transform.localPosition = new Vector3(0, 1.7f, 0);
        fireChamber.transform.localScale = new Vector3(0.52f, 0.55f, 0.52f);
        var chamberRen = fireChamber.GetComponent<MeshRenderer>();
        chamberRen.material = startLit ? matLit : matUnlit;

        // 4 Pilares decorativos esquineros de la celosía
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Post_{x}_{z}";
                post.transform.SetParent(lanternGo.transform, false);
                post.transform.localPosition = new Vector3(x * 0.27f, 1.7f, z * 0.27f);
                post.transform.localScale = new Vector3(0.08f, 0.56f, 0.08f);
                if (matRoof) post.GetComponent<MeshRenderer>().material = matRoof;
            }
        }

        // 5. Tejado Pagoda con voladizo tradicional (Kasa)
        var kasa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        kasa.name = "Kasa_Roof";
        kasa.transform.SetParent(lanternGo.transform, false);
        kasa.transform.localPosition = new Vector3(0, 2.05f, 0);
        kasa.transform.localScale = new Vector3(1.1f, 0.16f, 1.1f);
        if (matRoof) kasa.GetComponent<MeshRenderer>().material = matRoof;

        // 6. Remate sagrado de perla espiritual en la cúspide (Hōju)
        var hoju = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hoju.name = "Hoju_Jewel";
        hoju.transform.SetParent(lanternGo.transform, false);
        hoju.transform.localPosition = new Vector3(0, 2.25f, 0);
        hoju.transform.localScale = new Vector3(0.25f, 0.3f, 0.25f);
        if (matStone) hoju.GetComponent<MeshRenderer>().material = matStone;

        // Luz cálida interior
        var lightGo = new GameObject("Lantern Light");
        lightGo.transform.SetParent(lanternGo.transform, false);
        lightGo.transform.localPosition = new Vector3(0, 1.7f, 0);
        var ptLight = lightGo.AddComponent<Light>();
        ptLight.type = LightType.Point;
        ptLight.color = new Color(1.0f, 0.78f, 0.45f);
        ptLight.range = 5.5f;
        ptLight.intensity = 2.4f;
        ptLight.enabled = startLit;

        // Audio
        var aSrc = lanternGo.AddComponent<AudioSource>();
        aSrc.spatialBlend = 1.0f;
        aSrc.playOnAwake = false;

        // Collider y componentes de interacción XR
        var col = lanternGo.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 1.25f, 0);
        col.size = new Vector3(1.1f, 2.5f, 1.1f);

        lanternGo.AddComponent<XRSimpleInteractable>();
        var interLantern = lanternGo.AddComponent<InteractiveLantern>();

        var so = new SerializedObject(interLantern);
        so.FindProperty("lanternLight").objectReferenceValue = ptLight;
        so.FindProperty("lightCoverRenderer").objectReferenceValue = chamberRen;
        so.FindProperty("litMaterial").objectReferenceValue = matLit;
        so.FindProperty("unlitMaterial").objectReferenceValue = matUnlit;
        so.FindProperty("audioSource").objectReferenceValue = aSrc;
        so.FindProperty("igniteSound").objectReferenceValue = snd;
        so.FindProperty("extinguishSound").objectReferenceValue = snd;
        so.FindProperty("isLit").boolValue = startLit;
        so.ApplyModifiedProperties();
    }

    private static void CreateRakedSandGarden(Transform parent, Vector3 localPos, Material matSand, Material matBorder, Material matStone)
    {
        var sandGarden = new GameObject("Karesansui Sand Area");
        sandGarden.transform.SetParent(parent, false);
        sandGarden.transform.localPosition = localPos;

        var sandBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sandBed.name = "Sand Bed";
        sandBed.transform.SetParent(sandGarden.transform, false);
        sandBed.transform.localPosition = new Vector3(0, 0.05f, 0);
        sandBed.transform.localScale = new Vector3(14f, 0.15f, 14f);
        if (matSand) sandBed.GetComponent<MeshRenderer>().material = matSand;
        sandBed.AddComponent<TeleportationArea>();

        CreateBorderCube(sandGarden.transform, new Vector3(0, 0.15f, 7.1f), new Vector3(14.4f, 0.35f, 0.3f), matBorder);
        CreateBorderCube(sandGarden.transform, new Vector3(0, 0.15f, -7.1f), new Vector3(14.4f, 0.35f, 0.3f), matBorder);
        CreateBorderCube(sandGarden.transform, new Vector3(7.1f, 0.15f, 0), new Vector3(0.3f, 0.35f, 14.4f), matBorder);
        CreateBorderCube(sandGarden.transform, new Vector3(-7.1f, 0.15f, 0), new Vector3(0.3f, 0.35f, 14.4f), matBorder);

        CreateRockCluster(sandGarden.transform, new Vector3(-3.5f, 0.5f, -2.5f), matStone);
        CreateRockCluster(sandGarden.transform, new Vector3(4.0f, 0.6f, 3.0f), matStone);
    }

    private static void CreateBorderCube(Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = "WoodBorder";
        c.transform.SetParent(parent, false);
        c.transform.localPosition = pos;
        c.transform.localScale = scale;
        if (mat) c.GetComponent<MeshRenderer>().material = mat;
    }

    private static void CreateRockCluster(Transform parent, Vector3 center, Material matStone)
    {
        var cluster = new GameObject("Zen Rock Cluster");
        cluster.transform.SetParent(parent, false);
        cluster.transform.localPosition = center;

        var r1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        r1.name = "Central Rock";
        r1.transform.SetParent(cluster.transform, false);
        r1.transform.localPosition = Vector3.zero;
        r1.transform.localScale = new Vector3(1.6f, 1.3f, 1.4f);
        if (matStone) r1.GetComponent<MeshRenderer>().material = matStone;

        var r2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        r2.name = "Small Rock A";
        r2.transform.SetParent(cluster.transform, false);
        r2.transform.localPosition = new Vector3(1.1f, -0.3f, 0.5f);
        r2.transform.localScale = new Vector3(0.9f, 0.7f, 0.8f);
        if (matStone) r2.GetComponent<MeshRenderer>().material = matStone;

        var r3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        r3.name = "Small Rock B";
        r3.transform.SetParent(cluster.transform, false);
        r3.transform.localPosition = new Vector3(-0.9f, -0.4f, -0.4f);
        r3.transform.localScale = new Vector3(0.8f, 0.6f, 0.7f);
        if (matStone) r3.GetComponent<MeshRenderer>().material = matStone;
    }

    private static void CreateBonsaiAltar(Transform parent, Vector3 localPos, Material matWood, Material matCherry, AudioClip sndChime)
    {
        var altar = new GameObject("Sacred Bonsai Altar");
        altar.transform.SetParent(parent, false);
        altar.transform.localPosition = localPos;

        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Altar Table";
        table.transform.SetParent(altar.transform, false);
        table.transform.localPosition = new Vector3(0, 0.5f, 0);
        table.transform.localScale = new Vector3(1.8f, 0.9f, 1.4f);
        if (matWood) table.GetComponent<MeshRenderer>().material = matWood;

        var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pot.name = "Bonsai Pot";
        pot.transform.SetParent(altar.transform, false);
        pot.transform.localPosition = new Vector3(0, 1.05f, 0);
        pot.transform.localScale = new Vector3(0.85f, 0.15f, 0.7f);
        if (matWood) pot.GetComponent<MeshRenderer>().material = matWood;

        var treeRoot = new GameObject("Bonsai Trunk & Foliage");
        treeRoot.transform.SetParent(altar.transform, false);
        treeRoot.transform.localPosition = new Vector3(0, 1.15f, 0);

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(treeRoot.transform, false);
        trunk.transform.localPosition = new Vector3(0, 0.35f, 0);
        trunk.transform.localRotation = Quaternion.Euler(15f, 0, 10f);
        trunk.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
        if (matWood) trunk.GetComponent<MeshRenderer>().material = matWood;

        var crown1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown1.name = "Foliage_Center";
        crown1.transform.SetParent(treeRoot.transform, false);
        crown1.transform.localPosition = new Vector3(0.15f, 0.75f, 0);
        crown1.transform.localScale = new Vector3(0.75f, 0.45f, 0.65f);
        if (matCherry) crown1.GetComponent<MeshRenderer>().material = matCherry;

        var crown2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown2.name = "Foliage_Left";
        crown2.transform.SetParent(treeRoot.transform, false);
        crown2.transform.localPosition = new Vector3(-0.35f, 0.65f, 0.1f);
        crown2.transform.localScale = new Vector3(0.55f, 0.35f, 0.5f);
        if (matCherry) crown2.GetComponent<MeshRenderer>().material = matCherry;

        var psGo = new GameObject("Blossom Petal Particles");
        psGo.transform.SetParent(altar.transform, false);
        psGo.transform.localPosition = new Vector3(0, 1.8f, 0);
        var ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(1.0f, 0.75f, 0.85f, 0.85f);
        main.startSize = 0.08f;
        main.startLifetime = 3.5f;
        main.startSpeed = 0.6f;
        main.maxParticles = 80;
        var em = ps.emission;
        em.rateOverTime = 12f;

        var aSrc = altar.AddComponent<AudioSource>();
        aSrc.spatialBlend = 1.0f;
        aSrc.playOnAwake = false;

        var col = altar.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 1.2f, 0);
        col.size = new Vector3(1.5f, 1.6f, 1.2f);

        altar.AddComponent<XRSimpleInteractable>();
        var bonsai = altar.AddComponent<InteractiveBonsai>();

        var soB = new SerializedObject(bonsai);
        soB.FindProperty("petalParticles").objectReferenceValue = ps;
        soB.FindProperty("audioSource").objectReferenceValue = aSrc;
        soB.FindProperty("chimeSound").objectReferenceValue = sndChime;
        soB.FindProperty("branchRoot").objectReferenceValue = treeRoot.transform;
        soB.ApplyModifiedProperties();
    }

    private static void CreateCherryBlossomTree(Transform parent, Vector3 localPos, Material matWood, Material matCherry, float scaleMultiplier = 1.0f)
    {
        var tree = new GameObject("Japanese Cherry Tree (Sakura)");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = localPos;
        tree.transform.localScale = Vector3.one * scaleMultiplier;

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0, 2.0f, 0);
        trunk.transform.localScale = new Vector3(0.55f, 2.0f, 0.55f);
        if (matWood) trunk.GetComponent<MeshRenderer>().material = matWood;

        var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Sakura Canopy Core";
        canopy.transform.SetParent(tree.transform, false);
        canopy.transform.localPosition = new Vector3(0, 4.2f, 0);
        canopy.transform.localScale = new Vector3(4.2f, 2.8f, 4.2f);
        if (matCherry) canopy.GetComponent<MeshRenderer>().material = matCherry;

        var crownL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownL.name = "Sakura Crown Left";
        crownL.transform.SetParent(tree.transform, false);
        crownL.transform.localPosition = new Vector3(-1.8f, 3.8f, 0.8f);
        crownL.transform.localScale = new Vector3(2.6f, 2.0f, 2.6f);
        if (matCherry) crownL.GetComponent<MeshRenderer>().material = matCherry;

        var crownR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownR.name = "Sakura Crown Right";
        crownR.transform.SetParent(tree.transform, false);
        crownR.transform.localPosition = new Vector3(1.8f, 3.9f, -0.6f);
        crownR.transform.localScale = new Vector3(2.8f, 2.1f, 2.8f);
        if (matCherry) crownR.GetComponent<MeshRenderer>().material = matCherry;

        var petalFxGo = new GameObject("Falling Sakura Petals");
        petalFxGo.transform.SetParent(tree.transform, false);
        petalFxGo.transform.localPosition = new Vector3(0, 3.5f, 0);
        var ps = petalFxGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(1.0f, 0.72f, 0.82f, 0.85f);
        main.startSize = 0.08f;
        main.startLifetime = 5.0f;
        main.startSpeed = 0.45f;
        main.maxParticles = 40;
        var em = ps.emission;
        em.rateOverTime = 6f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2.5f;
    }

    private static void CreateBambooGrove(Transform parent, Vector3 center, Material matGreen)
    {
        var grove = new GameObject("Bamboo Grove");
        grove.transform.SetParent(parent, false);
        grove.transform.localPosition = center;

        for (int i = 0; i < 9; i++)
        {
            var stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stalk.name = $"BambooStalk_{i}";
            stalk.transform.SetParent(grove.transform, false);
            stalk.transform.localPosition = new Vector3(Mathf.Sin(i * 1.0f) * 1.8f, 2.8f, Mathf.Cos(i * 1.0f) * 1.8f);
            stalk.transform.localScale = new Vector3(0.18f, 2.8f, 0.18f);
            if (matGreen) stalk.GetComponent<MeshRenderer>().material = matGreen;
        }
    }

    private static void CreateMagicGarden(Transform parent, Vector3 localPos, Material matStone, Material matCrystal, AudioClip sndGrab, AudioClip sndChime)
    {
        var magicRoot = new GameObject("Spiritual Circle");
        magicRoot.transform.SetParent(parent, false);
        magicRoot.transform.localPosition = localPos;

        var dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dais.name = "Magic Dais";
        dais.transform.SetParent(magicRoot.transform, false);
        dais.transform.localPosition = new Vector3(0, 0.1f, 0);
        dais.transform.localScale = new Vector3(7f, 0.1f, 7f);
        if (matStone) dais.GetComponent<MeshRenderer>().material = matStone;
        dais.AddComponent<TeleportationArea>();

        var altarGo = new GameObject("Altar Zone (Snap Point)");
        altarGo.transform.SetParent(magicRoot.transform, false);
        altarGo.transform.localPosition = new Vector3(0, 0.6f, 0);

        var snapPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        snapPillar.name = "Altar Pillar";
        snapPillar.transform.SetParent(altarGo.transform, false);
        snapPillar.transform.localPosition = Vector3.zero;
        snapPillar.transform.localScale = new Vector3(0.9f, 0.5f, 0.9f);
        if (matStone) snapPillar.GetComponent<MeshRenderer>().material = matStone;

        var trigCol = altarGo.AddComponent<BoxCollider>();
        trigCol.isTrigger = true;
        trigCol.size = new Vector3(1.5f, 1.5f, 1.5f);

        var aLtGo = new GameObject("Altar Light");
        aLtGo.transform.SetParent(altarGo.transform, false);
        aLtGo.transform.localPosition = new Vector3(0, 0.7f, 0);
        var aLt = aLtGo.AddComponent<Light>();
        aLt.type = LightType.Point;
        aLt.color = new Color(0.4f, 1.0f, 0.6f);
        aLt.range = 4.5f;
        aLt.intensity = 2.5f;
        aLt.enabled = true;

        var aPsGo = new GameObject("Altar Aura Particles");
        aPsGo.transform.SetParent(altarGo.transform, false);
        aPsGo.transform.localPosition = new Vector3(0, 0.7f, 0);
        var aPs = aPsGo.AddComponent<ParticleSystem>();
        var aMain = aPs.main;
        aMain.startColor = new Color(0.4f, 1.0f, 0.6f, 0.8f);
        aMain.startSize = 0.15f;
        aMain.playOnAwake = true;

        var altarAudio = altarGo.AddComponent<AudioSource>();
        altarAudio.spatialBlend = 1.0f;
        altarAudio.playOnAwake = false;

        var altarComp = altarGo.AddComponent<AltarZone>();
        var soA = new SerializedObject(altarComp);
        soA.FindProperty("stoneSnapPoint").objectReferenceValue = snapPillar.transform;
        soA.FindProperty("altarParticles").objectReferenceValue = aPs;
        soA.FindProperty("altarLight").objectReferenceValue = aLt;
        soA.FindProperty("audioSource").objectReferenceValue = altarAudio;
        soA.FindProperty("activationSound").objectReferenceValue = sndChime;
        soA.ApplyModifiedProperties();

        var stoneGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        stoneGo.name = "Interactive Magic Stone";
        stoneGo.transform.SetParent(altarGo.transform, false);
        stoneGo.transform.localPosition = new Vector3(0, 0.45f, 0); // Descansando en la cima del pedestal del altar
        stoneGo.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        var stoneRen = stoneGo.GetComponent<MeshRenderer>();
        if (matCrystal) stoneRen.material = matCrystal;

        var rb = stoneGo.AddComponent<Rigidbody>();
        rb.mass = 1.0f;
        rb.isKinematic = true; // Estable sobre el altar
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var grab = stoneGo.AddComponent<XRGrabInteractable>();

        var sLtGo = new GameObject("Crystal Glow Light");
        sLtGo.transform.SetParent(stoneGo.transform, false);
        sLtGo.transform.localPosition = Vector3.zero;
        var sLt = sLtGo.AddComponent<Light>();
        sLt.type = LightType.Point;
        sLt.color = new Color(0.4f, 0.7f, 1f);
        sLt.range = 2.5f;
        sLt.intensity = 2.0f;

        var sAudio = stoneGo.AddComponent<AudioSource>();
        sAudio.spatialBlend = 1.0f;
        sAudio.playOnAwake = false;

        var magicStoneComp = stoneGo.AddComponent<MagicStone>();
        var soM = new SerializedObject(magicStoneComp);
        soM.FindProperty("stoneRenderer").objectReferenceValue = stoneRen;
        soM.FindProperty("auraLight").objectReferenceValue = sLt;
        soM.FindProperty("audioSource").objectReferenceValue = sAudio;
        soM.FindProperty("grabSound").objectReferenceValue = sndGrab;
        soM.FindProperty("releaseSound").objectReferenceValue = sndGrab;
        soM.ApplyModifiedProperties();
    }

    private static void CreateScenicRiverAndBridge(Transform parent, Vector3 localPos, Material matWater, Material matStone, Material matBridge, Material matWood, AudioClip sndWater, Material matCherry = null)
    {
        var riverRoot = new GameObject("River and Traditional Arched Bridge");
        riverRoot.transform.SetParent(parent, false);
        riverRoot.transform.localPosition = localPos;

        // Lecho del río
        var riverBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        riverBed.name = "River Bed Underneath";
        riverBed.transform.SetParent(riverRoot.transform, false);
        riverBed.transform.localPosition = new Vector3(0, -0.4f, 0);
        riverBed.transform.localScale = new Vector3(38f, 0.3f, 11f);
        if (matStone) riverBed.GetComponent<MeshRenderer>().material = matStone;

        // Superficie viva de agua translúcida
        var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = "Flowing River Water Surface";
        water.transform.SetParent(riverRoot.transform, false);
        water.transform.localPosition = new Vector3(0, 0.05f, 0);
        water.transform.localScale = new Vector3(38f, 0.15f, 10.5f);
        if (matWater) water.GetComponent<MeshRenderer>().material = matWater;
        water.AddComponent<WaterSurfaceOscillator>();

        // Audio del río
        var riverAudio = riverRoot.AddComponent<AudioSource>();
        riverAudio.clip = sndWater;
        riverAudio.loop = true;
        riverAudio.spatialBlend = 0.85f;
        riverAudio.volume = 0.5f;
        riverAudio.playOnAwake = true;
        riverAudio.Play();

        // Rocas y orillas ribereñas
        for (int i = -4; i <= 4; i++)
        {
            var boulderNorth = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulderNorth.name = $"RiverBank_North_{i}";
            boulderNorth.transform.SetParent(riverRoot.transform, false);
            boulderNorth.transform.localPosition = new Vector3(i * 4.0f + Mathf.Sin(i) * 0.8f, 0.2f, 5.2f);
            boulderNorth.transform.localScale = new Vector3(2.2f, 0.9f, 1.8f);
            if (matStone) boulderNorth.GetComponent<MeshRenderer>().material = matStone;

            var boulderSouth = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulderSouth.name = $"RiverBank_South_{i}";
            boulderSouth.transform.SetParent(riverRoot.transform, false);
            boulderSouth.transform.localPosition = new Vector3(i * 4.0f + Mathf.Cos(i) * 0.8f, 0.2f, -5.2f);
            boulderSouth.transform.localScale = new Vector3(2.1f, 0.85f, 1.7f);
            if (matStone) boulderSouth.GetComponent<MeshRenderer>().material = matStone;
        }

        // Nenúfares y Flores de Loto Flotantes en el río
        Vector3[] lilyPadPositions = new Vector3[]
        {
            new Vector3(-8.0f, 0.08f, 2.2f),
            new Vector3(-6.5f, 0.08f, -2.4f),
            new Vector3(-4.8f, 0.08f, 3.1f),
            new Vector3(4.8f, 0.08f, -2.8f),
            new Vector3(7.2f, 0.08f, 2.0f),
            new Vector3(9.5f, 0.08f, -1.8f)
        };

        for (int lp = 0; lp < lilyPadPositions.Length; lp++)
        {
            var padGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padGo.name = $"WaterLily_Pad_{lp}";
            padGo.transform.SetParent(riverRoot.transform, false);
            padGo.transform.localPosition = lilyPadPositions[lp];
            padGo.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);
            if (matStone) padGo.GetComponent<MeshRenderer>().material = matStone; // verde/musgo

            // Flor de loto central rosada
            var lotusGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lotusGo.name = $"Lotus_Flower_{lp}";
            lotusGo.transform.SetParent(padGo.transform, false);
            lotusGo.transform.localPosition = new Vector3(0, 1.5f, 0);
            lotusGo.transform.localScale = new Vector3(0.45f, 0.6f, 0.45f);
            if (matCherry) lotusGo.GetComponent<MeshRenderer>().material = matCherry;
        }

        // Puente arqueado tradicional japonés (Soribashi / Taiko-bashi)
        var bridge = new GameObject("Traditional Japanese Arched Bridge");
        bridge.transform.SetParent(riverRoot.transform, false);
        bridge.transform.localPosition = Vector3.zero;

        const float archHeight = 0.95f;
        const float spanHalf = 5.2f;
        const float baseY = 0.12f;

        // Función matemática del arco suave del puente
        float GetBridgeY(float z) => baseY + archHeight * (1.0f - Mathf.Pow(z / spanHalf, 2.0f));
        float GetBridgeAngleDeg(float z)
        {
            float dydz = -2.0f * archHeight * z / (spanHalf * spanHalf);
            return Mathf.Atan(dydz) * Mathf.Rad2Deg;
        }

        // 1. Tablones de madera secuenciales que conforman el arco curvado continuo
        const int numPlanks = 11;
        for (int p = 0; p < numPlanks; p++)
        {
            float z = -5.0f + p * 1.0f;
            float y = GetBridgeY(z);
            float angle = GetBridgeAngleDeg(z);

            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = $"BridgePlank_{p}";
            plank.transform.SetParent(bridge.transform, false);
            plank.transform.localPosition = new Vector3(0, y, z);
            plank.transform.localRotation = Quaternion.Euler(angle, 0, 0);
            plank.transform.localScale = new Vector3(3.0f, 0.18f, 1.08f);
            if (matWood) plank.GetComponent<MeshRenderer>().material = matWood;
            plank.AddComponent<TeleportationArea>();
        }

        // 2. Pilares de soporte de madera debajo del puente sobre el lecho del río
        float[] supportZs = { -2.5f, 0.0f, 2.5f };
        foreach (float sz in supportZs)
        {
            float sy = GetBridgeY(sz);
            float pillarHeight = sy - (-0.4f);

            for (int side = -1; side <= 1; side += 2)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"BridgePillar_{sz}_{side}";
                pillar.transform.SetParent(bridge.transform, false);
                pillar.transform.localPosition = new Vector3(side * 1.25f, -0.4f + pillarHeight * 0.5f, sz);
                pillar.transform.localScale = new Vector3(0.22f, pillarHeight * 0.5f, 0.22f);
                if (matWood) pillar.GetComponent<MeshRenderer>().material = matWood;
            }

            var crossbeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossbeam.name = $"BridgeCrossbeam_{sz}";
            crossbeam.transform.SetParent(bridge.transform, false);
            crossbeam.transform.localPosition = new Vector3(0, sy - 0.14f, sz);
            crossbeam.transform.localScale = new Vector3(2.7f, 0.18f, 0.22f);
            if (matWood) crossbeam.GetComponent<MeshRenderer>().material = matWood;
        }

        // 3. Barandillas rojas tradicionales bermellón a ambos lados
        CreateTraditionalBridgeRailings(bridge.transform, -1.55f, matBridge, matWood);
        CreateTraditionalBridgeRailings(bridge.transform, 1.55f, matBridge, matWood);

        // Fuente Tsukubai
        var fountainGo = new GameObject("Tsukubai Water Fountain");
        fountainGo.transform.SetParent(riverRoot.transform, false);
        fountainGo.transform.localPosition = new Vector3(6.5f, 0, -3.2f);

        var basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basin.name = "Stone Water Basin";
        basin.transform.SetParent(fountainGo.transform, false);
        basin.transform.localPosition = new Vector3(0, 0.45f, 0);
        basin.transform.localScale = new Vector3(1.4f, 0.45f, 1.4f);
        if (matStone) basin.GetComponent<MeshRenderer>().material = matStone;

        var spout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spout.name = "Bamboo Spout";
        spout.transform.SetParent(fountainGo.transform, false);
        spout.transform.localPosition = new Vector3(0, 1.0f, -0.6f);
        spout.transform.localRotation = Quaternion.Euler(75f, 0, 0);
        spout.transform.localScale = new Vector3(0.08f, 0.4f, 0.08f);
        if (matWood) spout.GetComponent<MeshRenderer>().material = matWood;

        var streamPsGo = new GameObject("Water Stream Particles");
        streamPsGo.transform.SetParent(fountainGo.transform, false);
        streamPsGo.transform.localPosition = new Vector3(0, 1.0f, -0.2f);
        var streamPs = streamPsGo.AddComponent<ParticleSystem>();
        var sMain = streamPs.main;
        sMain.startColor = new Color(0.4f, 0.7f, 0.9f, 0.7f);
        sMain.startSize = 0.06f;
        sMain.startSpeed = 1.2f;

        var fAudio = fountainGo.AddComponent<AudioSource>();
        fAudio.spatialBlend = 1.0f;
        fAudio.clip = sndWater;
        fAudio.loop = true;
        fAudio.playOnAwake = true;
        fAudio.Play();

        var fCol = fountainGo.AddComponent<BoxCollider>();
        fCol.center = new Vector3(0, 0.7f, 0);
        fCol.size = new Vector3(1.8f, 1.5f, 1.8f);

        fountainGo.AddComponent<XRSimpleInteractable>();
        var fountainComp = fountainGo.AddComponent<InteractiveFountain>();
        var soF = new SerializedObject(fountainComp);
        soF.FindProperty("waterStreamParticles").objectReferenceValue = streamPs;
        soF.FindProperty("waterAudioSource").objectReferenceValue = fAudio;
        soF.FindProperty("bambooShishi").objectReferenceValue = spout.transform;
        soF.ApplyModifiedProperties();
    }

    private static void CreateTraditionalBridgeRailings(Transform bridgeTransform, float xPos, Material matVermilion, Material matWood)
    {
        const float archHeight = 0.95f;
        const float spanHalf = 5.2f;
        const float baseY = 0.12f;
        float GetBridgeY(float z) => baseY + archHeight * (1.0f - Mathf.Pow(z / spanHalf, 2.0f));

        float[] postZs = { -4.5f, -2.7f, -0.9f, 0.9f, 2.7f, 4.5f };
        Vector3[] postPositions = new Vector3[postZs.Length];

        // Material dorado para los remates budistas Giboshi
        var matGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ZenGuardianGold.mat");

        for (int p = 0; p < postZs.Length; p++)
        {
            float z = postZs[p];
            float y = GetBridgeY(z);
            postPositions[p] = new Vector3(xPos, y, z);

            // Poste vertical bermellón
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = $"RailingPost_{xPos}_{p}";
            post.transform.SetParent(bridgeTransform, false);
            post.transform.localPosition = new Vector3(xPos, y + 0.50f, z);
            post.transform.localScale = new Vector3(0.14f, 0.50f, 0.14f);
            if (matVermilion) post.GetComponent<MeshRenderer>().material = matVermilion;

            // Remate ornamental Giboshi (esfera dorada en la cima del poste)
            var giboshi = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            giboshi.name = $"Giboshi_{xPos}_{p}";
            giboshi.transform.SetParent(bridgeTransform, false);
            giboshi.transform.localPosition = new Vector3(xPos, y + 1.05f, z);
            giboshi.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            if (matGold != null) giboshi.GetComponent<MeshRenderer>().material = matGold;
            else if (matVermilion) giboshi.GetComponent<MeshRenderer>().material = matVermilion;
        }

        // Pasamanos continuos conectando poste con poste
        for (int p = 0; p < postPositions.Length - 1; p++)
        {
            Vector3 p1 = postPositions[p];
            Vector3 p2 = postPositions[p + 1];
            Vector3 mid = (p1 + p2) * 0.5f;
            float length = Vector3.Distance(new Vector3(0, p1.y, p1.z), new Vector3(0, p2.y, p2.z));
            float angleDeg = Mathf.Atan2(p2.y - p1.y, p2.z - p1.z) * Mathf.Rad2Deg;

            // Barandilla superior
            var topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = $"TopRail_{xPos}_{p}";
            topRail.transform.SetParent(bridgeTransform, false);
            topRail.transform.localPosition = new Vector3(xPos, mid.y + 0.95f, mid.z);
            topRail.transform.localRotation = Quaternion.Euler(-angleDeg, 0, 0);
            topRail.transform.localScale = new Vector3(0.12f, 0.12f, length + 0.04f);
            if (matVermilion) topRail.GetComponent<MeshRenderer>().material = matVermilion;

            // Barandilla intermedia
            var midRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midRail.name = $"MidRail_{xPos}_{p}";
            midRail.transform.SetParent(bridgeTransform, false);
            midRail.transform.localPosition = new Vector3(xPos, mid.y + 0.50f, mid.z);
            midRail.transform.localRotation = Quaternion.Euler(-angleDeg, 0, 0);
            midRail.transform.localScale = new Vector3(0.08f, 0.08f, length + 0.04f);
            if (matVermilion) midRail.GetComponent<MeshRenderer>().material = matVermilion;
        }
    }

    private static void CreateShintoSanctuary(Transform parent, Vector3 localPos, Material matTorii, Material matWood, Material matRoof, Material matStone)
    {
        var sanctuary = new GameObject("Shinto Temple Hall");
        sanctuary.transform.SetParent(parent, false);
        sanctuary.transform.localPosition = localPos;

        var dais = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dais.name = "Sanctuary Foundation";
        dais.transform.SetParent(sanctuary.transform, false);
        dais.transform.localPosition = new Vector3(0, 0.4f, 4f);
        dais.transform.localScale = new Vector3(14f, 0.8f, 12f);
        if (matStone) dais.GetComponent<MeshRenderer>().material = matStone;
        dais.AddComponent<TeleportationArea>();

        // Escalones de acceso frontal al estrado del santuario
        var stepLow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stepLow.name = "Sanctuary Access Step Low";
        stepLow.transform.SetParent(sanctuary.transform, false);
        stepLow.transform.localPosition = new Vector3(0, 0.25f, -2.55f);
        stepLow.transform.localScale = new Vector3(7.0f, 0.35f, 1.2f);
        if (matStone) stepLow.GetComponent<MeshRenderer>().material = matStone;
        stepLow.AddComponent<TeleportationArea>();

        var stepHigh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stepHigh.name = "Sanctuary Access Step High";
        stepHigh.transform.SetParent(sanctuary.transform, false);
        stepHigh.transform.localPosition = new Vector3(0, 0.55f, -2.05f);
        stepHigh.transform.localScale = new Vector3(7.0f, 0.35f, 0.9f);
        if (matStone) stepHigh.GetComponent<MeshRenderer>().material = matStone;
        stepHigh.AddComponent<TeleportationArea>();

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = 0; z <= 2; z++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"SanctuaryPillar_{x}_{z}";
                pillar.transform.SetParent(sanctuary.transform, false);
                pillar.transform.localPosition = new Vector3(x * 5.2f, 2.8f, 1.5f + z * 3.5f);
                pillar.transform.localScale = new Vector3(0.55f, 2.0f, 0.55f);
                if (matTorii) pillar.GetComponent<MeshRenderer>().material = matTorii;
            }
        }

        var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "Sanctuary Back Wall";
        backWall.transform.SetParent(sanctuary.transform, false);
        backWall.transform.localPosition = new Vector3(0, 2.8f, 9.5f);
        backWall.transform.localScale = new Vector3(11.5f, 4.0f, 0.4f);
        if (matWood) backWall.GetComponent<MeshRenderer>().material = matWood;

        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Temple Roof";
        roof.transform.SetParent(sanctuary.transform, false);
        roof.transform.localPosition = new Vector3(0, 5.2f, 4.5f);
        roof.transform.localScale = new Vector3(16f, 0.7f, 14f);
        if (matRoof) roof.GetComponent<MeshRenderer>().material = matRoof;
    }

    /// <summary>
    /// Personaje Virtual Humanoide (Guardián del Jardín) basado en las lecturas académicas de RV:
    /// 'Introducción a los personajes virtuales', 'Animación corporal en RV' y 'RV Social'.
    /// <summary>
    /// Guardián Humanoide Virtual del Jardín Zen basado en el modelo 3D rigged 'MazeLowMan.fbx'.
    /// Incluye Animator con Controller de la asignatura, seguimiento de mirada mutua mediante IK (LookAt.cs),
    /// reconocimiento de asentimiento/negación (NodDetector.cs y HeadGestureDetector.cs),
    /// atributos espirituales japoneses (Corona sagrada, Halo Kōrin, Aura de partículas y Luz de santuario).
    /// </summary>
    private static void CreateHumanoidGuardian(Transform parent, Vector3 localPos, Material matFabric, Material matGold, HeadGestureDetector gestureDetector)
    {
        var guardianGo = new GameObject("Guardian del Jardin Zen (Virtual Humanoid)");
        guardianGo.transform.SetParent(parent, false);
        guardianGo.transform.localPosition = localPos;

        // 1. Instanciar el modelo 3D Humanoide FBX (priorizando el modelo Zen Guardian de Mixamo)
        var humanoidFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/ZenGuardian/Models/ZenGuardian.fbx");
        if (humanoidFbx == null) humanoidFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Character/MazeLowMan.fbx");
        GameObject characterModel = null;
        if (humanoidFbx != null)
        {
            characterModel = (GameObject)PrefabUtility.InstantiatePrefab(humanoidFbx);
            characterModel.name = "Humanoid_Character_Rig";
            characterModel.transform.SetParent(guardianGo.transform, false);
            characterModel.transform.localPosition = Vector3.zero;
            characterModel.transform.localRotation = Quaternion.Euler(0, 180f, 0); // Orientado hacia el usuario al aproximarse
            characterModel.transform.localScale = Vector3.one * 1.15f; // Estatura humana natural (1.8m)

            // Desempaquetar completamente para guardar todos los cambios de componentes en la escena
            PrefabUtility.UnpackPrefabInstance(characterModel, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Aplicar vestidura ceremonial Zen a todos los renderers (SkinnedMeshRenderer / MeshRenderer)
            var renderers = characterModel.GetComponentsInChildren<Renderer>(true);
            foreach (var ren in renderers)
            {
                if (matFabric != null)
                {
                    ren.sharedMaterial = matFabric;
                    EditorUtility.SetDirty(ren);
                }
            }
        }

        // 2. Localizar el hueso de la cabeza (LowMan_Head)
        Transform headBone = null;
        if (characterModel != null)
        {
            headBone = FindTransformRecursive(characterModel.transform, "LowMan_Head");
        }

        if (headBone == null)
        {
            var fallbackHead = new GameObject("Head_Joint");
            fallbackHead.transform.SetParent(guardianGo.transform, false);
            fallbackHead.transform.localPosition = new Vector3(0, 1.85f, 0);
            headBone = fallbackHead.transform;
        }

        // 3. Atributos espirituales Shinto del Guardián
        // Corona mística / Tiara dorada en la frente
        var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crown.name = "Spirit_Crown";
        crown.transform.SetParent(headBone, false);
        crown.transform.localPosition = new Vector3(0, 0.12f, 0.02f);
        crown.transform.localScale = new Vector3(0.32f, 0.04f, 0.32f);
        if (matGold)
        {
            var crRen = crown.GetComponent<MeshRenderer>();
            crRen.sharedMaterial = matGold;
            EditorUtility.SetDirty(crRen);
        }

        // Halo Espiritual Circular de Luz Dorada (Kōrin)
        var halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        halo.name = "Kōrin_Spiritual_Halo";
        halo.transform.SetParent(headBone, false);
        halo.transform.localPosition = new Vector3(0, 0.08f, -0.22f);
        halo.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        halo.transform.localScale = new Vector3(0.95f, 0.035f, 0.95f);
        if (matGold)
        {
            var hRen = halo.GetComponent<MeshRenderer>();
            hRen.sharedMaterial = matGold;
            EditorUtility.SetDirty(hRen);
        }

        // Aura de partículas espirituales flotantes
        var auraGo = new GameObject("Guardian Spirit Aura");
        auraGo.transform.SetParent(guardianGo.transform, false);
        auraGo.transform.localPosition = new Vector3(0, 1.4f, 0);
        var auraPs = auraGo.AddComponent<ParticleSystem>();
        var aMain = auraPs.main;
        aMain.startColor = new Color(1.0f, 0.9f, 0.6f, 0.65f);
        aMain.startSize = 0.22f;
        aMain.startLifetime = 3.0f;
        var aShape = auraPs.shape;
        aShape.shapeType = ParticleSystemShapeType.Sphere;
        aShape.radius = 0.85f;

        // Luz puntual de santuario
        var ltGo = new GameObject("Guardian Aura Light");
        ltGo.transform.SetParent(guardianGo.transform, false);
        ltGo.transform.localPosition = new Vector3(0, 1.8f, 0.4f);
        var lt = ltGo.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = new Color(1.0f, 0.88f, 0.6f);
        lt.range = 6.0f;
        lt.intensity = 2.8f;

        var aSrc = guardianGo.AddComponent<AudioSource>();
        aSrc.spatialBlend = 1.0f;

        // 4. Configurar Animator con el controlador de la asignatura y Avatar Humanoide
        var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Character/ClassScripts/MaleCharacterAnimator.controller");
        Animator animator = null;
        if (characterModel != null)
        {
            animator = characterModel.GetComponent<Animator>();
            if (animator == null) animator = characterModel.AddComponent<Animator>();
            if (animCtrl != null) animator.runtimeAnimatorController = animCtrl;
            animator.applyRootMotion = false;

            // Vincular el Avatar humanoide del archivo FBX
            var subAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Character/MazeLowMan.fbx");
            foreach (var sub in subAssets)
            {
                if (sub is Avatar av)
                {
                    animator.avatar = av;
                    break;
                }
            }
            EditorUtility.SetDirty(animator);
        }

        // 5. Integrar scripts de interacción de la clase: LookAt y NodDetector
        if (characterModel != null)
        {
            var lookAt = characterModel.AddComponent<LookAt>();
            lookAt.eye = headBone;
            lookAt.transitionSpeed = 2.5f;
            lookAt.lookAtTime = 4.0f;
            lookAt.lookAwayTime = 1.8f;
            lookAt.headLookProb = 0.85f;
            lookAt.eyeLookProb = 1.0f;
            EditorUtility.SetDirty(lookAt);

            var nodDet = characterModel.AddComponent<NodDetector>();
            nodDet.threshold = 0.25f;
            nodDet.nodParameter = "Nod";
            nodDet.shakeParameter = "Shake";
            EditorUtility.SetDirty(nodDet);
        }

        // 6. Componente de lógica conversacional del Guardián
        var guardian = guardianGo.AddComponent<GardenGuardian>();
        var soG = new SerializedObject(guardian);
        soG.FindProperty("voiceAudioSource").objectReferenceValue = aSrc;
        soG.FindProperty("gestureDetector").objectReferenceValue = gestureDetector;
        soG.FindProperty("spiritAuraParticles").objectReferenceValue = auraPs;
        soG.FindProperty("spiritualLight").objectReferenceValue = lt;
        soG.FindProperty("headJoint").objectReferenceValue = headBone;
        if (animator != null) soG.FindProperty("animator").objectReferenceValue = animator;
        soG.ApplyModifiedProperties();

        EditorUtility.SetDirty(characterModel);
        EditorUtility.SetDirty(guardianGo);
    }

    private static Transform FindTransformRecursive(Transform parent, string targetName)
    {
        if (parent.name == targetName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var res = FindTransformRecursive(parent.GetChild(i), targetName);
            if (res != null) return res;
        }
        return null;
    }

    private static Material GetOrCreateMaterial(string path, Color baseColor, float smoothness = 0.2f, float metallic = 0.0f, Color? emission = null, bool transparent = false)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            if (emission.HasValue && emission.Value != Color.black)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            if (transparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static void EnsureURPPipelineConfigured()
    {
        var settingsDir = "Assets/Settings";
        if (!System.IO.Directory.Exists(settingsDir))
        {
            System.IO.Directory.CreateDirectory(settingsDir);
        }

        var urpAssetPath = "Assets/Settings/UniversalRenderPipelineAsset.asset";
        var rendererDataPath = "Assets/Settings/UniversalRendererData.asset";

        var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
        if (urpAsset == null)
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererDataPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererDataPath);
            }

            urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            AssetDatabase.SaveAssets();
        }

        GraphicsSettings.defaultRenderPipeline = urpAsset;
        QualitySettings.renderPipeline = urpAsset;
        Debug.Log("[ZenGardenBuilder] URP Pipeline asegurado y asignado como renderizador activo.");
    }
}
