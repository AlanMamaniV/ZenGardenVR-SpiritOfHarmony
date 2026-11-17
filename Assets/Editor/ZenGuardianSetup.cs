using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using ZenGardenVR.Character;

namespace ZenGardenVR.EditorTools
{
    /// <summary>
    /// Editor Script para la configuración automatizada y validación del personaje
    /// Zen Guardian a partir de los modelos FBX de Mixamo en Unity 6.
    /// </summary>
    public static class ZenGuardianSetup
    {
        private const string ModelsDir = "Assets/Characters/ZenGuardian/Models";
        private const string AnimsDir = "Assets/Characters/ZenGuardian/Animations";
        private const string MatsDir = "Assets/Characters/ZenGuardian/Materials";
        private const string TexturesDir = "Assets/Characters/ZenGuardian/Textures";
        private const string AnimatorDir = "Assets/Characters/ZenGuardian/Animator";
        private const string PrefabsDir = "Assets/Characters/ZenGuardian/Prefabs";

        private const string MainModelPath = ModelsDir + "/ZenGuardian.fbx";
        private const string ControllerPath = AnimatorDir + "/ZenGuardian.controller";
        private const string PrefabPath = PrefabsDir + "/ZenGuardian.prefab";
        private const string ScenePath = "Assets/Scenes/MainVRScene.unity";

        [MenuItem("ZenGarden/Configurar Zen Guardian Completo", false, 10)]
        public static void RunFullSetup()
        {
            var prefab = SetupAssetsAndPrefab();

            if (prefab != null)
            {
                IntegrateIntoActiveScene(prefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>=== [ZenGuardianSetup] ¡Configuración del Zen Guardian completada con éxito! ===</color>");
            EditorUtility.DisplayDialog("Zen Guardian Configurado",
                "El personaje Zen Guardian ha sido configurado exitosamente con su Avatar Humanoide, animaciones de Mixamo, Animator Controller, Prefab y ubicado en el Santuario de la escena VR.",
                "Excelente");
        }

        public static GameObject SetupAssetsAndPrefab()
        {
            Debug.Log("<color=cyan>=== [ZenGuardianSetup] Iniciando configuración de assets y Prefab del Zen Guardian ===</color>");

            EnsureDirectories();
            OrganizeFiles();
            var avatar = ConfigureMainModelAvatar();

            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError("[ZenGuardianSetup] ERROR: El Avatar de ZenGuardian.fbx no es válido o no es humanoide. Abortando.");
                return null;
            }

            Debug.Log($"<color=green>[ZenGuardianSetup] Avatar Humanoide generado con éxito: '{avatar.name}'.</color>");

            ConfigureAnimations(avatar);
            var controller = CreateAnimatorController();
            var mat = EnsureURPMaterial();
            return CreateOrUpdatePrefab(avatar, controller, mat);
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { ModelsDir, AnimsDir, MatsDir, TexturesDir, AnimatorDir, PrefabsDir };
            foreach (var d in dirs)
            {
                if (!AssetDatabase.IsValidFolder(d))
                {
                    var parent = Path.GetDirectoryName(d).Replace('\\', '/');
                    var leaf = Path.GetFileName(d);
                    AssetDatabase.CreateFolder(parent, leaf);
                }
            }
        }

        private static void OrganizeFiles()
        {
            // Mapeo seguro de nombres originales a la estructura oficial
            var mappings = new Dictionary<string, string>
            {
                { ModelsDir + "/Kachujin G Rosales.fbx", MainModelPath },
                { ModelsDir + "/Kachujin G Rosales@Idle.fbx", AnimsDir + "/Idle.fbx" },
                { ModelsDir + "/Kachujin G Rosales@Standing Greeting.fbx", AnimsDir + "/StandingGreeting.fbx" },
                { ModelsDir + "/Kachujin G Rosales@Waving.fbx", AnimsDir + "/Waving.fbx" },
                { ModelsDir + "/Kachujin G Rosales@Talking.fbx", AnimsDir + "/Talking.fbx" },
                { ModelsDir + "/Kachujin G Rosales@Head Nod Yes.fbx", AnimsDir + "/HeadNodYes.fbx" },
                { ModelsDir + "/Kachujin G Rosales@Sarcastic Head Nod.fbx", AnimsDir + "/SarcasticHeadNod.fbx" }
            };

            foreach (var pair in mappings)
            {
                if (File.Exists(pair.Key) && !File.Exists(pair.Value))
                {
                    File.Copy(pair.Key, pair.Value, false);
                    Debug.Log($"[ZenGuardianSetup] Copiado seguro: {pair.Key} -> {pair.Value}");
                }
            }

            AssetDatabase.Refresh();
        }

        private static Avatar ConfigureMainModelAvatar()
        {
            Debug.Log("[ZenGuardianSetup] 1. Configurando Rig Humanoid en ZenGuardian.fbx...");
            var importer = AssetImporter.GetAtPath(MainModelPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[ZenGuardianSetup] No se encontró el importador para {MainModelPath}");
                return null;
            }

            bool needsReimport = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                needsReimport = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                needsReimport = true;
            }

            if (needsReimport)
            {
                importer.SaveAndReimport();
            }

            // Cargar el Avatar generado
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(MainModelPath);
            foreach (var sub in subAssets)
            {
                if (sub is Avatar av)
                {
                    return av;
                }
            }

            return null;
        }

        private static void ConfigureAnimations(Avatar sourceAvatar)
        {
            Debug.Log("[ZenGuardianSetup] 2. Configurando archivos FBX de animación con Humanoid Avatar...");

            var animConfigs = new (string path, string clipName, bool loop)[]
            {
                (AnimsDir + "/Idle.fbx", "Idle", true),
                (AnimsDir + "/StandingGreeting.fbx", "StandingGreeting", false),
                (AnimsDir + "/Waving.fbx", "Waving", false),
                (AnimsDir + "/Talking.fbx", "Talking", true),
                (AnimsDir + "/HeadNodYes.fbx", "HeadNodYes", true),
                (AnimsDir + "/SarcasticHeadNod.fbx", "SarcasticHeadNod", false)
            };

            foreach (var cfg in animConfigs)
            {
                if (!File.Exists(cfg.path))
                {
                    Debug.LogWarning($"[ZenGuardianSetup] Archivo de animación no encontrado: {cfg.path}");
                    continue;
                }

                var importer = AssetImporter.GetAtPath(cfg.path) as ModelImporter;
                if (importer == null) continue;

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sourceAvatar;

                // Configurar clip de animación con bloqueo de root motion
                var defaultClips = importer.defaultClipAnimations;
                if (defaultClips != null && defaultClips.Length > 0)
                {
                    for (int i = 0; i < defaultClips.Length; i++)
                    {
                        var clip = defaultClips[i];
                        clip.name = cfg.clipName;
                        clip.loopTime = cfg.loop;
                        clip.loopPose = cfg.loop;

                        // Prevenir desplazamientos involuntarios y traspaso de suelo
                        clip.lockRootRotation = true;
                        clip.keepOriginalOrientation = true;
                        clip.lockRootHeightY = true;
                        clip.keepOriginalPositionY = true;
                        clip.lockRootPositionXZ = true;
                        clip.keepOriginalPositionXZ = true;
                    }
                    importer.clipAnimations = defaultClips;
                }

                importer.SaveAndReimport();
                Debug.Log($"[ZenGuardianSetup] Animación configurada: {cfg.clipName} (Loop: {cfg.loop})");
            }
        }

        private static AnimatorController CreateAnimatorController()
        {
            Debug.Log("[ZenGuardianSetup] 3. Creando Animator Controller en " + ControllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            // Parámetros
            controller.AddParameter("IsListening", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsTalking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Greeting", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Wave", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Nod", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("ExtraNod", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Cargar Motion Clips
            var clipIdle = LoadAnimationClip(AnimsDir + "/Idle.fbx");
            var clipGreeting = LoadAnimationClip(AnimsDir + "/StandingGreeting.fbx");
            var clipWaving = LoadAnimationClip(AnimsDir + "/Waving.fbx");
            var clipTalking = LoadAnimationClip(AnimsDir + "/Talking.fbx");
            var clipListening = LoadAnimationClip(AnimsDir + "/HeadNodYes.fbx");
            var clipExtraNod = LoadAnimationClip(AnimsDir + "/SarcasticHeadNod.fbx");

            // Estados
            var stateIdle = rootStateMachine.AddState("Idle", new Vector3(250, 0, 0));
            stateIdle.motion = clipIdle;
            rootStateMachine.defaultState = stateIdle;

            var stateGreeting = rootStateMachine.AddState("Greeting", new Vector3(500, -100, 0));
            stateGreeting.motion = clipGreeting;

            var stateWaving = rootStateMachine.AddState("Waving", new Vector3(500, -20, 0));
            stateWaving.motion = clipWaving;

            var stateListening = rootStateMachine.AddState("Listening", new Vector3(250, 120, 0));
            stateListening.motion = clipListening;

            var stateTalking = rootStateMachine.AddState("Talking", new Vector3(500, 120, 0));
            stateTalking.motion = clipTalking;

            var stateExtraNod = rootStateMachine.AddState("ExtraNod", new Vector3(250, 220, 0));
            stateExtraNod.motion = clipExtraNod;

            // Transiciones: Idle <-> Greeting
            var tToGreet = stateIdle.AddTransition(stateGreeting);
            tToGreet.AddCondition(AnimatorConditionMode.If, 0, "Greeting");
            tToGreet.duration = 0.2f;

            var tGreetToIdle = stateGreeting.AddTransition(stateIdle);
            tGreetToIdle.hasExitTime = true;
            tGreetToIdle.exitTime = 0.95f;
            tGreetToIdle.duration = 0.3f;

            // Transiciones: Idle <-> Waving
            var tToWave = stateIdle.AddTransition(stateWaving);
            tToWave.AddCondition(AnimatorConditionMode.If, 0, "Wave");
            tToWave.duration = 0.2f;

            var tWaveToIdle = stateWaving.AddTransition(stateIdle);
            tWaveToIdle.hasExitTime = true;
            tWaveToIdle.exitTime = 0.95f;
            tWaveToIdle.duration = 0.3f;

            // Transiciones: Idle <-> Listening
            var tToListening = stateIdle.AddTransition(stateListening);
            tToListening.AddCondition(AnimatorConditionMode.If, 0, "IsListening");
            tToListening.duration = 0.25f;

            var tListeningToIdle = stateListening.AddTransition(stateIdle);
            tListeningToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsListening");
            tListeningToIdle.duration = 0.3f;

            // Transiciones: Listening <-> Talking
            var tToTalking = stateListening.AddTransition(stateTalking);
            tToTalking.AddCondition(AnimatorConditionMode.If, 0, "IsTalking");
            tToTalking.duration = 0.25f;

            var tTalkingToListening = stateTalking.AddTransition(stateListening);
            tTalkingToListening.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTalking");
            tTalkingToListening.duration = 0.3f;

            // Transición directa Talking -> Idle si deja de escuchar
            var tTalkingToIdle = stateTalking.AddTransition(stateIdle);
            tTalkingToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsListening");
            tTalkingToIdle.duration = 0.3f;

            // Transición AnyState -> ExtraNod
            var tAnyToExtraNod = rootStateMachine.AddAnyStateTransition(stateExtraNod);
            tAnyToExtraNod.AddCondition(AnimatorConditionMode.If, 0, "ExtraNod");
            tAnyToExtraNod.duration = 0.2f;

            var tExtraNodToIdle = stateExtraNod.AddTransition(stateIdle);
            tExtraNodToIdle.hasExitTime = true;
            tExtraNodToIdle.exitTime = 0.92f;
            tExtraNodToIdle.duration = 0.25f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip LoadAnimationClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static Material EnsureURPMaterial()
        {
            string matPath = MatsDir + "/ZenGuardianKachujin.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null) urpShader = Shader.Find("Standard");

                mat = new Material(urpShader);
                mat.name = "ZenGuardianKachujin";
                mat.color = new Color(0.92f, 0.93f, 0.96f);
                mat.SetFloat("_Smoothness", 0.35f);

                // Asignar textura diffuse si está disponible en TexturesDir
                var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesDir });
                if (textures.Length > 0)
                {
                    var texPath = AssetDatabase.GUIDToAssetPath(textures[0]);
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                    if (tex != null) mat.mainTexture = tex;
                }

                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        private static GameObject CreateOrUpdatePrefab(Avatar avatar, AnimatorController controller, Material mat)
        {
            Debug.Log("[ZenGuardianSetup] 4. Creando Prefab en " + PrefabPath);

            var modelFbx = AssetDatabase.LoadAssetAtPath<GameObject>(MainModelPath);
            if (modelFbx == null)
            {
                Debug.LogError("[ZenGuardianSetup] No se pudo cargar " + MainModelPath);
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelFbx);
            instance.name = "ZenGuardian";

            // Desempaquetar para personalizar componentes
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Animator
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;

            // Aplicar material URP a todos los SkinnedMeshRenderers
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var ren in renderers)
            {
                if (mat != null)
                {
                    ren.sharedMaterial = mat;
                }
            }

            // Collider de Interacción
            var collider = instance.GetComponent<CapsuleCollider>();
            if (collider == null) collider = instance.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, 0.9f, 0);
            collider.height = 1.8f;
            collider.radius = 0.4f;
            collider.isTrigger = true;

            // AudioSource para voz y campanas
            var aSrc = instance.GetComponent<AudioSource>();
            if (aSrc == null) aSrc = instance.AddComponent<AudioSource>();
            aSrc.spatialBlend = 1.0f;
            aSrc.playOnAwake = false;
            aSrc.minDistance = 1.5f;
            aSrc.maxDistance = 15.0f;
            var chime = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/chime.wav");

            // Scripts de Comportamiento
            var guardianCtrl = instance.GetComponent<ZenGuardianController>();
            if (guardianCtrl == null) guardianCtrl = instance.AddComponent<ZenGuardianController>();

            var guardianGaze = instance.GetComponent<ZenGuardianGaze>();
            if (guardianGaze == null) guardianGaze = instance.AddComponent<ZenGuardianGaze>();

            var guardianInteract = instance.GetComponent<ZenGuardianInteraction>();
            if (guardianInteract == null) guardianInteract = instance.AddComponent<ZenGuardianInteraction>();

            // Configurar Serialized Properties de Interaction
            var soInteract = new SerializedObject(guardianInteract);
            soInteract.FindProperty("controller").objectReferenceValue = guardianCtrl;
            soInteract.FindProperty("gaze").objectReferenceValue = guardianGaze;
            soInteract.FindProperty("audioSource").objectReferenceValue = aSrc;
            if (chime != null) soInteract.FindProperty("chimeSound").objectReferenceValue = chime;
            soInteract.ApplyModifiedProperties();

            // Guardar Prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            GameObject.DestroyImmediate(instance);

            Debug.Log($"<color=green>[ZenGuardianSetup] Prefab creado exitosamente en {PrefabPath}</color>");
            return prefab;
        }

        private static void IntegrateIntoActiveScene(GameObject prefab)
        {
            Debug.Log("[ZenGuardianSetup] 5. Integrando ZenGuardian en MainVRScene.unity...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[ZenGuardianSetup] No se pudo abrir la escena " + ScenePath);
                return;
            }

            // Buscar si ya existía un guardián previo y reemplazarlo
            var oldGuardian = GameObject.Find("Guardian del Jardin Zen (Virtual Humanoid)");
            if (oldGuardian != null)
            {
                GameObject.DestroyImmediate(oldGuardian);
            }

            var existingZenGuardian = GameObject.Find("ZenGuardian");
            if (existingZenGuardian != null)
            {
                GameObject.DestroyImmediate(existingZenGuardian);
            }

            // Buscar la zona del santuario Shinto
            var sanctuaryHall = GameObject.Find("Shinto Temple Hall");
            Transform parentTransform = null;
            Vector3 targetPosition = new Vector3(0, 0.8f, 137.5f); // Posición frontal del altar Shinto

            if (sanctuaryHall != null)
            {
                parentTransform = sanctuaryHall.transform.parent;
                targetPosition = sanctuaryHall.transform.position + new Vector3(0, 0.80f, 3.5f);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "ZenGuardian";
            if (parentTransform != null) instance.transform.SetParent(parentTransform, true);
            instance.transform.position = targetPosition;
            instance.transform.rotation = Quaternion.Euler(0, 180f, 0); // Orientado hacia el usuario
            instance.transform.localScale = Vector3.one;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=green>[ZenGuardianSetup] ZenGuardian colocado en {targetPosition} y escena guardada.</color>");
        }
    }
}
