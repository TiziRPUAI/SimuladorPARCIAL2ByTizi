using System;
using UnityEngine;

namespace BallisticSimulator.Model
{
    /// <summary>
    /// Modelo de datos puro que almacena los parámetros físicos de entrada para la simulación balística.
    /// No hereda de MonoBehaviour para respetar estrictamente el patrón MVC.
    /// </summary>
    [Serializable]
    public sealed class BallisticData
    {
        public const float MinAngle = 5f;
        public const float MaxAngle = 80f;
        public const float MinForce = 5f;
        public const float MaxForce = 60f;
        public const float MinMass = 0.5f;
        public const float MaxMass = 5f;
        public const float DefaultGravity = 9.81f;

        [SerializeField] private float angle = 30f;
        [SerializeField] private float force = 20f;
        [SerializeField] private float mass = 1.5f;
        [SerializeField] private float gravity = DefaultGravity;

        public float Angle
        {
            get => angle;
            set => angle = Mathf.Clamp(value, MinAngle, MaxAngle);
        }

        public float Force
        {
            get => force;
            set => force = Mathf.Clamp(value, MinForce, MaxForce);
        }

        public float Mass
        {
            get => Mathf.Max(0.01f, mass);
            set => mass = Mathf.Clamp(value, MinMass, MaxMass);
        }

        public float Gravity
        {
            get => Mathf.Max(0.01f, gravity);
            set => gravity = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// Rapidez inicial calculada por impulso instantáneo (v0 = J / m).
        /// </summary>
        public float InitialSpeed => Force / Mass;

        public BallisticData()
        {
            angle = 30f;
            force = 20f;
            mass = 1.5f;
            gravity = DefaultGravity;
        }

        public BallisticData(float angle, float force, float mass, float gravity = DefaultGravity)
        {
            Angle = angle;
            Force = force;
            Mass = mass;
            Gravity = gravity;
        }

        /// <summary>
        /// Crea una copia profunda inmutable de los datos en el instante del disparo.
        /// </summary>
        public BallisticData Clone()
        {
            return new BallisticData(Angle, Force, Mass, Gravity);
        }
    }
}
