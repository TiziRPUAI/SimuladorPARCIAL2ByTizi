using UnityEngine;

namespace BallisticSimulator.Targets
{
    /// <summary>
    /// Componente físico para cada pieza de la estructura objetivo.
    /// Evalúa el derribo única y estrictamente cuando el bloque cae fuera de la plataforma
    /// por debajo del umbral de altura (transform.position.y < fallLimitY).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class StructureTarget : MonoBehaviour
    {
        [Header("Validación Estricta de Caída")]
        [Tooltip("Altura límite en Y. El bloque solo cuenta como derribado si cae por debajo de este valor.")]
        [SerializeField] private float fallLimitY = 1.25f;

        [Header("Configuración de Articulación Frágil")]
        [SerializeField] private Rigidbody connectedAnchorBody;
        [SerializeField] private float jointBreakForce = 22f;
        [SerializeField] private float jointBreakTorque = 22f;

        private Rigidbody cachedRigidbody;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool initialStateCached;

        public float FallLimitY
        {
            get => fallLimitY;
            set => fallLimitY = value;
        }

        /// <summary>
        /// Validación estricta: únicamente se considera derribado si cayó físicamente de la plataforma
        /// superando el límite inferior de altura. Volcaduras o desplazamientos sobre la plataforma no cuentan.
        /// </summary>
        public bool IsKnockedDown => transform.position.y < fallLimitY;

        public void ConfigureTarget(
            float heightLimitY,
            Rigidbody anchorBody,
            float breakForce,
            float breakTorque)
        {
            fallLimitY = heightLimitY;
            connectedAnchorBody = anchorBody;
            jointBreakForce = Mathf.Max(1f, breakForce);
            jointBreakTorque = Mathf.Max(1f, breakTorque);
            CacheInitialTransform();
        }

        private void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody>();
            CacheInitialTransform();
        }

        public void CacheInitialTransform()
        {
            if (cachedRigidbody == null)
            {
                cachedRigidbody = GetComponent<Rigidbody>();
            }

            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialStateCached = true;
        }

        /// <summary>
        /// Limpia las uniones previas y recoloca el bloque en su pose original sobre la plataforma.
        /// </summary>
        public void RestoreTransformAndClearJoints()
        {
            if (!initialStateCached)
            {
                CacheInitialTransform();
            }

            FixedJoint[] existingJoints = GetComponents<FixedJoint>();
            foreach (FixedJoint joint in existingJoints)
            {
                if (joint != null)
                {
                    DestroyImmediate(joint);
                }
            }

            if (cachedRigidbody != null)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
            }

            transform.SetPositionAndRotation(initialPosition, initialRotation);
        }

        /// <summary>
        /// Reconstruye el FixedJoint fino tras haber restaurado todos los bloques de la torre.
        /// </summary>
        public void RebuildFineFixedJoint()
        {
            if (connectedAnchorBody == null) return;

            FixedJoint joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = connectedAnchorBody;
            joint.breakForce = jointBreakForce;
            joint.breakTorque = jointBreakTorque;
            joint.enableCollision = true;
        }
    }
}
