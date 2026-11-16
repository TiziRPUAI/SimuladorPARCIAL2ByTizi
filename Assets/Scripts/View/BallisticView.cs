using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BallisticSimulator.Model;
using UnityEngine;
using UnityEngine.UI;

namespace BallisticSimulator.View
{
    /// <summary>
    /// Vista MVC encargada exclusivamente de la interfaz uGUI, la rotación directa del cañón (barrelPivot),
    /// el efecto visual de Retroceso (Recoil) al disparar, el LineRenderer predictivo y el panel de Telemetría.
    /// </summary>
    public sealed class BallisticView : MonoBehaviour
    {
        [Header("Controles uGUI (Sliders y Botones)")]
        [SerializeField] private Slider angleSlider;
        [SerializeField] private Slider forceSlider;
        [SerializeField] private Slider massSlider;
        [SerializeField] private Button fireButton;
        [SerializeField] private Button resetTowerButton;

        [Header("Etiquetas de Parámetros")]
        [SerializeField] private Text angleValueText;
        [SerializeField] private Text forceValueText;
        [SerializeField] private Text massValueText;
        [SerializeField] private Text physicsPreviewText;

        [Header("Panel de Registro de Telemetría")]
        [SerializeField] private Text telemetryHeaderText;
        [SerializeField] private Text telemetryLogText;
        [SerializeField] private Text telemetryPathText;
        [SerializeField] private ScrollRect telemetryScrollRect;

        [Header("Articulación del Cañón y Retroceso (Recoil al Disparar)")]
        [SerializeField] private Transform barrelPivot;
        [Tooltip("Grados que el cañón patea hacia atrás al disparar.")]
        [SerializeField, Range(2f, 20f)] private float recoilKickAngle = 9.5f;
        [Tooltip("Duración del impulso brusco inicial hacia atrás (segundos).")]
        [SerializeField, Range(0.02f, 0.15f)] private float recoilKickDuration = 0.055f;
        [Tooltip("Duración de la recuperación amortiguada al ángulo original (segundos).")]
        [SerializeField, Range(0.2f, 1.5f)] private float recoilRecoveryDuration = 0.55f;
        [Tooltip("Factor de amortiguamiento exponencial en el retorno.")]
        [SerializeField, Range(3f, 16f)] private float recoilDamping = 7.0f;
        [Tooltip("Frecuencia de estabilización amortiguada.")]
        [SerializeField, Range(6f, 30f)] private float recoilFrequency = 14.0f;

        [Header("Visualización de Trayectoria")]
        [SerializeField] private LineRenderer trajectoryLineRenderer;
        [SerializeField, Range(16, 120)] private int trajectoryPointCount = 65;
        [SerializeField, Range(0.02f, 0.15f)] private float simulationTimeStep = 0.055f;

        private Coroutine recoilCoroutine;

        public event Action<float> OnAngleChanged;
        public event Action<float> OnForceChanged;
        public event Action<float> OnMassChanged;
        public event Action OnFireClicked;
        public event Action OnResetTowerClicked;

        public Transform BarrelPivot => barrelPivot;

        /// <summary>
        /// Inyección de dependencias de componentes UI, barrelPivot y LineRenderer desde el Editor Builder.
        /// </summary>
        public void InjectViewReferences(
            Slider angle,
            Slider force,
            Slider mass,
            Button fire,
            Button resetTower,
            Text angleText,
            Text forceText,
            Text massText,
            Text previewText,
            Text headerText,
            Text logText,
            Text pathText,
            ScrollRect scrollRect,
            Transform cannonBarrelPivot,
            LineRenderer lineRenderer)
        {
            angleSlider = angle;
            forceSlider = force;
            massSlider = mass;
            fireButton = fire;
            resetTowerButton = resetTower;
            angleValueText = angleText;
            forceValueText = forceText;
            massValueText = massText;
            physicsPreviewText = previewText;
            telemetryHeaderText = headerText;
            telemetryLogText = logText;
            telemetryPathText = pathText;
            telemetryScrollRect = scrollRect;
            barrelPivot = cannonBarrelPivot;
            trajectoryLineRenderer = lineRenderer;
        }

        private void Awake()
        {
            RegisterUIListeners();
        }

        private void OnDestroy()
        {
            UnregisterUIListeners();
        }

        private void RegisterUIListeners()
        {
            if (angleSlider != null)
            {
                angleSlider.onValueChanged.RemoveListener(HandleAngleSliderChanged);
                angleSlider.onValueChanged.AddListener(HandleAngleSliderChanged);
            }

            if (forceSlider != null)
            {
                forceSlider.onValueChanged.RemoveListener(HandleForceSliderChanged);
                forceSlider.onValueChanged.AddListener(HandleForceSliderChanged);
            }

            if (massSlider != null)
            {
                massSlider.onValueChanged.RemoveListener(HandleMassSliderChanged);
                massSlider.onValueChanged.AddListener(HandleMassSliderChanged);
            }

            if (fireButton != null)
            {
                fireButton.onClick.RemoveListener(HandleFireButtonClicked);
                fireButton.onClick.AddListener(HandleFireButtonClicked);
            }

            if (resetTowerButton != null)
            {
                resetTowerButton.onClick.RemoveListener(HandleResetTowerButtonClicked);
                resetTowerButton.onClick.AddListener(HandleResetTowerButtonClicked);
            }
        }

        private void UnregisterUIListeners()
        {
            if (angleSlider != null) angleSlider.onValueChanged.RemoveListener(HandleAngleSliderChanged);
            if (forceSlider != null) forceSlider.onValueChanged.RemoveListener(HandleForceSliderChanged);
            if (massSlider != null) massSlider.onValueChanged.RemoveListener(HandleMassSliderChanged);
            if (fireButton != null) fireButton.onClick.RemoveListener(HandleFireButtonClicked);
            if (resetTowerButton != null) resetTowerButton.onClick.RemoveListener(HandleResetTowerButtonClicked);
        }

        /// <summary>
        /// Al mover el slider de ángulo, el cañón rota directamente sin tambaleo.
        /// </summary>
        private void HandleAngleSliderChanged(float value)
        {
            SetBarrelAngleImmediate(value);
            OnAngleChanged?.Invoke(value);
        }

        private void HandleForceSliderChanged(float value) => OnForceChanged?.Invoke(value);
        private void HandleMassSliderChanged(float value) => OnMassChanged?.Invoke(value);
        private void HandleFireButtonClicked() => OnFireClicked?.Invoke();
        private void HandleResetTowerButtonClicked() => OnResetTowerClicked?.Invoke();

        /// <summary>
        /// Establece el ángulo del cañón de manera inmediata y detiene cualquier retroceso en curso.
        /// </summary>
        public void SetBarrelAngleImmediate(float targetAngle)
        {
            if (recoilCoroutine != null && Application.isPlaying)
            {
                StopCoroutine(recoilCoroutine);
                recoilCoroutine = null;
            }

            ApplyBarrelPivotAngle(targetAngle);
        }

        /// <summary>
        /// Ejecuta el efecto visual de Retroceso (Recoil) única y exclusivamente al disparar.
        /// Empuja bruscamente el cañón hacia atrás (sumando grados al ángulo actual) y lo devuelve
        /// suavemente al ángulo original dictado por el slider mediante una curva amortiguada.
        /// </summary>
        public void TriggerRecoil(float baseAngle)
        {
            if (barrelPivot == null) return;

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                ApplyBarrelPivotAngle(baseAngle);
                return;
            }

            if (recoilCoroutine != null)
            {
                StopCoroutine(recoilCoroutine);
            }

            recoilCoroutine = StartCoroutine(RecoilBarrelCoroutine(baseAngle));
        }

        private IEnumerator RecoilBarrelCoroutine(float baseAngle)
        {
            // Fase 1: Impulso brusco hacia atrás (sumando grados al ángulo actual)
            float kickElapsed = 0f;
            while (kickElapsed < recoilKickDuration)
            {
                kickElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(kickElapsed / recoilKickDuration);
                float kickEase = Mathf.Sin(t * Mathf.PI * 0.5f);
                ApplyBarrelPivotAngle(baseAngle + (recoilKickAngle * kickEase));
                yield return null;
            }

            // Fase 2: Retorno suave amortiguado al ángulo original del slider
            float recoveryElapsed = 0f;
            while (recoveryElapsed < recoilRecoveryDuration)
            {
                recoveryElapsed += Time.deltaTime;
                float exponentialDecay = Mathf.Exp(-recoilDamping * recoveryElapsed);
                float dampedCurve = Mathf.Cos(recoveryElapsed * recoilFrequency);
                float recoilOffset = recoilKickAngle * exponentialDecay * dampedCurve;

                ApplyBarrelPivotAngle(baseAngle + recoilOffset);
                yield return null;
            }

            ApplyBarrelPivotAngle(baseAngle);
            recoilCoroutine = null;
        }

        private void ApplyBarrelPivotAngle(float elevationDegrees)
        {
            if (barrelPivot == null) return;

            barrelPivot.localRotation = Quaternion.Euler(-elevationDegrees, 0f, 0f);

            if (barrelPivot.TryGetComponent(out HingeJoint joint))
            {
                JointSpring spring = joint.spring;
                spring.targetPosition = elevationDegrees;
                joint.spring = spring;
                joint.useSpring = true;
            }

            Physics.SyncTransforms();
        }

        /// <summary>
        /// Inicializa los rangos y valores de los Sliders sin disparar eventos redundantes.
        /// </summary>
        public void InitializeControls(BallisticData data)
        {
            if (data == null) return;

            if (angleSlider != null)
            {
                angleSlider.minValue = BallisticData.MinAngle;
                angleSlider.maxValue = BallisticData.MaxAngle;
                angleSlider.SetValueWithoutNotify(data.Angle);
            }

            if (forceSlider != null)
            {
                forceSlider.minValue = BallisticData.MinForce;
                forceSlider.maxValue = BallisticData.MaxForce;
                forceSlider.SetValueWithoutNotify(data.Force);
            }

            if (massSlider != null)
            {
                massSlider.minValue = BallisticData.MinMass;
                massSlider.maxValue = BallisticData.MaxMass;
                massSlider.SetValueWithoutNotify(data.Mass);
            }

            SetBarrelAngleImmediate(data.Angle);
            UpdateParameterDisplay(data);
        }

        /// <summary>
        /// Actualiza los textos de los Sliders y el resumen cinemático en tiempo real.
        /// </summary>
        public void UpdateParameterDisplay(BallisticData data)
        {
            if (data == null) return;

            if (angleValueText != null)
            {
                angleValueText.text = $"{data.Angle:F1}°";
            }

            if (forceValueText != null)
            {
                forceValueText.text = $"{data.Force:F1} N·s";
            }

            if (massValueText != null)
            {
                massValueText.text = $"{data.Mass:F2} kg";
            }

            if (physicsPreviewText != null)
            {
                float v0 = data.InitialSpeed;
                float rad = data.Angle * Mathf.Deg2Rad;
                float vy = v0 * Mathf.Sin(rad);
                float vx = v0 * Mathf.Cos(rad);
                float g = data.Gravity;
                float apexHeight = (vy * vy) / (2f * g);
                float estRange = (v0 * v0 * Mathf.Sin(2f * rad)) / g;

                physicsPreviewText.text =
                    $"<color=#38BDF8>v₀:</color> {v0:F1} m/s   |   <color=#38BDF8>vₓ:</color> {vx:F1} m/s   |   <color=#38BDF8>vᵧ:</color> {vy:F1} m/s\n" +
                    $"<color=#38BDF8>Apogeo Δh:</color> +{apexHeight:F1} m   |   <color=#38BDF8>Alcance:</color> ~{estRange:F1} m   |   <color=#38BDF8>g:</color> {g:F2} m/s²";
            }
        }

        /// <summary>
        /// Dibuja la curva parabólica predictiva usando r(t) = r0 + v0*t + 0.5*g*t^2.
        /// </summary>
        public void DrawPredictiveTrajectory(BallisticData data, Vector3 origin, Vector3 launchDirection)
        {
            if (trajectoryLineRenderer == null || data == null) return;

            Vector3 dir = launchDirection.sqrMagnitude > 0.0001f ? launchDirection.normalized : Vector3.forward;
            Vector3 initialVelocity = dir * data.InitialSpeed;
            Vector3 gravityVector = new(0f, -data.Gravity, 0f);

            int maxPoints = Mathf.Max(2, trajectoryPointCount);
            Vector3[] points = new Vector3[maxPoints];
            int validCount = 0;

            Vector3 previousPoint = origin;

            for (int i = 0; i < maxPoints; i++)
            {
                float t = i * simulationTimeStep;
                Vector3 currentPoint = origin + (initialVelocity * t) + (0.5f * gravityVector * (t * t));

                if (i > 0 && currentPoint.y <= 0.02f)
                {
                    float prevY = previousPoint.y;
                    float currY = currentPoint.y;
                    float denom = prevY - currY;
                    float lerpFactor = Mathf.Abs(denom) > 0.0001f ? Mathf.Clamp01((prevY - 0.02f) / denom) : 1f;
                    points[validCount++] = Vector3.Lerp(previousPoint, currentPoint, lerpFactor);
                    break;
                }

                points[validCount++] = currentPoint;
                previousPoint = currentPoint;
            }

            trajectoryLineRenderer.useWorldSpace = true;
            trajectoryLineRenderer.positionCount = validCount;
            for (int i = 0; i < validCount; i++)
            {
                trajectoryLineRenderer.SetPosition(i, points[i]);
            }
        }

        /// <summary>
        /// Actualiza el panel de Registro de Telemetría con el historial completo de simulaciones.
        /// </summary>
        public void DisplayTelemetryHistory(IReadOnlyList<SimulationRecord> records, string storagePath = null, string statusNotice = null)
        {
            int count = records?.Count ?? 0;

            if (telemetryHeaderText != null)
            {
                string statusSuffix = string.IsNullOrWhiteSpace(statusNotice) ? $"REGISTROS: {count}" : statusNotice;
                telemetryHeaderText.text = $"TELEMETRÍA BALÍSTICA  //  {statusSuffix}";
            }

            if (telemetryPathText != null && !string.IsNullOrWhiteSpace(storagePath))
            {
                telemetryPathText.text = $"JSON: {storagePath}";
            }

            if (telemetryLogText == null) return;

            if (records == null || count == 0)
            {
                telemetryLogText.text =
                    "<color=#94A3B8><i>Sin registros en el historial.\n" +
                    "Ajuste Ángulo, Fuerza y Masa y presione [FUEGO] para iniciar telemetría.</i></color>";
                return;
            }

            StringBuilder sb = new(count * 140);
            int maxEntriesToShow = Mathf.Min(count, 40);

            for (int i = count - 1; i >= count - maxEntriesToShow; i--)
            {
                SimulationRecord rec = records[i];
                if (rec == null) continue;

                BallisticData input = rec.InputData;
                float angle = input != null ? input.Angle : 0f;
                float force = input != null ? input.Force : 0f;
                float mass = input != null ? input.Mass : 0f;

                string hitColor = rec.KnockedDownPieces >= 3
                    ? "#34D399"
                    : (rec.KnockedDownPieces > 0 ? "#FBBF24" : "#F87171");

                sb.AppendLine(
                    $"<b><color=#00F5D4>#{rec.Id}</color></b>  <color=#94A3B8>[{rec.Timestamp}]</color>");
                sb.AppendLine(
                    $"  <color=#CBD5E1>Params:</color> <b>{angle:F1}°</b>  |  <b>{force:F1} N·s</b>  |  <b>{mass:F2} kg</b>");
                sb.AppendLine(
                    $"  <color=#CBD5E1>Impacto:</color> <b><color=#F8FAFC>{rec.ImpactDistance:F2} m</color></b>   •   " +
                    $"<color=#CBD5E1>Caídos:</color> <b><color={hitColor}>{rec.KnockedDownPieces}/5 bloques</color></b>");
                sb.AppendLine("<color=#1E293B>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            }

            telemetryLogText.text = sb.ToString();

            if (telemetryScrollRect != null)
            {
                telemetryScrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }
}
