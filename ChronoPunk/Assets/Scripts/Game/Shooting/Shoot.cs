using UnityEngine;
using UnityEngine.InputSystem;
using Cronopunk.Movement;


public class Shoot : PausableMonoBehaviour
{
    [SerializeField]
    //Se espera que tenga un animator asociado, puede ser tanto de player como de enemigo siempre y cuando 
    //se use el mismo nombre para el parametro trigger que gestiona el disparo, por convencion usaremos "Shoot"
    private Animator _animator;


    [Header("Referencias")]
    [Tooltip("Prefab de la bala que se disparar�")]
    [SerializeField] GameObject bulletPrefab;
    [Tooltip("Punto de salida de la bala")]
    [SerializeField] Transform firePoint;

    // Propiedad p�blica para acceder al firePoint
    public Transform FirePoint => firePoint;

    [Header("Propiedades del Disparo")]

    [Tooltip("Cadencia de disparo (segundos entre disparos)")]
    [SerializeField] public float fireRate = 0.2f;

    [Header("Sistema de Munición")]
    [Tooltip("¿Puede este objeto recoger munición del mapa?")]
    [SerializeField] private bool canCollectAmmo = true;
    [Tooltip("¿Tiene munición infinita? (ignora el sistema de munición)")]
    [SerializeField] private bool hasInfiniteAmmo = false;
    [Tooltip("Munición actual")]
    [SerializeField] private int currentAmmo = 0;

    [Header("Propiedades de la Bala")]
    [Tooltip("Da�o de la bala")]
    [SerializeField] int bulletDamage = 1;
    [Tooltip("Velocidad de la bala")]
    [SerializeField] float bulletSpeed = 30f;
    [Tooltip("Tiempo de vida de la bala antes de autodestruirse")]
    [SerializeField] float bulletLifetime = 5f;
    [Tooltip("Capas que detectar�n las colisiones de la bala")]
    [SerializeField] LayerMask bulletCollisionLayers;

    private float _fireRateTimer;
    private Vector2 _aimDirection = Vector2.right;
    private KinematicMover _mover;

    // Propiedades públicas para el sistema de munición
    public bool CanCollectAmmo => canCollectAmmo;
    public bool HasInfiniteAmmo => hasInfiniteAmmo;
    public int CurrentAmmo => currentAmmo;
    public bool HasAmmo => hasInfiniteAmmo || currentAmmo > 0;

    private void Awake()
    {
        _mover = GetComponent<KinematicMover>();
    }

    private void FixedUpdate()
    {
        if (IsPaused) return;

        if (_fireRateTimer > 0)
        {
            _fireRateTimer -= Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Cuantiza una dirección de entrada a 8 direcciones (cada 45 grados).
    /// </summary>
    public static Vector2 QuantizeToEightDirections(Vector2 inputDirection, float deadZone = 0.1f)
    {
        if (inputDirection.sqrMagnitude <= deadZone * deadZone)
        {
            return Vector2.zero;
        }

        float angle = Mathf.Atan2(inputDirection.y, inputDirection.x);
        float step = Mathf.PI / 4f;
        float snappedAngle = Mathf.Round(angle / step) * step;

        return new Vector2(Mathf.Cos(snappedAngle), Mathf.Sin(snappedAngle));
    }

    /// <summary>
    /// Establece la direcci�n de apuntado bas�ndose en un vector de entrada.
    /// La rotaci�n del punto de disparo (firePoint) se actualiza con el �ngulo del vector
    /// </summary>
    /// <param name="inputDirection">El vector de direcci�n de entrada, proveniente de un joystick o teclado.</param>
    public void SetAim(Vector2 inputDirection)
    {
        if (IsPaused) return;

        // Solo se actualiza la direcci�n si el input supera un umbral m�nimo para evitar drift.
        if (inputDirection.sqrMagnitude > 0.1f)
        {
            _aimDirection = inputDirection.normalized;

            // Calculamos el �ngulo en grados a partir del vector de direcci�n.
            // Mathf.Atan2 nos da el �ngulo en radianes y lo convertimos a grados con Rad2Deg.
            float angle = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg;

            firePoint.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// "Intenta" un disparo que instanciará un proyectil restará munición si no es infinita.
    /// </summary>
    /// <returns>Si se ha disparado</returns>
    public bool TryShoot()
    {
        if (IsPaused || _fireRateTimer > 0 || !HasAmmo) return false;

        ShootBullet();

        //ANIMACIONES
        if (_animator != null)
        {
            _animator.SetTrigger("Shoot");
        }

        // Consumir munición si no es infinita
        if (!hasInfiniteAmmo)
        {
            currentAmmo--;
        }

        _fireRateTimer = fireRate; // Reinicia el cooldown
        return true;
    }

    private void ShootBullet()
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        // Calcular la posición de disparo usando KinematicMover para determinismo
        // firePoint.localPosition es el offset relativo, lo aplicamos a la posición del mover
        Vector2 spawnPosition;
        if (_mover != null)
        {
            // Usar la posición determinista del KinematicMover + offset local del firePoint
            spawnPosition = _mover.Position + (Vector2)firePoint.localPosition;
        }
        else
        {
            spawnPosition = firePoint.position;
        }

        // Usamos la rotación del firePoint (ya ajustada en OnAim) para instanciar la bala
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, firePoint.rotation);

        // Inicializar BulletBehaviour (si existe)
        if (bullet.TryGetComponent<BulletBehaviour>(out var bb))
        {
            bb.Initialize(gameObject, _aimDirection, bulletDamage, bulletSpeed, bulletLifetime, bulletCollisionLayers);
        }
    }

    /// <summary>
    /// Añade munición al inventario actual.
    /// </summary>
    /// <param name="amount">Cantidad de munición a añadir</param>
    /// <returns>Cantidad realmente añadida</returns>
    public int AddAmmo(int amount)
    {
        if (!canCollectAmmo || hasInfiniteAmmo) return 0;

        currentAmmo += amount;
        return amount;
    }

    /// <summary>
    /// Establece la munición actual directamente (útil para configuración inicial)
    /// </summary>
    /// <param name="amount">Nueva cantidad de munición</param>
    public void SetAmmo(int amount)
    {
        if (hasInfiniteAmmo) return;
        currentAmmo = Mathf.Max(0, amount);
    }
}
