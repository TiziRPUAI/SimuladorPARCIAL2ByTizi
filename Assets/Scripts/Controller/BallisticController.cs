using System;
using System.Collections;
using System.Collections.Generic;
using BallisticSimulator.Model;
using BallisticSimulator.Persistence;
using BallisticSimulator.Targets;
using BallisticSimulator.View;
using UnityEngine;

namespace BallisticSimulator.Controller
{
    /// <summary>
    /// Controlador MVC central. Recibe los eventos de BallisticView, actualiza el modelo BallisticData,
    /// instancia proyectiles usando estrictamente muzzleTransform.position y muzzleTransform.forward,
    /// dispara el efecto de Retroceso (Recoil) en la Vista al hacer fuego y registra la telemetría.
    /// </summary>
    public sealed class BallisticController : MonoBehaviour
    {
        [Header("Arquitectura MVC & Persistencia")]
        [SerializeField] private BallisticData model = new();
        [SerializeField] private BallisticView view;
        [SerializeField] private SimulationRepository repository = new();

        [Header("Referencias Físicas del Cañón")]
        [SerializeField] private HingeJoint cannonHingeJoint;
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private Material projectileMaterial;

        [Header("Estructura Objetivo (5 Bloques con StructureTarget)")]
        [SerializeField] private StructureTarget[] structureTargets = Array.Empty<StructureTarget>();
        [SerializeField, Range(0.8f, 3.0f)] private float impactEvaluationDelay = 1.35f;

        private readonly List<GameObject> activeProjectiles = new();
        private bool eventsSubscribed;

        public BallisticData Model => model;
        public BallisticView View => view;
        public SimulationRepository Repository => repository;

        /// <summary>
        /// Inyección de dependencias invocada por SimulationManager / BallisticSimulatorBuilder.
        /// </summary>
        public void InjectDependencies(
            BallisticData injectedModel,
            BallisticView injectedView,
            SimulationRepository injectedRepository,
            HingeJoint injectedHingeJoint,
            Transform injectedMuzzle,
            StructureTarget[] injectedTargets,
            Material injectedProjectileMaterial)
        {
            model = injectedModel ?? new BallisticData();
            view = injectedView;
            repository = injectedRepository ?? new SimulationRepository();
            cannonHingeJoint = injectedHingeJoint;
            muzzleTransform = injectedMuzzle;
            structureTargets = injectedTargets ?? Array.Empty<StructureTarget>();
            projectileMaterial = injectedProjectileMaterial;
        }

        private void Awake()
        {
            model ??= new BallisticData();
            repository ??= new SimulationRepository();

            if (view == null)
            {
                view = FindFirstObjectByType<BallisticView>();
            }

            Physics.gravity = new Vector3(0f, -model.Gravity, 0f);
        }

        private void OnEnable()
        {
            SubscribeToViewEvents();
        }

        private void Start()
        {
            SubscribeToViewEvents();
            SyncCannonJointSpring(model.Angle);

            if (view != null)
            {
                view.InitializeControls(model);
                RefreshTrajectoryPrediction();

                IReadOnlyList<SimulationRecord> history = repository.LoadAll();
                view.DisplayTelemetryHistory(history, repository.FilePath);
            }
        }

        private void Update()
        {
            RefreshTrajectoryPrediction();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewEvents();
        }

        private void SubscribeToViewEvents()
        {
            if (eventsSubscribed || view == null) return;

            view.OnAngleChanged += HandleAngleChanged;
            view.OnForceChanged += HandleForceChanged;
            view.OnMassChanged += HandleMassChanged;
            view.OnFireClicked += HandleFireRequested;
            view.OnResetTowerClicked += HandleResetTowerRequested;
            eventsSubscribed = true;
        }

        private void UnsubscribeFromViewEvents()
        {
            if (!eventsSubscribed || view == null) return;

            view.OnAngleChanged -= HandleAngleChanged;
            view.OnForceChanged -= HandleForceChanged;
            view.OnMassChanged -= HandleMassChanged;
            view.OnFireClicked -= HandleFireRequested;
            view.OnResetTowerClicked -= HandleResetTowerRequested;
            eventsSubscribed = false;
        }

        private void HandleAngleChanged(float newAngle)
        {
            if (model == null) return;

            model.Angle = newAngle;
            SyncCannonJointSpring(model.Angle);

            if (view != null)
            {
                view.SetBarrelAngleImmediate(model.Angle);
                view.UpdateParameterDisplay(model);
                RefreshTrajectoryPrediction();
            }
        }

        private void HandleForceChanged(float newForce)
        {
            if (model == null) return;

            model.Force = newForce;

            if (view != null)
            {
                view.UpdateParameterDisplay(model);
                RefreshTrajectoryPrediction();
            }
        }

        private void HandleMassChanged(float newMass)
        {
            if (model == null) return;

            model.Mass = newMass;

            if (view != null)
            {
                view.UpdateParameterDisplay(model);
                RefreshTrajectoryPrediction();
            }
        }

        /// <summary>
        /// Sincroniza los límites y el resorte físico del HingeJoint con el ángulo del modelo.
        /// </summary>
        public void SyncCannonJointSpring(float elevationDegrees)
        {
            if (cannonHingeJoint == null) return;

            JointLimits limits = cannonHingeJoint.limits;
            limits.min = 0f;
            limits.max = 85f;
            cannonHingeJoint.limits = limits;
            cannonHingeJoint.useLimits = true;

            JointSpring spring = cannonHingeJoint.spring;
            spring.spring = 35000f;
            spring.damper = 3500f;
            spring.targetPosition = elevationDegrees;
            cannonHingeJoint.spring = spring;
            cannonHingeJoint.useSpring = true;
        }

        /// <summary>
        /// Actualiza la línea de predicción directamente desde la posición y vector forward del Muzzle.
        /// </summary>
        public void RefreshTrajectoryPrediction()
        {
            if (view == null || model == null || muzzleTransform == null) return;

            view.DrawPredictiveTrajectory(model, muzzleTransform.position, muzzleTransform.forward);
        }

        private void HandleFireRequested()
        {
            if (model == null || muzzleTransform == null) return;

            if (CountKnockedDownTargets() >= 5)
            {
                RebuildTargetTower();
            }

            // 1. Aseguramos que el cañón esté en el ángulo exacto del slider antes de leer muzzleTransform
            if (view != null)
            {
                view.SetBarrelAngleImmediate(model.Angle);
            }
            SyncCannonJointSpring(model.Angle);
            Physics.SyncTransforms();

            CleanupExcessProjectiles();

            // 2. Obtenemos directamente posición, rotación y dirección física desde el objeto Muzzle hijo del cañón
            Vector3 spawnOrigin = muzzleTransform.position;
            Vector3 launchDirection = muzzleTransform.forward;
            Quaternion launchRotation = muzzleTransform.rotation;
            BallisticData shotDataSnapshot = model.Clone();

            GameObject projectileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObj.name = $"Projectile_{DateTime.Now:HHmmss}";
            projectileObj.transform.SetPositionAndRotation(spawnOrigin, launchRotation);
            projectileObj.transform.localScale = Vector3.one * 0.44f;

            if (projectileObj.TryGetComponent(out Renderer rend))
            {
                if (projectileMaterial != null)
                {
                    rend.sharedMaterial = projectileMaterial;
                }
                else
                {
                    rend.material.color = new Color(1f, 0.45f, 0.1f, 1f);
                }
            }

            if (cannonHingeJoint != null && projectileObj.TryGetComponent(out Collider projCollider))
            {
                Collider[] cannonColliders = cannonHingeJoint.transform.root.GetComponentsInChildren<Collider>();
                foreach (Collider col in cannonColliders)
                {
                    if (col != null)
                    {
                        Physics.IgnoreCollision(projCollider, col, true);
                    }
                }
            }

            Rigidbody rb = projectileObj.AddComponent<Rigidbody>();
            rb.mass = shotDataSnapshot.Mass;
            rb.useGravity = true;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            ConfigureProjectileTrail(projectileObj);

            ProjectileImpactDetector detector = projectileObj.AddComponent<ProjectileImpactDetector>();
            detector.Initialize(shotDataSnapshot, spawnOrigin, HandleProjectileImpacted);

            // 3. Impulso aplicado estrictamente en la dirección muzzle.forward
            rb.AddForce(launchDirection * shotDataSnapshot.Force, ForceMode.Impulse);

            activeProjectiles.Add(projectileObj);

            // 4. Disparamos el efecto visual de Retroceso (Recoil) única y exclusivamente al hacer fuego
            if (view != null)
            {
                view.TriggerRecoil(model.Angle);

                if (repository != null)
                {
                    view.DisplayTelemetryHistory(
                        repository.GetRecords(),
                        repository.FilePath,
                        "PROYECTIL EN VUELO...");
                }
            }
        }

        private static void ConfigureProjectileTrail(GameObject projectileObj)
        {
            if (projectileObj == null) return;

            TrailRenderer trail = projectileObj.AddComponent<TrailRenderer>();
            trail.time = 1.8f;
            trail.minVertexDistance = 0.08f;
            trail.startWidth = 0.18f;
            trail.endWidth = 0.0f;

            Shader trailShader = Shader.Find("Sprites/Default");
            if (trailShader != null)
            {
                trail.material = new Material(trailShader);
            }

            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.0f, 0.96f, 0.83f), 0f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.15f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;
        }

        private void HandleProjectileImpacted(BallisticData shotSnapshot, Vector3 launchOrigin, Vector3 impactPoint)
        {
            float distance = Vector3.Distance(launchOrigin, impactPoint);
            StartCoroutine(RecordTelemetryAfterImpactSettles(shotSnapshot, distance));
        }

        private IEnumerator RecordTelemetryAfterImpactSettles(BallisticData shotSnapshot, float impactDistance)
        {
            if (view != null && repository != null)
            {
                view.DisplayTelemetryHistory(
                    repository.GetRecords(),
                    repository.FilePath,
                    $"IMPACTO A {impactDistance:F2}m • VERIFICANDO CAÍDAS...");
            }

            yield return new WaitForSeconds(impactEvaluationDelay);

            int knockedDown = CountKnockedDownTargets();
            SimulationRecord record = new(shotSnapshot, impactDistance, knockedDown);

            if (repository != null)
            {
                repository.SaveRecord(record);

                if (view != null)
                {
                    IReadOnlyList<SimulationRecord> updatedRecords = repository.GetRecords();
                    view.DisplayTelemetryHistory(
                        updatedRecords,
                        repository.FilePath,
                        $"IMPACTO: {impactDistance:F2}m | CAÍDOS: {knockedDown}/5");
                }
            }
        }

        /// <summary>
        /// Evalúa cuántos bloques cayeron estrictamente fuera de la plataforma (transform.position.y < fallLimitY).
        /// </summary>
        public int CountKnockedDownTargets()
        {
            if (structureTargets == null || structureTargets.Length == 0) return 0;

            int fallenCount = 0;
            foreach (StructureTarget target in structureTargets)
            {
                if (target != null && target.IsKnockedDown)
                {
                    fallenCount++;
                }
            }

            return fallenCount;
        }

        private void HandleResetTowerRequested()
        {
            RebuildTargetTower();

            if (view != null && repository != null)
            {
                view.DisplayTelemetryHistory(
                    repository.GetRecords(),
                    repository.FilePath,
                    "TORRE DE 5 BLOQUES RECONSTRUIDA");
            }
        }

        /// <summary>
        /// Restaura los 5 StructureTarget a su posición original sobre la plataforma y regenera sus FixedJoints.
        /// </summary>
        public void RebuildTargetTower()
        {
            if (structureTargets == null || structureTargets.Length == 0) return;

            foreach (StructureTarget target in structureTargets)
            {
                if (target != null)
                {
                    target.RestoreTransformAndClearJoints();
                }
            }

            Physics.SyncTransforms();

            foreach (StructureTarget target in structureTargets)
            {
                if (target != null)
                {
                    target.RebuildFineFixedJoint();
                }
            }
        }

        private void CleanupExcessProjectiles()
        {
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                if (activeProjectiles[i] == null)
                {
                    activeProjectiles.RemoveAt(i);
                }
            }

            while (activeProjectiles.Count >= 6)
            {
                GameObject oldest = activeProjectiles[0];
                activeProjectiles.RemoveAt(0);
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }
        }
    }

    /// <summary>
    /// Componente auxiliar adjunto dinámicamente al proyectil para detectar el primer impacto válido
    /// y reportar los datos de colisión al BallisticController.
    /// </summary>
    public sealed class ProjectileImpactDetector : MonoBehaviour
    {
        private BallisticData shotSnapshot;
        private Vector3 launchOrigin;
        private Action<BallisticData, Vector3, Vector3> onImpactCallback;
        private bool hasImpacted;

        public void Initialize(
            BallisticData snapshot,
            Vector3 origin,
            Action<BallisticData, Vector3, Vector3> impactCallback)
        {
            shotSnapshot = snapshot;
            launchOrigin = origin;
            onImpactCallback = impactCallback;
            hasImpacted = false;

            Destroy(gameObject, 14f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasImpacted || collision == null) return;

            hasImpacted = true;

            Vector3 hitPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

            onImpactCallback?.Invoke(shotSnapshot, launchOrigin, hitPoint);
        }
    }
}
