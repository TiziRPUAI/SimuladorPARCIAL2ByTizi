using BallisticSimulator.Controller;
using BallisticSimulator.Model;
using BallisticSimulator.Persistence;
using BallisticSimulator.Targets;
using BallisticSimulator.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BallisticSimulator.Editor
{
    /// <summary>
    /// Ensamblador procedural de escena para el Simulador Balístico Avanzado con Telemetría ("Mastering MVC").
    /// Construye el cañón articulado con HingeJoint, la torre de 5 bloques con StructureTarget sobre plataforma elevada,
    /// el LineRenderer predictivo, un Dashboard uGUI profesional Dark Theme (VerticalLayoutGroup, HorizontalLayoutGroup,
    /// Shadow y Outline) y ensambla el SimulationManager automáticamente.
    /// </summary>
    public static class BallisticSimulatorBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/BallisticGenerated";

        // Paleta de colores Dark Theme Militar / Aeroespacial
        private static readonly Color PanelBgColor = new(0.05f, 0.08f, 0.14f, 0.92f);
        private static readonly Color CardBgColor = new(0.08f, 0.13f, 0.21f, 0.94f);
        private static readonly Color SubCardBgColor = new(0.04f, 0.06f, 0.11f, 0.96f);
        private static readonly Color CyanAccentColor = new(0.0f, 0.96f, 0.83f, 1f);
        private static readonly Color SkyCyanColor = new(0.22f, 0.74f, 0.97f, 1f);
        private static readonly Color AmberAccentColor = new(0.98f, 0.65f, 0.15f, 1f);
        private static readonly Color CrimsonFireColor = new(0.90f, 0.18f, 0.28f, 1f);
        private static readonly Color ResetBtnColor = new(0.13f, 0.24f, 0.36f, 1f);
        private static readonly Color TextWhiteColor = new(0.97f, 0.98f, 1.0f, 1f);
        private static readonly Color TextMutedColor = new(0.62f, 0.72f, 0.84f, 1f);
        private static readonly Color OutlineCyanGlow = new(0.0f, 0.85f, 0.95f, 0.32f);

        [MenuItem("TP Final/Generar Simulador Balístico MVC")]
        public static void BuildBallisticSimulatorScene()
        {
            CleanupExistingSimulatorObjects();
            EnsureDirectoryExists(MaterialsFolder);

            // 1. Materiales PBR (URP / Standard)
            Material groundMat = CreateOrUpdateMaterial("Mat_GroundRange", new Color(0.09f, 0.12f, 0.17f), 0.25f, 0.45f);
            Material markerMat = CreateOrUpdateMaterial("Mat_RangeMarker", SkyCyanColor, 0.1f, 0.7f, new Color(0.05f, 0.25f, 0.35f));
            Material cannonBaseMat = CreateOrUpdateMaterial("Mat_CannonBase", new Color(0.16f, 0.19f, 0.25f), 0.82f, 0.68f);
            Material cannonAxisMat = CreateOrUpdateMaterial("Mat_CannonAxis", new Color(0.92f, 0.58f, 0.15f), 0.9f, 0.82f);
            Material cannonBarrelMat = CreateOrUpdateMaterial("Mat_CannonBarrel", new Color(0.25f, 0.31f, 0.39f), 0.88f, 0.78f);
            Material muzzleGlowMat = CreateOrUpdateMaterial("Mat_MuzzleRing", CyanAccentColor, 0.5f, 0.9f, new Color(0.0f, 0.75f, 0.65f));
            Material projectileMat = CreateOrUpdateMaterial("Mat_BallisticProjectile", new Color(1.0f, 0.42f, 0.12f), 0.6f, 0.85f, new Color(0.8f, 0.22f, 0.05f));
            Material lineMat = CreateOrUpdateLineMaterial("Mat_TrajectoryLine");

            Color[] blockColors =
            {
                new(0.94f, 0.27f, 0.27f), // Bloque 1
                new(0.98f, 0.45f, 0.09f), // Bloque 2
                new(0.96f, 0.62f, 0.04f), // Bloque 3
                new(0.92f, 0.78f, 0.12f), // Bloque 4
                new(0.22f, 0.82f, 0.55f)  // Bloque 5
            };

            Material[] blockMaterials = new Material[5];
            for (int i = 0; i < 5; i++)
            {
                blockMaterials[i] = CreateOrUpdateMaterial($"Mat_TowerBlock_{i + 1}", blockColors[i], 0.35f, 0.7f);
            }

            // 2. Entorno, Cámara y Luz
            SetupEnvironmentAndRange(groundMat, markerMat, cannonBaseMat);
            SetupCameraAndLighting();

            // 3. Construcción del Cañón Articulado (Base + Eje + Cañón con HingeJoint + LineRenderer)
            BuildArticulatedCannon(
                cannonBaseMat,
                cannonAxisMat,
                cannonBarrelMat,
                muzzleGlowMat,
                lineMat,
                out Transform barrelPivotTransform,
                out HingeJoint cannonHingeJoint,
                out Transform muzzleTransform,
                out LineRenderer trajectoryLineRenderer);

            // 4. Construcción del Objetivo Destructible (Plataforma elevada + 5 Bloques con StructureTarget y FixedJoints)
            BuildDestructibleTower(
                cannonBaseMat,
                markerMat,
                blockMaterials,
                out StructureTarget[] structureTargets);

            // 5. Construcción del Dashboard uGUI Profesional (Dark Theme, LayoutGroups, Shadow & Outline)
            EnsureEventSystem();
            BuildProfessionalDashboardUI(
                out Canvas _,
                out Slider angleSlider,
                out Slider forceSlider,
                out Slider massSlider,
                out Button fireButton,
                out Button resetTowerButton,
                out Text angleValueText,
                out Text forceValueText,
                out Text massValueText,
                out Text physicsPreviewText,
                out Text telemetryHeaderText,
                out Text telemetryLogText,
                out Text telemetryPathText,
                out ScrollRect telemetryScrollRect);

            // 6. Creación del SimulationManager y Ensamblaje MVC con Inyección de Dependencias
            GameObject simulationManagerObj = new("SimulationManager");
            Undo.RegisterCreatedObjectUndo(simulationManagerObj, "Create SimulationManager");

            BallisticData model = new(angle: 32f, force: 21f, mass: 1.5f, gravity: 9.81f);
            SimulationRepository repository = new("ballistic_telemetry.json");

            BallisticView view = simulationManagerObj.AddComponent<BallisticView>();
            view.InjectViewReferences(
                angleSlider,
                forceSlider,
                massSlider,
                fireButton,
                resetTowerButton,
                angleValueText,
                forceValueText,
                massValueText,
                physicsPreviewText,
                telemetryHeaderText,
                telemetryLogText,
                telemetryPathText,
                telemetryScrollRect,
                barrelPivotTransform,
                trajectoryLineRenderer);

            BallisticController controller = simulationManagerObj.AddComponent<BallisticController>();
            SimulationManager manager = simulationManagerObj.AddComponent<SimulationManager>();

            manager.AssembleMVC(
                model,
                view,
                controller,
                repository,
                cannonHingeJoint,
                muzzleTransform,
                structureTargets,
                projectileMat);

            // Sincronización visual inicial en el Editor
            controller.SyncCannonJointSpring(model.Angle);
            view.InitializeControls(model);
            controller.RefreshTrajectoryPrediction();
            view.DisplayTelemetryHistory(repository.LoadAll(), repository.FilePath);

            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(simulationManagerObj);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Selection.activeGameObject = simulationManagerObj;
            Debug.Log("[TP Final] Simulador Balístico MVC generado exitosamente con StructureTarget, Wobble senoidal amortiguado y Dashboard Dark Theme.");
        }

        private static void CleanupExistingSimulatorObjects()
        {
            string[] generatedNames =
            {
                "BallisticEnvironment",
                "CannonArticulatedSystem",
                "DestructibleTargetTower",
                "BallisticHUDCanvas",
                "SimulationManager"
            };

            foreach (string objName in generatedNames)
            {
                GameObject existing = GameObject.Find(objName);
                while (existing != null)
                {
                    Undo.DestroyObjectImmediate(existing);
                    existing = GameObject.Find(objName);
                }
            }
        }

        private static void SetupEnvironmentAndRange(Material groundMat, Material markerMat, Material trimMat)
        {
            GameObject envRoot = new("BallisticEnvironment");
            Undo.RegisterCreatedObjectUndo(envRoot, "Create Environment");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "TestingRangeFloor";
            floor.transform.SetParent(envRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 15f);
            floor.transform.localScale = new Vector3(24f, 1f, 48f);
            AssignMaterial(floor, groundMat);

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = side < 0 ? "RangeRail_Left" : "RangeRail_Right";
                rail.transform.SetParent(envRoot.transform, false);
                rail.transform.localPosition = new Vector3(side * 4.5f, 0.08f, 15f);
                rail.transform.localScale = new Vector3(0.25f, 0.16f, 42f);
                AssignMaterial(rail, trimMat);
            }

            for (int dist = 5; dist <= 30; dist += 5)
            {
                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"DistanceMarker_{dist}m";
                strip.transform.SetParent(envRoot.transform, false);
                strip.transform.localPosition = new Vector3(0f, 0.01f, dist);
                strip.transform.localScale = new Vector3(8.5f, 0.02f, 0.12f);
                if (strip.TryGetComponent(out Collider col))
                {
                    Object.DestroyImmediate(col);
                }
                AssignMaterial(strip, markerMat);
            }
        }

        private static void SetupCameraAndLighting()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            mainCam.transform.position = new Vector3(27.0f, 6.8f, 9.0f);
            mainCam.transform.LookAt(new Vector3(0f, 2.5f, 9.0f));
            mainCam.fieldOfView = 44f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 1f);

            Light dirLight = Object.FindFirstObjectByType<Light>();
            if (dirLight == null)
            {
                GameObject lightObj = new("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }

            dirLight.transform.rotation = Quaternion.Euler(42f, -125f, 0f);
            dirLight.color = new Color(1f, 0.97f, 0.92f);
            dirLight.intensity = 1.35f;
            dirLight.shadows = LightShadows.Soft;
        }

        private static void BuildArticulatedCannon(
            Material baseMat,
            Material axisMat,
            Material barrelMat,
            Material muzzleMat,
            Material lineMat,
            out Transform barrelPivotTransform,
            out HingeJoint cannonHingeJoint,
            out Transform muzzleTransform,
            out LineRenderer trajectoryLineRenderer)
        {
            GameObject cannonRoot = new("CannonArticulatedSystem");
            Undo.RegisterCreatedObjectUndo(cannonRoot, "Create Articulated Cannon");
            cannonRoot.transform.position = Vector3.zero;

            // A) Base Estática
            GameObject staticBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            staticBase.name = "BaseEstatica";
            staticBase.transform.SetParent(cannonRoot.transform, false);
            staticBase.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            staticBase.transform.localScale = new Vector3(2.2f, 0.7f, 2.4f);
            AssignMaterial(staticBase, baseMat);

            Rigidbody baseRb = staticBase.AddComponent<Rigidbody>();
            baseRb.isKinematic = true;
            baseRb.useGravity = false;

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject cheek = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cheek.name = side < 0 ? "SoporteLateral_Izq" : "SoporteLateral_Der";
                cheek.transform.SetParent(staticBase.transform, false);
                cheek.transform.localPosition = new Vector3(side * 0.34f, 0.95f, 0f);
                cheek.transform.localScale = new Vector3(0.14f, 1.3f, 0.45f);
                if (cheek.TryGetComponent(out Collider cheekCol))
                {
                    Object.DestroyImmediate(cheekCol);
                }
                AssignMaterial(cheek, baseMat);
            }

            // B) Eje de Rotación
            GameObject rotationAxis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rotationAxis.name = "EjeDeRotacion";
            rotationAxis.transform.SetParent(cannonRoot.transform, false);
            rotationAxis.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            rotationAxis.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rotationAxis.transform.localScale = new Vector3(0.28f, 0.95f, 0.28f);
            if (rotationAxis.TryGetComponent(out Collider axisCol))
            {
                axisCol.enabled = false;
            }
            AssignMaterial(rotationAxis, axisMat);

            Rigidbody axisRb = rotationAxis.AddComponent<Rigidbody>();
            axisRb.isKinematic = true;
            axisRb.useGravity = false;

            // C) Cañón Articulado (BarrelPivot) con HingeJoint
            GameObject cannonPivot = new("BarrelPivot");
            cannonPivot.transform.SetParent(cannonRoot.transform, false);
            cannonPivot.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            cannonPivot.transform.localRotation = Quaternion.identity;
            barrelPivotTransform = cannonPivot.transform;

            BoxCollider barrelCollider = cannonPivot.AddComponent<BoxCollider>();
            barrelCollider.center = new Vector3(0f, 0f, 1.1f);
            barrelCollider.size = new Vector3(0.42f, 0.42f, 2.0f);

            Rigidbody cannonRb = cannonPivot.AddComponent<Rigidbody>();
            cannonRb.mass = 12f;
            cannonRb.isKinematic = true;
            cannonRb.useGravity = false;
            cannonRb.linearDamping = 2f;
            cannonRb.angularDamping = 5f;
            cannonRb.interpolation = RigidbodyInterpolation.Interpolate;

            if (staticBase.TryGetComponent(out Collider baseCol))
            {
                Physics.IgnoreCollision(barrelCollider, baseCol, true);
            }

            cannonHingeJoint = cannonPivot.AddComponent<HingeJoint>();
            cannonHingeJoint.connectedBody = axisRb;
            cannonHingeJoint.anchor = Vector3.zero;
            cannonHingeJoint.axis = Vector3.left;

            JointLimits limits = new()
            {
                min = 0f,
                max = 85f
            };
            cannonHingeJoint.limits = limits;
            cannonHingeJoint.useLimits = true;

            JointSpring spring = new()
            {
                spring = 35000f,
                damper = 3500f,
                targetPosition = 32f
            };
            cannonHingeJoint.spring = spring;
            cannonHingeJoint.useSpring = true;

            GameObject breechSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            breechSphere.name = "CulataCanon";
            breechSphere.transform.SetParent(cannonPivot.transform, false);
            breechSphere.transform.localPosition = Vector3.zero;
            breechSphere.transform.localScale = new Vector3(0.68f, 0.68f, 0.68f);
            if (breechSphere.TryGetComponent(out Collider breechCol))
            {
                Object.DestroyImmediate(breechCol);
            }
            AssignMaterial(breechSphere, barrelMat);

            GameObject barrelTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrelTube.name = "TuboCanon";
            barrelTube.transform.SetParent(cannonPivot.transform, false);
            barrelTube.transform.localPosition = new Vector3(0f, 0f, 1.1f);
            barrelTube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrelTube.transform.localScale = new Vector3(0.46f, 1.1f, 0.46f);
            if (barrelTube.TryGetComponent(out Collider tubeCol))
            {
                Object.DestroyImmediate(tubeCol);
            }
            AssignMaterial(barrelTube, barrelMat);

            GameObject muzzleRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            muzzleRing.name = "AnilloBoca";
            muzzleRing.transform.SetParent(cannonPivot.transform, false);
            muzzleRing.transform.localPosition = new Vector3(0f, 0f, 2.18f);
            muzzleRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            muzzleRing.transform.localScale = new Vector3(0.54f, 0.08f, 0.54f);
            if (muzzleRing.TryGetComponent(out Collider ringCol))
            {
                Object.DestroyImmediate(ringCol);
            }
            AssignMaterial(muzzleRing, muzzleMat);

            // Objeto Muzzle estrictamente hijo (child transform) de BarrelPivot
            GameObject muzzleObj = new("Muzzle");
            muzzleObj.transform.SetParent(cannonPivot.transform, false);
            muzzleObj.transform.localPosition = new Vector3(0f, 0f, 2.38f);
            muzzleObj.transform.localRotation = Quaternion.identity;
            muzzleTransform = muzzleObj.transform;

            // D) LineRenderer adjunto al cañón
            trajectoryLineRenderer = cannonPivot.AddComponent<LineRenderer>();
            trajectoryLineRenderer.useWorldSpace = true;
            trajectoryLineRenderer.sharedMaterial = lineMat;
            trajectoryLineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            trajectoryLineRenderer.receiveShadows = false;
            trajectoryLineRenderer.numCapVertices = 6;
            trajectoryLineRenderer.numCornerVertices = 4;
            trajectoryLineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.13f),
                new Keyframe(1f, 0.04f));

            Gradient lineGradient = new();
            lineGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(CyanAccentColor, 0f),
                    new GradientColorKey(SkyCyanColor, 0.5f),
                    new GradientColorKey(new Color(1.0f, 0.45f, 0.15f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.85f, 0.7f),
                    new GradientAlphaKey(0.35f, 1f)
                });
            trajectoryLineRenderer.colorGradient = lineGradient;
        }

        private static void BuildDestructibleTower(
            Material foundationMat,
            Material rimMat,
            Material[] blockMaterials,
            out StructureTarget[] structureTargets)
        {
            const float towerZ = 18f;
            const float platformHeight = 1.6f;
            const float blockHeight = 0.82f;
            // Límite estricto de altura: un bloque sobre la plataforma tiene su centro en y >= 2.01m (o >= 1.95m volcado).
            // Solo si cae físicamente de la plataforma hacia el suelo (y < 1.35m) cuenta como derribado.
            const float strictFallLimitY = 1.35f;

            GameObject towerRoot = new("DestructibleTargetTower");
            Undo.RegisterCreatedObjectUndo(towerRoot, "Create Destructible Tower");
            towerRoot.transform.position = new Vector3(0f, 0f, towerZ);

            // Plataforma elevada estrecha para que los bloques golpeados caigan al vacío
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "TargetPlatform";
            platform.transform.SetParent(towerRoot.transform, false);
            platform.transform.localPosition = new Vector3(0f, platformHeight * 0.5f, 0f);
            platform.transform.localScale = new Vector3(1.85f, platformHeight, 1.25f);
            AssignMaterial(platform, foundationMat);

            GameObject platformRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platformRim.name = "PlatformTopRim";
            platformRim.transform.SetParent(towerRoot.transform, false);
            platformRim.transform.localPosition = new Vector3(0f, platformHeight - 0.04f, 0f);
            platformRim.transform.localScale = new Vector3(1.92f, 0.08f, 1.32f);
            if (platformRim.TryGetComponent(out Collider rimCol))
            {
                Object.DestroyImmediate(rimCol);
            }
            AssignMaterial(platformRim, rimMat);

            Rigidbody platformRb = platform.AddComponent<Rigidbody>();
            platformRb.isKinematic = true;
            platformRb.useGravity = false;

            structureTargets = new StructureTarget[5];
            Rigidbody[] blockRbs = new Rigidbody[5];
            const float baseBreakForce = 24f;
            const float baseBreakTorque = 24f;

            for (int i = 0; i < 5; i++)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"TargetBlock_{i + 1}";
                block.transform.SetParent(towerRoot.transform, false);

                float centerY = platformHeight + (blockHeight * 0.5f) + (i * blockHeight);
                float widthScale = 1.45f - (i * 0.05f);
                block.transform.localPosition = new Vector3(0f, centerY, 0f);
                block.transform.localScale = new Vector3(widthScale, blockHeight, 0.95f);

                if (blockMaterials != null && i < blockMaterials.Length && blockMaterials[i] != null)
                {
                    AssignMaterial(block, blockMaterials[i]);
                }

                Rigidbody rb = block.AddComponent<Rigidbody>();
                rb.mass = 0.65f;
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.12f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                blockRbs[i] = rb;

                Rigidbody connectedBody = i == 0 ? platformRb : blockRbs[i - 1];
                float breakF = Mathf.Max(10f, baseBreakForce - (i * 3f));
                float breakT = Mathf.Max(10f, baseBreakTorque - (i * 3f));

                FixedJoint fixedJoint = block.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = connectedBody;
                fixedJoint.breakForce = breakF;
                fixedJoint.breakTorque = breakT;
                fixedJoint.enableCollision = true;

                StructureTarget target = block.AddComponent<StructureTarget>();
                target.ConfigureTarget(strictFallLimitY, connectedBody, breakF, breakT);
                structureTargets[i] = target;
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject esObj = new("EventSystem");
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void BuildProfessionalDashboardUI(
            out Canvas canvas,
            out Slider angleSlider,
            out Slider forceSlider,
            out Slider massSlider,
            out Button fireButton,
            out Button resetTowerButton,
            out Text angleValueText,
            out Text forceValueText,
            out Text massValueText,
            out Text physicsPreviewText,
            out Text telemetryHeaderText,
            out Text telemetryLogText,
            out Text telemetryPathText,
            out ScrollRect telemetryScrollRect)
        {
            Font uiFont = GetStandardFont();

            GameObject canvasObj = new("BallisticHUDCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Ballistic HUD Canvas");

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // =========================================================================
            // PANEL IZQUIERDO: DASHBOARD DE CONTROL TÁCTICO (VerticalLayoutGroup)
            // =========================================================================
            GameObject leftPanel = CreateStyledPanel(
                "DashboardPanel_Left",
                canvasObj.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(22f, 0f),
                new Vector2(390f, 720f),
                PanelBgColor,
                OutlineCyanGlow);

            VerticalLayoutGroup leftVLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftVLayout.padding = new RectOffset(18, 18, 18, 18);
            leftVLayout.spacing = 12f;
            leftVLayout.childAlignment = TextAnchor.UpperCenter;
            leftVLayout.childControlWidth = true;
            leftVLayout.childControlHeight = true;
            leftVLayout.childForceExpandWidth = true;
            leftVLayout.childForceExpandHeight = false;

            // Encabezado del Dashboard Izquierdo
            GameObject headerCard = CreateLayoutCard("HeaderCard", leftPanel.transform, CardBgColor, 68f, OutlineCyanGlow);
            VerticalLayoutGroup headerLayout = headerCard.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(14, 14, 10, 10);
            headerLayout.spacing = 2f;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            CreateLayoutText(
                "DashboardTitle",
                headerCard.transform,
                uiFont,
                "BALLISTIC COMMAND // MVC",
                19,
                FontStyle.Bold,
                TextWhiteColor,
                TextAnchor.MiddleLeft,
                26f,
                addOutline: true);

            CreateLayoutText(
                "DashboardSubtitle",
                headerCard.transform,
                uiFont,
                "SISTEMA DE CONTROL DE TIRO & WOBBLE DINÁMICO",
                11,
                FontStyle.Bold,
                CyanAccentColor,
                TextAnchor.MiddleLeft,
                18f,
                addOutline: false);

            // Tarjeta 1: Slider de Ángulo
            CreateSliderCard(
                leftPanel.transform,
                uiFont,
                "ÁNGULO DE ELEVACIÓN",
                BallisticData.MinAngle,
                BallisticData.MaxAngle,
                32f,
                CyanAccentColor,
                out angleSlider,
                out angleValueText);

            // Tarjeta 2: Slider de Fuerza
            CreateSliderCard(
                leftPanel.transform,
                uiFont,
                "FUERZA DE IMPULSO",
                BallisticData.MinForce,
                BallisticData.MaxForce,
                21f,
                SkyCyanColor,
                out forceSlider,
                out forceValueText);

            // Tarjeta 3: Slider de Masa
            CreateSliderCard(
                leftPanel.transform,
                uiFont,
                "MASA DEL PROYECTIL",
                BallisticData.MinMass,
                BallisticData.MaxMass,
                1.5f,
                AmberAccentColor,
                out massSlider,
                out massValueText);

            // Tarjeta de Telemetría Predictiva en Tiempo Real
            GameObject previewCard = CreateLayoutCard("KinematicsPreviewCard", leftPanel.transform, SubCardBgColor, 82f, OutlineCyanGlow);
            VerticalLayoutGroup previewLayout = previewCard.AddComponent<VerticalLayoutGroup>();
            previewLayout.padding = new RectOffset(12, 12, 8, 8);
            previewLayout.spacing = 4f;
            previewLayout.childControlWidth = true;
            previewLayout.childControlHeight = true;

            CreateLayoutText(
                "PreviewHeader",
                previewCard.transform,
                uiFont,
                "SOLUCIÓN CINEMÁTICA EN TIEMPO REAL",
                11,
                FontStyle.Bold,
                SkyCyanColor,
                TextAnchor.MiddleCenter,
                16f,
                addOutline: false);

            physicsPreviewText = CreateLayoutText(
                "PhysicsPreviewText",
                previewCard.transform,
                uiFont,
                "Calculando solución balística...",
                12,
                FontStyle.Normal,
                TextWhiteColor,
                TextAnchor.MiddleCenter,
                44f,
                addOutline: false);

            // Botón de Acción Principal: ¡FUEGO!
            fireButton = CreateLayoutButton(
                "FireActionButton",
                leftPanel.transform,
                uiFont,
                "¡ F U E G O !",
                21,
                62f,
                CrimsonFireColor,
                TextWhiteColor,
                new Color(1f, 0.35f, 0.45f, 0.55f));

            // Botón Secundario: Reconstruir Torre
            resetTowerButton = CreateLayoutButton(
                "ResetTowerButton",
                leftPanel.transform,
                uiFont,
                "RECONSTRUIR TORRE OBJETIVO",
                13,
                42f,
                ResetBtnColor,
                CyanAccentColor,
                OutlineCyanGlow);

            // =========================================================================
            // PANEL DERECHO: REGISTRO DE TELEMETRÍA (VerticalLayoutGroup + ScrollRect)
            // =========================================================================
            GameObject rightPanel = CreateStyledPanel(
                "TelemetryPanel_Right",
                canvasObj.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-22f, 0f),
                new Vector2(430f, 720f),
                PanelBgColor,
                OutlineCyanGlow);

            VerticalLayoutGroup rightVLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
            rightVLayout.padding = new RectOffset(18, 18, 18, 18);
            rightVLayout.spacing = 12f;
            rightVLayout.childAlignment = TextAnchor.UpperCenter;
            rightVLayout.childControlWidth = true;
            rightVLayout.childControlHeight = true;
            rightVLayout.childForceExpandWidth = true;
            rightVLayout.childForceExpandHeight = false;

            // Encabezado del Panel de Telemetría
            GameObject rightHeaderCard = CreateLayoutCard("TelemetryHeaderCard", rightPanel.transform, CardBgColor, 62f, OutlineCyanGlow);
            HorizontalLayoutGroup rightHeaderHLayout = rightHeaderCard.AddComponent<HorizontalLayoutGroup>();
            rightHeaderHLayout.padding = new RectOffset(14, 14, 10, 10);
            rightHeaderHLayout.spacing = 8f;
            rightHeaderHLayout.childAlignment = TextAnchor.MiddleLeft;
            rightHeaderHLayout.childControlWidth = true;
            rightHeaderHLayout.childControlHeight = true;
            rightHeaderHLayout.childForceExpandWidth = true;
            rightHeaderHLayout.childForceExpandHeight = true;

            telemetryHeaderText = CreateLayoutText(
                "TelemetryHeaderText",
                rightHeaderCard.transform,
                uiFont,
                "TELEMETRÍA BALÍSTICA  //  REGISTROS: 0",
                15,
                FontStyle.Bold,
                CyanAccentColor,
                TextAnchor.MiddleLeft,
                38f,
                addOutline: true);

            // Viewport ScrollRect del Historial
            GameObject scrollContainer = CreateLayoutCard("TelemetryScrollViewport", rightPanel.transform, SubCardBgColor, 558f, OutlineCyanGlow);
            LayoutElement scrollLayoutElem = scrollContainer.GetComponent<LayoutElement>();
            scrollLayoutElem.flexibleHeight = 1f;

            scrollContainer.AddComponent<RectMask2D>();

            telemetryScrollRect = scrollContainer.AddComponent<ScrollRect>();
            telemetryScrollRect.horizontal = false;
            telemetryScrollRect.vertical = true;
            telemetryScrollRect.movementType = ScrollRect.MovementType.Clamped;
            telemetryScrollRect.scrollSensitivity = 28f;

            GameObject contentObj = new("TelemetryContent");
            contentObj.transform.SetParent(scrollContainer.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 540f);

            VerticalLayoutGroup contentVLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentVLayout.padding = new RectOffset(14, 14, 12, 12);
            contentVLayout.spacing = 6f;
            contentVLayout.childControlWidth = true;
            contentVLayout.childControlHeight = true;
            contentVLayout.childForceExpandWidth = true;
            contentVLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            GameObject logTextObj = new("TelemetryLogText");
            logTextObj.transform.SetParent(contentObj.transform, false);
            telemetryLogText = logTextObj.AddComponent<Text>();
            telemetryLogText.font = uiFont;
            telemetryLogText.fontSize = 13;
            telemetryLogText.lineSpacing = 1.22f;
            telemetryLogText.supportRichText = true;
            telemetryLogText.color = TextWhiteColor;
            telemetryLogText.alignment = TextAnchor.UpperLeft;
            telemetryLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
            telemetryLogText.verticalOverflow = VerticalWrapMode.Overflow;
            AddTextShadow(logTextObj, new Color(0f, 0f, 0f, 0.85f), new Vector2(1f, -1f));

            telemetryScrollRect.viewport = scrollContainer.GetComponent<RectTransform>();
            telemetryScrollRect.content = contentRect;

            // Footer con Ruta de Persistencia JSON
            GameObject footerCard = CreateLayoutCard("TelemetryFooterCard", rightPanel.transform, CardBgColor, 38f, OutlineCyanGlow);
            HorizontalLayoutGroup footerHLayout = footerCard.AddComponent<HorizontalLayoutGroup>();
            footerHLayout.padding = new RectOffset(12, 12, 4, 4);
            footerHLayout.childAlignment = TextAnchor.MiddleLeft;
            footerHLayout.childControlWidth = true;
            footerHLayout.childControlHeight = true;

            telemetryPathText = CreateLayoutText(
                "TelemetryPathFooter",
                footerCard.transform,
                uiFont,
                "JSON: Application.persistentDataPath/ballistic_telemetry.json",
                11,
                FontStyle.Italic,
                TextMutedColor,
                TextAnchor.MiddleLeft,
                26f,
                addOutline: false);
        }

        private static void CreateSliderCard(
            Transform parent,
            Font font,
            string labelTitle,
            float minVal,
            float maxVal,
            float initialVal,
            Color accentColor,
            out Slider slider,
            out Text valueText)
        {
            GameObject cardObj = CreateLayoutCard($"SliderCard_{labelTitle}", parent, CardBgColor, 94f, OutlineCyanGlow);

            VerticalLayoutGroup cardVLayout = cardObj.AddComponent<VerticalLayoutGroup>();
            cardVLayout.padding = new RectOffset(14, 14, 10, 12);
            cardVLayout.spacing = 8f;
            cardVLayout.childControlWidth = true;
            cardVLayout.childControlHeight = true;
            cardVLayout.childForceExpandWidth = true;
            cardVLayout.childForceExpandHeight = false;

            // Fila superior con HorizontalLayoutGroup (Etiqueta a la izquierda, Valor a la derecha)
            GameObject topRow = new("HeaderRow");
            topRow.transform.SetParent(cardObj.transform, false);
            topRow.AddComponent<RectTransform>();
            LayoutElement rowElem = topRow.AddComponent<LayoutElement>();
            rowElem.preferredHeight = 26f;

            HorizontalLayoutGroup topHLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topHLayout.spacing = 8f;
            topHLayout.childAlignment = TextAnchor.MiddleLeft;
            topHLayout.childControlWidth = true;
            topHLayout.childControlHeight = true;
            topHLayout.childForceExpandWidth = true;
            topHLayout.childForceExpandHeight = true;

            CreateLayoutText(
                "ParameterLabel",
                topRow.transform,
                font,
                labelTitle,
                13,
                FontStyle.Bold,
                TextWhiteColor,
                TextAnchor.MiddleLeft,
                24f,
                addOutline: false);

            valueText = CreateLayoutText(
                "ParameterValue",
                topRow.transform,
                font,
                $"{initialVal:F1}",
                15,
                FontStyle.Bold,
                accentColor,
                TextAnchor.MiddleRight,
                24f,
                addOutline: true);

            // Contenedor del Slider
            GameObject sliderObj = new("SliderControl");
            sliderObj.transform.SetParent(cardObj.transform, false);
            sliderObj.AddComponent<RectTransform>();
            LayoutElement sliderElem = sliderObj.AddComponent<LayoutElement>();
            sliderElem.preferredHeight = 30f;

            GameObject bgObj = CreateStyledPanel(
                "Background",
                sliderObj.transform,
                new Vector2(0f, 0.28f),
                new Vector2(1f, 0.72f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                SubCardBgColor,
                new Color(1f, 1f, 1f, 0.08f));

            GameObject fillArea = new("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.28f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.72f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            GameObject fillObj = CreateStyledPanel(
                "Fill",
                fillArea.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                accentColor,
                null);

            GameObject handleArea = new("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handleObj = CreateStyledPanel(
                "Handle",
                handleArea.transform,
                new Vector2(0f, 0.05f),
                new Vector2(0f, 0.95f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(20f, 0f),
                TextWhiteColor,
                accentColor);

            slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillObj.GetComponent<RectTransform>();
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = handleObj.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.value = initialVal;

            _ = bgObj;
        }

        private static GameObject CreateLayoutCard(
            string name,
            Transform parent,
            Color bgColor,
            float preferredHeight,
            Color? outlineColor)
        {
            GameObject cardObj = new(name);
            cardObj.transform.SetParent(parent, false);
            cardObj.AddComponent<RectTransform>();

            Image img = cardObj.AddComponent<Image>();
            img.color = bgColor;

            if (outlineColor.HasValue)
            {
                Outline outline = cardObj.AddComponent<Outline>();
                outline.effectColor = outlineColor.Value;
                outline.effectDistance = new Vector2(1f, -1f);
            }

            Shadow shadow = cardObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(3f, -3f);

            LayoutElement elem = cardObj.AddComponent<LayoutElement>();
            elem.preferredHeight = preferredHeight;

            return cardObj;
        }

        private static Button CreateLayoutButton(
            string name,
            Transform parent,
            Font font,
            string label,
            int fontSize,
            float preferredHeight,
            Color bgColor,
            Color textColor,
            Color outlineColor)
        {
            GameObject btnObj = CreateLayoutCard(name, parent, bgColor, preferredHeight, outlineColor);

            Image img = btnObj.GetComponent<Image>();
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            HorizontalLayoutGroup btnLayout = btnObj.AddComponent<HorizontalLayoutGroup>();
            btnLayout.padding = new RectOffset(10, 10, 4, 4);
            btnLayout.childAlignment = TextAnchor.MiddleCenter;
            btnLayout.childControlWidth = true;
            btnLayout.childControlHeight = true;

            CreateLayoutText(
                "ButtonLabel",
                btnObj.transform,
                font,
                label,
                fontSize,
                FontStyle.Bold,
                textColor,
                TextAnchor.MiddleCenter,
                preferredHeight - 8f,
                addOutline: true);

            return btn;
        }

        private static GameObject CreateStyledPanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color,
            Color? outlineColor)
        {
            GameObject panelObj = new(name);
            panelObj.transform.SetParent(parent, false);

            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image img = panelObj.AddComponent<Image>();
            img.color = color;

            if (outlineColor.HasValue)
            {
                Outline outline = panelObj.AddComponent<Outline>();
                outline.effectColor = outlineColor.Value;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            Shadow shadow = panelObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(4f, -4f);

            return panelObj;
        }

        private static Text CreateLayoutText(
            string name,
            Transform parent,
            Font font,
            string content,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor alignment,
            float preferredHeight,
            bool addOutline)
        {
            GameObject textObj = new(name);
            textObj.transform.SetParent(parent, false);
            textObj.AddComponent<RectTransform>();

            Text txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.supportRichText = true;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            if (addOutline)
            {
                Outline outline = textObj.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            AddTextShadow(textObj, new Color(0f, 0f, 0f, 0.85f), new Vector2(1.5f, -1.5f));

            LayoutElement elem = textObj.AddComponent<LayoutElement>();
            elem.preferredHeight = preferredHeight;

            return txt;
        }

        private static void AddTextShadow(GameObject target, Color shadowColor, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = distance;
        }

        private static Font GetStandardFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
            }
            return font;
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static Material CreateOrUpdateMaterial(
            string matName,
            Color baseColor,
            float metallic,
            float smoothness,
            Color? emissionColor = null)
        {
            string path = $"{MaterialsFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                litShader = Shader.Find("Standard");
            }

            if (mat == null)
            {
                mat = new Material(litShader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (litShader != null && mat.shader != litShader)
            {
                mat.shader = litShader;
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            if (emissionColor.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emissionColor.Value);
                }
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrUpdateLineMaterial(string matName)
        {
            string path = $"{MaterialsFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null)
            {
                lineShader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (mat == null)
            {
                mat = new Material(lineShader);
                AssetDatabase.CreateAsset(mat, path);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AssignMaterial(GameObject target, Material mat)
        {
            if (target == null || mat == null) return;
            if (target.TryGetComponent(out Renderer rend))
            {
                rend.sharedMaterial = mat;
            }
        }
    }
}
