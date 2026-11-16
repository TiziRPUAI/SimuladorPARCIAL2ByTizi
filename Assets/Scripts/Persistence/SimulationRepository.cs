using System;
using System.Collections.Generic;
using System.IO;
using BallisticSimulator.Model;
using UnityEngine;

namespace BallisticSimulator.Persistence
{
    /// <summary>
    /// Repositorio de persistencia local en JSON dentro de Application.persistentDataPath.
    /// Gestiona la lectura y escritura de un arreglo de SimulationRecord para telemetría histórica.
    /// </summary>
    [Serializable]
    public sealed class SimulationRepository
    {
        private const string DefaultFileName = "ballistic_telemetry.json";

        [SerializeField] private string fileName = DefaultFileName;

        private readonly List<SimulationRecord> cachedRecords = new();
        private bool isLoaded;

        public string FilePath
        {
            get
            {
                string safeName = string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName.Trim();
                return Path.Combine(Application.persistentDataPath, safeName);
            }
        }

        public SimulationRepository() : this(DefaultFileName)
        {
        }

        public SimulationRepository(string customFileName)
        {
            fileName = string.IsNullOrWhiteSpace(customFileName) ? DefaultFileName : customFileName.Trim();
        }

        /// <summary>
        /// Carga todos los registros de telemetría almacenados en disco.
        /// </summary>
        public IReadOnlyList<SimulationRecord> LoadAll()
        {
            cachedRecords.Clear();
            string path = FilePath;

            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        SimulationRecordCollection collection = JsonUtility.FromJson<SimulationRecordCollection>(json);
                        if (collection?.records != null)
                        {
                            foreach (SimulationRecord record in collection.records)
                            {
                                if (record != null)
                                {
                                    cachedRecords.Add(record);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SimulationRepository] No se pudo leer el archivo de telemetría en '{path}': {ex.Message}");
            }

            isLoaded = true;
            return cachedRecords;
        }

        /// <summary>
        /// Agrega un nuevo registro de simulación y persiste el arreglo completo en formato JSON.
        /// </summary>
        public void SaveRecord(SimulationRecord record)
        {
            if (record == null)
            {
                Debug.LogWarning("[SimulationRepository] Se intentó guardar un SimulationRecord nulo.");
                return;
            }

            if (!isLoaded)
            {
                LoadAll();
            }

            cachedRecords.Add(record);
            PersistToDisk();
        }

        /// <summary>
        /// Obtiene el historial actual en memoria (cargándolo del disco si aún no fue inicializado).
        /// </summary>
        public IReadOnlyList<SimulationRecord> GetRecords()
        {
            if (!isLoaded)
            {
                return LoadAll();
            }

            return cachedRecords;
        }

        private void PersistToDisk()
        {
            string path = FilePath;

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                SimulationRecordCollection wrapper = new()
                {
                    records = cachedRecords.ToArray()
                };

                string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SimulationRepository] Error al guardar telemetría en '{path}': {ex.Message}");
            }
        }
    }
}
