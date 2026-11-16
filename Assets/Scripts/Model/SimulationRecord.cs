using System;
using UnityEngine;

namespace BallisticSimulator.Model
{
    /// <summary>
    /// Registro individual de telemetría de una simulación balística ejecutada.
    /// Clase de datos pura serializable a JSON (sin MonoBehaviour).
    /// </summary>
    [Serializable]
    public sealed class SimulationRecord
    {
        [SerializeField] private string id;
        [SerializeField] private string timestamp;
        [SerializeField] private BallisticData inputData;
        [SerializeField] private float impactDistance;
        [SerializeField] private int knockedDownPieces;

        public string Id => id ?? string.Empty;
        public string Timestamp => timestamp ?? string.Empty;
        public BallisticData InputData => inputData;
        public float ImpactDistance => impactDistance;
        public int KnockedDownPieces => knockedDownPieces;

        public SimulationRecord()
        {
            id = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            inputData = new BallisticData();
            impactDistance = 0f;
            knockedDownPieces = 0;
        }

        public SimulationRecord(BallisticData sourceData, float distance, int piecesDown)
        {
            id = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            inputData = sourceData != null ? sourceData.Clone() : new BallisticData();
            impactDistance = Mathf.Max(0f, distance);
            knockedDownPieces = Mathf.Max(0, piecesDown);
        }
    }

    /// <summary>
    /// Contenedor raíz para serializar un arreglo de registros mediante JsonUtility.
    /// </summary>
    [Serializable]
    public sealed class SimulationRecordCollection
    {
        public SimulationRecord[] records = Array.Empty<SimulationRecord>();
    }
}
