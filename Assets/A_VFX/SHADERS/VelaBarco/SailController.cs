using UnityEngine;

// Le pasa a las velas (shader VelaWave) cuánto se está moviendo el barco.
// Ponelo en la raíz de cualquier barco (el tuyo o los enemigos).
//  - Velocidad: si el barco tiene Movement (el tuyo) usa SpeedRatio (sprint = 1).
//    Si no (enemigos), la mide sola por cuánto se movió el transform.
//  - Giro: lo mide por cuánto rotó en Y.
// Cada barco tiene su propia fase, así las velas no se mueven todas iguales.
public class SailController : MonoBehaviour
{
    [Tooltip("Velas a controlar. Vacío = busca en los hijos los renderers con material VelaWave.")]
    [SerializeField] private Renderer[] _sails;

    [Header("Velocidad (enemigos / barcos sin Movement)")]
    [Tooltip("Velocidad (m/s) a la que la vela queda inflada al máximo")]
    [SerializeField] private float _fullSpeed = 10f;

    [Header("Giro")]
    [Tooltip("Velocidad de giro (grados/seg) a la que la vela se tuerce al máximo")]
    [SerializeField] private float _fullTurnRate = 90f;

    [Header("Suavizado")]
    [Tooltip("Qué tan rápido se infla/desinfla (más alto = más rápido)")]
    [SerializeField] private float _inflateResponse = 2.5f;
    [SerializeField] private float _turnResponse = 4f;

    [Header("Debug")]
    [Tooltip("Loguea las velas encontradas y su tamaño al arrancar")]
    [SerializeField] private bool _logSizes = true;

    private static readonly int SpeedID = Shader.PropertyToID("_SailSpeed");
    private static readonly int TurnID = Shader.PropertyToID("_SailTurn");
    private static readonly int PhaseID = Shader.PropertyToID("_SailPhase");
    private static readonly int SizeID = Shader.PropertyToID("_SailSize");
    private static readonly int AxisUID = Shader.PropertyToID("_SailAxisU");
    private static readonly int AxisVID = Shader.PropertyToID("_SailAxisV");
    private static readonly int AxisNID = Shader.PropertyToID("_SailAxisN");
    private static readonly int BMinID = Shader.PropertyToID("_SailBoundsMin");
    private static readonly int BMaxID = Shader.PropertyToID("_SailBoundsMax");

    private Movement _movement;
    private MaterialPropertyBlock _mpb;
    private float[] _sizes;
    private Vector4[] _axisU, _axisV, _axisN, _bMin, _bMax;
    private float _phase;
    private float _speed;
    private float _turn;
    private Vector3 _lastPos;
    private float _lastYaw;

    private void Awake()
    {
        _movement = GetComponentInParent<Movement>();
        if (_movement == null) _movement = GetComponentInChildren<Movement>();
        _mpb = new MaterialPropertyBlock();
        _phase = Random.Range(0f, 100f);

        if (_sails == null || _sails.Length == 0)
        {
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m != null && m.shader != null && m.shader.name.Contains("VelaWave"))
                    list.Add(r);
            }
            _sails = list.ToArray();
            if (_sails.Length == 0)
                Debug.LogWarning($"[SailController] {name}: no encontré velas con el shader VelaWave.", this);
        }

        // Medimos cada vela: tamaño y qué eje de la malla es vertical / horizontal / grosor.
        // Así el shader fija los bordes de arriba y abajo por posición real (no depende del UV).
        int n = _sails.Length;
        _sizes = new float[n];
        _axisU = new Vector4[n]; _axisV = new Vector4[n]; _axisN = new Vector4[n];
        _bMin = new Vector4[n]; _bMax = new Vector4[n];
        for (int i = 0; i < n; i++)
        {
            var r = _sails[i];
            Mesh mesh = null;
            if (r is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;   // velas que venían con Cloth
            else if (r != null)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
            }
            if (mesh == null)
            {
                Debug.LogWarning($"[SailController] {name}: no encontré la malla de {r?.name}.", this);
                _sizes[i] = 1f;
                continue;
            }

            Bounds b = mesh.bounds;
            Vector3 ext = b.size;
            int thin = 0;
            for (int k = 1; k < 3; k++) if (ext[k] < ext[thin]) thin = k;
            int a = (thin + 1) % 3, c = (thin + 2) % 3;

            // el eje vertical es el que en el mundo apunta más hacia arriba
            float upA = Mathf.Abs(Vector3.Dot(r.transform.TransformDirection(Axis(a)).normalized, Vector3.up));
            float upC = Mathf.Abs(Vector3.Dot(r.transform.TransformDirection(Axis(c)).normalized, Vector3.up));
            int vert = upA >= upC ? a : c;
            int hor = vert == a ? c : a;

            _axisV[i] = Axis(vert);
            _axisU[i] = Axis(hor);
            _axisN[i] = Axis(thin);
            _bMin[i] = b.min;
            _bMax[i] = b.max;
            _sizes[i] = Mathf.Max(0.0001f, Mathf.Max(ext[vert], ext[hor]));

            if (_logSizes)
                Debug.Log($"[SailController] {name} -> vela '{r.name}' tamaño {_sizes[i]:F2} | vertical {"XYZ"[vert]}, horizontal {"XYZ"[hor]}, infla en {"XYZ"[thin]}", this);
        }
    }

    private static Vector3 Axis(int i) => i == 0 ? Vector3.right : (i == 1 ? Vector3.up : Vector3.forward);

    private void OnEnable()
    {
        _lastPos = transform.position;
        _lastYaw = transform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // pausa

        // velocidad 0..1
        float rawSpeed;
        if (_movement != null && _movement.enabled)
            rawSpeed = _movement.SpeedRatio;
        else
        {
            Vector3 delta = transform.position - _lastPos;
            delta.y = 0f;
            rawSpeed = Mathf.Clamp01((delta.magnitude / dt) / Mathf.Max(0.01f, _fullSpeed));
        }

        // giro -1..1
        float yaw = transform.eulerAngles.y;
        float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / dt;
        float rawTurn = Mathf.Clamp(yawRate / Mathf.Max(1f, _fullTurnRate), -1f, 1f);

        _lastPos = transform.position;
        _lastYaw = yaw;

        _speed = Mathf.Lerp(_speed, rawSpeed, 1f - Mathf.Exp(-_inflateResponse * dt));
        _turn = Mathf.Lerp(_turn, rawTurn, 1f - Mathf.Exp(-_turnResponse * dt));

        for (int i = 0; i < _sails.Length; i++)
        {
            var r = _sails[i];
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(SpeedID, _speed);
            _mpb.SetFloat(TurnID, _turn);
            _mpb.SetFloat(PhaseID, _phase + i * 1.7f);
            _mpb.SetFloat(SizeID, _sizes[i]);
            _mpb.SetVector(AxisUID, _axisU[i]);
            _mpb.SetVector(AxisVID, _axisV[i]);
            _mpb.SetVector(AxisNID, _axisN[i]);
            _mpb.SetVector(BMinID, _bMin[i]);
            _mpb.SetVector(BMaxID, _bMax[i]);
            r.SetPropertyBlock(_mpb);
        }
    }
}
