using System.Collections.Generic;
using UnityEngine;


    /// <summary>
    /// Hace aparecer (instancia) piedras pequeñas sobre una mesa al pulsar un botón VR.
    ///
    /// Cómo dispararlo:
    ///   - Botón de UI World-Space:  arrastra este componente al evento OnClick() del Button
    ///                               y selecciona SpawnStone().
    ///   - Mando VR (XRI):           llama SpawnStone() desde un InputAction (botón A/X, gatillo, etc.)
    ///   - XR Grab/Poke Interactable: engancha SpawnStone() a su evento "Select Entered".
    ///
    /// El prefab de la piedra debe tener: MeshRenderer + Collider + Rigidbody.
    /// Para que se pueda agarrar, añádele también XRGrabInteractable.
    /// </summary>
    public class ZenObjectSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [Tooltip("Prefab de la piedra. Necesita Rigidbody + Collider. Idealmente un XRGrabInteractable.")]
        [SerializeField] private GameObject stonePrefab;

        [Header("Punto de aparición")]
        [Tooltip("Transform sobre la mesa donde caerán las piedras. Si está vacío, usa este objeto.")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("Radio horizontal de dispersión para que no caigan todas en el mismo punto.")]
        [SerializeField] private float spawnRadius = 0.10f;
        [Tooltip("Altura sobre el punto desde la que cae la piedra (sensación de 'colocar').")]
        [SerializeField] private float spawnHeight = 0.20f;

        [Header("Variación natural")]
        [Tooltip("Rango de escala aleatoria (1 = tamaño original del prefab).")]
        [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        [Tooltip("Rotación inicial aleatoria para que ninguna piedra se vea idéntica.")]
        [SerializeField] private bool randomRotation = true;

        [Header("Física suave")]
        [Tooltip("Masa de la piedra. Más alta = se siente más pesada y estable al agarrarla.")]
        [SerializeField] private float stoneMass = 1.5f;
        [Tooltip("Drag lineal. Sube un poco para evitar deslizamientos erráticos sobre la mesa.")]
        [SerializeField] private float linearDrag = 0.4f;
        [Tooltip("Drag angular para que dejen de girar pronto y no 'reboten' nerviosas.")]
        [SerializeField] private float angularDrag = 0.6f;

        [Header("Límite")]
        [Tooltip("Máximo de piedras simultáneas. Al superarlo, recicla la más antigua.")]
        [SerializeField] private int maxStones = 20;

        [Header("Audio opcional")]
        [Tooltip("Sonido suave al aparecer cada piedra.")]
        [SerializeField] private AudioSource spawnSfx;

        private readonly Queue<GameObject> _spawned = new Queue<GameObject>();

        private void Awake()
        {
            if (spawnPoint == null) spawnPoint = transform;
        }

        /// <summary>Instancia una piedra. Engánchalo al botón / mando.</summary>
        public void SpawnStone()
        {
            if (stonePrefab == null)
            {
                Debug.LogWarning("[ZenObjectSpawner] No hay 'stonePrefab' asignado.", this);
                return;
            }

            // Reciclar la piedra más antigua si llegamos al límite (mantiene rendimiento estable).
            if (_spawned.Count >= maxStones)
            {
                GameObject oldest = _spawned.Dequeue();
                if (oldest != null) Destroy(oldest);
            }

            // Posición: dentro de un círculo sobre la mesa, a cierta altura.
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = spawnPoint.position
                          + new Vector3(circle.x, spawnHeight, circle.y);

            Quaternion rot = randomRotation ? Random.rotation : spawnPoint.rotation;

            GameObject stone = Instantiate(stonePrefab, pos, rot);

            // Variación de tamaño para que se vean naturales.
            float s = Random.Range(scaleRange.x, scaleRange.y);
            stone.transform.localScale *= s;

            // Ajuste de física para una sensación pesada y suave (no rebotona).
            if (stone.TryGetComponent(out Rigidbody rb))
            {
                rb.mass = stoneMass;
#if UNITY_6000_0_OR_NEWER
                rb.linearDamping = linearDrag;
                rb.angularDamping = angularDrag;
#else
                rb.drag = linearDrag;
                rb.angularDrag = angularDrag;
#endif
                rb.interpolation = RigidbodyInterpolation.Interpolate; // movimiento fluido en VR
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            _spawned.Enqueue(stone);

            if (spawnSfx != null) spawnSfx.Play();
        }

        /// <summary>Elimina todas las piedras (botón "limpiar" opcional).</summary>
        public void ClearAll()
        {
            while (_spawned.Count > 0)
            {
                GameObject s = _spawned.Dequeue();
                if (s != null) Destroy(s);
            }
        }

        // Visualiza el área de aparición en el editor.
        private void OnDrawGizmosSelected()
        {
            Transform p = spawnPoint != null ? spawnPoint : transform;
            Gizmos.color = new Color(0.55f, 0.9f, 0.6f, 0.5f);
            Gizmos.DrawWireSphere(p.position + Vector3.up * spawnHeight, spawnRadius);
        }
    }

