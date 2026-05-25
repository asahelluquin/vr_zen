using UnityEngine;

    /// <summary>
    /// Guía de respiración 4-7-8 para relajación en VR.
    /// Una esfera flota frente al jugador, escala de tamaño y cambia de color
    /// según la fase del ciclo respiratorio:
    ///   - Inhalar: se agranda durante 4 s   (azul claro -> verde)
    ///   - Mantener: conserva el tamaño 7 s
    ///   - Exhalar: se encoge durante 8 s     (verde -> azul claro)
    ///
    /// Llama a ToggleBreathing() desde un botón en la muñeca (XRI / Input Action).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class BreathingGuide : MonoBehaviour
    {
        public enum BreathPhase { Idle, Inhale, Hold, Exhale }

        [Header("Tiempos del ciclo (segundos)")]
        [SerializeField] private float inhaleDuration = 4f;
        [SerializeField] private float holdDuration   = 7f;
        [SerializeField] private float exhaleDuration  = 8f;

        [Header("Escala de la esfera (en metros de diámetro)")]
        [SerializeField] private float minScale = 0.15f;
        [SerializeField] private float maxScale = 0.45f;

        [Header("Color  (Inhalar -> Exhalar)")]
        [Tooltip("Color al final de la inhalación / durante el hold.")]
        [SerializeField] private Color inhaleColor = new Color(0.60f, 0.85f, 1.00f); // azul claro
        [Tooltip("Color al final de la exhalación / reposo.")]
        [SerializeField] private Color exhaleColor = new Color(0.55f, 0.90f, 0.60f); // verde

        [Range(0f, 4f)]
        [SerializeField] private float emissionIntensity = 1.4f;

        [Header("Seguimiento de la cabeza")]
        [Tooltip("Normalmente la Main Camera del XR Origin. Si se deja vacío, se busca Camera.main.")]
        [SerializeField] private Transform headTransform;
        [SerializeField] private float followDistance = 0.6f;
        [SerializeField] private float verticalOffset = -0.10f;
        [Tooltip("Mayor = sigue más rápido la cabeza. Mantenlo bajo para una sensación calmada.")]
        [SerializeField] private float followSmooth = 2.5f;

        [Header("Comportamiento")]
        [Tooltip("Si está activo, la esfera empieza a respirar al iniciar la escena.")]
        [SerializeField] private bool playOnStart = false;
        [Tooltip("Suaviza el final de exhalación antes de volver a inhalar (segundos).")]
        [SerializeField] private float restBetweenCycles = 0.5f;

        [Header("Audio opcional")]
        [Tooltip("Sonido suave al iniciar la inhalación (sube de tono).")]
        [SerializeField] private AudioSource inhaleSfx;
        [Tooltip("Sonido suave al iniciar la exhalación (baja de tono).")]
        [SerializeField] private AudioSource exhaleSfx;

        // --- Estado interno ---
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _mpb;
        private BreathPhase _phase = BreathPhase.Idle;
        private float _phaseTimer;
        private bool _isActive;

        private static readonly int ColorId    = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public BreathPhase CurrentPhase => _phase;
        public bool IsActive => _isActive;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();

            if (headTransform == null && Camera.main != null)
                headTransform = Camera.main.transform;

            ApplyVisuals(minScale, exhaleColor);
        }

        private void Start()
        {
            if (playOnStart) StartBreathing();
        }

        private void Update()
        {
            FollowHead();

            if (!_isActive) return;

            _phaseTimer += Time.deltaTime;

            switch (_phase)
            {
                case BreathPhase.Inhale: TickInhale(); break;
                case BreathPhase.Hold:   TickHold();   break;
                case BreathPhase.Exhale: TickExhale(); break;
            }
        }

        // ---------- Control público (engánchalo al botón de la muñeca) ----------

        public void StartBreathing()
        {
            _isActive = true;
            EnterPhase(BreathPhase.Inhale);
        }

        public void StopBreathing()
        {
            _isActive = false;
            _phase = BreathPhase.Idle;
        }

        public void ToggleBreathing()
        {
            if (_isActive) StopBreathing();
            else StartBreathing();
        }

        // ---------- Lógica de fases ----------

        private void EnterPhase(BreathPhase next)
        {
            _phase = next;
            _phaseTimer = 0f;

            if (next == BreathPhase.Inhale && inhaleSfx != null) inhaleSfx.Play();
            if (next == BreathPhase.Exhale && exhaleSfx != null) exhaleSfx.Play();
        }

        private void TickInhale()
        {
            // SmoothStep da una aceleración/desaceleración suave (ease-in-out),
            // mucho más natural y relajante que un Lerp lineal.
            float t = Mathf.SmoothStep(0f, 1f, _phaseTimer / inhaleDuration);
            float scale = Mathf.Lerp(minScale, maxScale, t);
            Color color = Color.Lerp(exhaleColor, inhaleColor, t);
            ApplyVisuals(scale, color);

            if (_phaseTimer >= inhaleDuration) EnterPhase(BreathPhase.Hold);
        }

        private void TickHold()
        {
            // Mantiene tamaño y color máximos durante toda la fase.
            ApplyVisuals(maxScale, inhaleColor);

            if (_phaseTimer >= holdDuration) EnterPhase(BreathPhase.Exhale);
        }

        private void TickExhale()
        {
            float t = Mathf.SmoothStep(0f, 1f, _phaseTimer / exhaleDuration);
            float scale = Mathf.Lerp(maxScale, minScale, t);
            Color color = Color.Lerp(inhaleColor, exhaleColor, t);
            ApplyVisuals(scale, color);

            if (_phaseTimer >= exhaleDuration + restBetweenCycles)
                EnterPhase(BreathPhase.Inhale); // reinicia el ciclo
        }

        // ---------- Visuales ----------

        private void ApplyVisuals(float scale, Color color)
        {
            transform.localScale = Vector3.one * scale;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, color);
            // La emisión hace que la esfera "brille" suavemente, ideal con Bloom (URP).
            _mpb.SetColor(EmissionId, color * emissionIntensity);
            _renderer.SetPropertyBlock(_mpb);
        }

        private void FollowHead()
        {
            if (headTransform == null) return;

            // Proyectamos el "frente" de la cabeza al plano horizontal para que la
            // esfera no suba/baje bruscamente al mirar arriba o abajo.
            Vector3 forward = headTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = transform.forward; // evita NaN si mira recto al cielo
            forward.Normalize();

            Vector3 targetPos = headTransform.position
                                + forward * followDistance
                                + Vector3.up * verticalOffset;

            transform.position = Vector3.Lerp(
                transform.position, targetPos, Time.deltaTime * followSmooth);

            // Que la esfera mire siempre al jugador (irrelevante para una esfera lisa,
            // pero útil si más adelante le añades textura o partículas).
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(transform.position - headTransform.position),
                Time.deltaTime * followSmooth);
        }
    }

