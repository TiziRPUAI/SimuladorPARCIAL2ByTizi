using System;
using BallisticSimulator.Model;
using BallisticSimulator.Persistence;
using BallisticSimulator.Targets;
using BallisticSimulator.View;
using UnityEngine;

namespace BallisticSimulator.Controller
{
    /// <summary>
    /// Composition Root de la arquitectura MVC ("Mastering MVC: Architecture for Ballistic Simulation in Unity").
    /// Ensambla y mantiene las referencias entre Modelo (BallisticData), Vista (BallisticView),
    /// Controlador (BallisticController), Persistencia (SimulationRepository) y Objetivos (StructureTarget).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationManager : MonoBehaviour
    {
        [Header("Tríada MVC & Persistencia")]
        [SerializeField] private BallisticData ballisticData = new(32f, 21f, 1.5f, 9.81f);
        [SerializeField] private BallisticView ballisticView;
        [SerializeField] private BallisticController ballisticController;
        [SerializeField] private SimulationRepository simulationRepository = new("ballistic_telemetry.json");

        [Header("Referencias de Escena Ensambladas")]
        [SerializeField] private HingeJoint cannonHingeJoint;
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private StructureTarget[] structureTargets = Array.Empty<StructureTarget>();
        [SerializeField] private Material projectileMaterial;

        public BallisticData Data => ballisticData;
        public BallisticView View => ballisticView;
        public BallisticController Controller => ballisticController;
        public SimulationRepository Repository => simulationRepository;

        /// <summary>
        /// Ensambla automáticamente todas las dependencias MVC desde el script de Editor.
        /// </summary>
        public void AssembleMVC(
            BallisticData data,
            BallisticView view,
            BallisticController controller,
            SimulationRepository repository,
            HingeJoint hingeJoint,
            Transform muzzle,
            StructureTarget[] targets,
            Material projMaterial)
        {
            ballisticData = data ?? new BallisticData(32f, 21f, 1.5f, 9.81f);
            ballisticView = view;
            ballisticController = controller;
            simulationRepository = repository ?? new SimulationRepository("ballistic_telemetry.json");
            cannonHingeJoint = hingeJoint;
            muzzleTransform = muzzle;
            structureTargets = targets ?? Array.Empty<StructureTarget>();
            projectileMaterial = projMaterial;

            if (ballisticController != null)
            {
                ballisticController.InjectDependencies(
                    ballisticData,
                    ballisticView,
                    simulationRepository,
                    cannonHingeJoint,
                    muzzleTransform,
                    structureTargets,
                    projectileMaterial);
            }
        }

        private void Awake()
        {
            ballisticData ??= new BallisticData(32f, 21f, 1.5f, 9.81f);
            simulationRepository ??= new SimulationRepository("ballistic_telemetry.json");

            if (ballisticView == null)
            {
                ballisticView = GetComponent<BallisticView>() ?? FindFirstObjectByType<BallisticView>();
            }

            if (ballisticController == null)
            {
                ballisticController = GetComponent<BallisticController>() ?? FindFirstObjectByType<BallisticController>();
            }

            if (ballisticController != null)
            {
                ballisticController.InjectDependencies(
                    ballisticData,
                    ballisticView,
                    simulationRepository,
                    cannonHingeJoint,
                    muzzleTransform,
                    structureTargets,
                    projectileMaterial);
            }
        }
    }
}
