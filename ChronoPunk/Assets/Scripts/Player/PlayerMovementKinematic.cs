using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cronopunk.Movement
{

    #region Game Feel Settings

    [System.Serializable]
    // Ajustes para multiplicador de caida (para caidas mas rapidas)
    public class FallMultiplierSettings
    {
        [Tooltip("Activar/desactivar el multiplicador de caida")]
        public bool enabled = true;

        [Tooltip("Multiplicador de gravedad cuando el jugador esta cayendo")]
        public float fallMultiplier = 2f;
    }

    [System.Serializable]
    // Ajustes para limitar la velocidad maxima de caida
    public class FallControlSettings
    {
        [Tooltip("Activar/desactivar el limitador de velocidad de caida")]
        public bool enabled = true;

        [Tooltip("Limitador de velocidad cuando el jugador esta cayendo")]
        public float maxFallSpeed = 50f;
    }

    [System.Serializable]
    // Ajustes para buffer de salto (detecta input de salto unos ms antes de tocar suelo)
    public class JumpBufferSettings
    {
        [Tooltip("Activar/desactivar el buffer de salto")]
        public bool enabled = true;

        [Tooltip("Tiempo (en segundos) durante el cual se guarda la intencion de salto")]
        public float jumpBufferTime = 0.1f;
    }

    [System.Serializable]
    // Ajustes para salto con fuerza variable (jump cut)
    public class JumpCutSettings
    {
        [Tooltip("Activar/desactivar el salto variable (jump cut)")]
        public bool enabled = true;

        [Tooltip("Multiplicador para reducir velocidad vertical al soltar salto antes del pico")]
        public float jumpCutMultiplier = 0.5f;
    }

    [System.Serializable]
    // Ajustes para coyote jump (permite saltar unos ms despues de salir del suelo)
    public class CoyoteJumpSettings
    {
        [Tooltip("Activar/desactivar el coyote jump")]
        public bool enabled = true;

        [Tooltip("Tiempo (en segundos) durante el cual se permite saltar despues de salir del suelo")]
        public float coyoteTime = 0.1f;
    }

    [System.Serializable]
    // Ajustes para suavizar aceleracion/desaceleracion horizontal
    public class SmoothMovementSettings
    {
        [Tooltip("Activar/desactivar la aceleracion y desaceleracion suaves")]
        public bool enabled = true;

        [Tooltip("Tiempo en segundos para alcanzar la velocidad maxima")]
        public float accelerationTime = 0.1f;

        [Tooltip("Tiempo en segundos para detenerse cuando no hay input")]
        public float decelerationTime = 0.1f;

        [Tooltip("Si esta activo, cambio de direccion instantaneo sin inercia")]
        public bool instantTurn = false;
    }

    #endregion

    /// <summary>
    /// PlayerMovementKinematic maneja:
    /// - Movimiento horizontal con suavizado
    /// - Salto con buffer, coyote time y jump cut
    /// - Multiplicador y limite de caida
    ///
    /// Usa KinematicMover para control determinista:
    /// calculamos la posicion manualmente en FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(KinematicMover))]
    [RequireComponent(typeof(ParticleSystem))]
    public class PlayerMovementKinematic : PausableMonoBehaviour
    {
        
        [SerializeField]
        private Animator _playerAnimator;

        [SerializeField]
        private SpriteRenderer _playerSpriteRenderer;

        [Header("Movimiento")]
        [SerializeField]
        private float _moveSpeed;    // velocidad horizontal maxima

        [SerializeField]
        private float _jumpForce;    // impulso de salto

        [Header("Game Feel Settings")]
        [SerializeField]
        private FallMultiplierSettings _fallMultiplierSettings;

        [SerializeField]
        private FallControlSettings _fallControlSettings;

        [SerializeField]
        private JumpBufferSettings _jumpBufferSettings;

        [SerializeField]
        private JumpCutSettings _jumpCutSettings;

        [SerializeField]
        private CoyoteJumpSettings _coyoteJumpSettings;

        [SerializeField]
        private SmoothMovementSettings _smoothMovementSettings;

        [Header("Ground Detection")]
        [Tooltip("Capas que seran detectadas como suelo")]
        [SerializeField]
        private LayerMask _groundLayers;

        [SerializeField]
        private float _walkSoundTime; //Cada cuanto tiempo de cooldown debe sonar el sonido de andar

        

        // COMPONENTES
        private CapsuleCollider2D _capsuleCollider;
        private KinematicMover _km;
        private ParticleSystem _dustParticles;

        // ESTADO INTERNO
        private float _moveDirection;            // -1 a +1 segun input
        private bool _onGround;                 // si estamos tocando suelo
        private bool _shouldJump;               // flag para saltar en FixedUpdate
        private float _coyoteTimeCounter;        // contador de coyote time
        private float _jumpBufferCounter;        // contador de buffer de salto
        private float _currentSpeed;             // velocidad horizontal actual (suavizada)
        private float _velocitySmoothing;        // aux para SmoothDamp
        private float _kinematicVerticalVelocity;// estimacion vertical manual
        private float _elapsedTimeWalkSound; //tiempo pasado para el calculo del sonido de los andares

        /// <summary>
        /// Indica si el jugador está actualmente en el suelo.
        /// </summary>
        public bool IsOnGround => _onGround;

        Vector2 virtualPosition;

        // Pequeño margen para evitar jitter y penetraciones
        private const float SkinWidth = 0.05f;

        private void Awake()
        {
            // Obtenemos componentes y configuramos Rigidbody kinematico
            _capsuleCollider = GetComponent<CapsuleCollider2D>();
            _km = GetComponent<KinematicMover>();
            _dustParticles = GetComponent<ParticleSystem>();

            _kinematicVerticalVelocity = 0f;
        }

        private void FixedUpdate()
        {
            if (this.IsPaused) return;

            virtualPosition = _km.Position + ResolvePenetrations();

            // 1) Detectar si estamos en suelo para coyote time y buffer
            bool onGroundFrameBefore = _onGround;
            _onGround = CheckDisplacementCast(virtualPosition, SkinWidth, Vector2.down, out _);

            //ANIMACIONES 
            _playerAnimator.SetBool("Grounded", _onGround);

            // Si acabamos de caer de un salto
            if (onGroundFrameBefore != _onGround)
            {
                _dustParticles.Play();

                if (!onGroundFrameBefore)
                {
                    GetComponent<PlayerAudio>().PlayLand();
                }        
            }
                
            // 2) Actualizar coyote time
            if (_onGround)
            {
                _coyoteTimeCounter = _coyoteJumpSettings.enabled
                    ? _coyoteJumpSettings.coyoteTime
                    : 0f;
            }
            else
            {
                _coyoteTimeCounter -= Time.fixedDeltaTime;
            }

            // 3) Reducir contador de salto en buffer
            if (_jumpBufferCounter > 0f)
            {
                _jumpBufferCounter -= Time.fixedDeltaTime;
            }

            // 4) Si hay salto en buffer y estamos aptos, preparamos salto
            if (_jumpBufferSettings.enabled
                && _jumpBufferCounter > 0f
                && (_onGround || (_coyoteJumpSettings.enabled && _coyoteTimeCounter > 0f)))
            {
                _shouldJump = true;
            }

            // 5) CALCULO DE VELOCIDADES
            // Velocidad objetivo segun input
            float targetHorizontalSpeed = _moveDirection * _moveSpeed;

            // Suavizado de aceleracion/desaceleracion
            if (_smoothMovementSettings.enabled)
            {
                _currentSpeed = Mathf.SmoothDamp(
                    _currentSpeed,
                    targetHorizontalSpeed,
                    ref _velocitySmoothing,
                    _moveDirection != 0f
                        ? _smoothMovementSettings.accelerationTime
                        : _smoothMovementSettings.decelerationTime
                );

                // Giro instantaneo si cambia direccion y esta activo
                if (_smoothMovementSettings.instantTurn
                    && _moveDirection != 0f
                    && Mathf.Sign(_moveDirection) != Mathf.Sign(_currentSpeed))
                {
                    _currentSpeed = targetHorizontalSpeed;
                    _velocitySmoothing = 0f;
                    _dustParticles.Play();
                }

                // Dust al cambiar de dirección
                if (_onGround && 
                    _moveDirection != 0f
                    && Mathf.Sign(_moveDirection) != Mathf.Sign(_currentSpeed))
                {
                    _dustParticles.Play();
                }
            }
            else
            {
                _currentSpeed = targetHorizontalSpeed;
            }

            // 6) APLICAR GRAVEDAD MANUAL
            if (_onGround)
            {
                // Reset velocidad vertical si estamos en suelo
                _kinematicVerticalVelocity = 0f;
            }
            else
            {
                // Multiplicador de caida para sensacion mas pesada
                float gravity = Physics2D.gravity.y
                    * (_fallMultiplierSettings.enabled
                        ? _fallMultiplierSettings.fallMultiplier
                        : 1f);

                _kinematicVerticalVelocity += gravity * Time.fixedDeltaTime;
            }

            // 7) SALTO
            if (_shouldJump)
            {
                if(tag == "Player") {
                    GetComponent<PlayerAudio>().PlayJump();
                }
                _dustParticles.Play();
                _kinematicVerticalVelocity = _jumpForce;
                _shouldJump = false;
                _coyoteTimeCounter = 0f;
                _jumpBufferCounter = 0f;
            }

            // 8) LIMITE DE VELOCIDAD DE CAIDA
            if (_fallControlSettings.enabled
                && _kinematicVerticalVelocity < -_fallControlSettings.maxFallSpeed)
            {
                _kinematicVerticalVelocity = -_fallControlSettings.maxFallSpeed;
            }

            // 9) DESPLAZAMIENTO MANUAL

            // 9.1) Desplazamiento horizontal con step-up
            Vector2 horDisp = new Vector2(_currentSpeed * Time.fixedDeltaTime, 0f);
            if (horDisp.x != 0f)
            {
                Vector2 dirH = Vector2.right * Mathf.Sign(horDisp.x);

                if (CheckDisplacementCast(virtualPosition, Mathf.Abs(horDisp.x), dirH, out RaycastHit2D hitH))
                {
                    // Si hay obstaculo, intentamos step-up (subir un escalón)
                    float radius = _capsuleCollider.size.x * 0.5f;
                    Vector2 stepOrigin = virtualPosition + Vector2.up * radius;

                    // Si hay espacio para subir, aplicamos desplazamiento diagonal
                    if (!CheckDisplacementCast(stepOrigin, Mathf.Abs(horDisp.x), dirH, out _) && _kinematicVerticalVelocity <= 0f)
                    {
                        float stepAmount = Mathf.Abs(horDisp.x);
                        virtualPosition += new Vector2(horDisp.x, stepAmount);
                        horDisp.x = 0f; // consumido
                    }
                    else
                    {
                        float allowed = Mathf.Max(0f, hitH.distance - SkinWidth);
                        horDisp.x = Mathf.Sign(horDisp.x) * allowed;
                    }
                }
            }
            virtualPosition += horDisp;

            // 9.2) Desplazamiento vertical
            Vector2 vertDisp = new Vector2(0f, _kinematicVerticalVelocity * Time.fixedDeltaTime);
            if (vertDisp.y != 0f)
            {
                Vector2 dirV = Vector2.up * Mathf.Sign(vertDisp.y);

                if (CheckDisplacementCast(virtualPosition, Mathf.Abs(vertDisp.y), dirV, out RaycastHit2D hitV))
                {
                    float allowed = Mathf.Max(0f, hitV.distance - SkinWidth);
                    vertDisp.y = Mathf.Sign(vertDisp.y) * allowed;
                    _kinematicVerticalVelocity = 0f;
                }
            }
            virtualPosition += vertDisp;

            // 10) Aplicamos posicion final al Rigidbody
            _km.AddMovement(virtualPosition - _km.Position);

            if(_elapsedTimeWalkSound > _walkSoundTime && _moveDirection != 0 && _onGround)
            {
                GetComponent<PlayerAudio>().PlayWalk();
                _elapsedTimeWalkSound = 0;
            }
            else
            {
                _elapsedTimeWalkSound += Time.fixedDeltaTime;
            }
        }

        // Llamar cada update de input para cambiar direccion
        public void SetHorizontalMove(float inputDirection)
        {
            if (IsPaused) return;
            if (inputDirection > 0)
                _moveDirection = 1;
            else if (inputDirection < 0)
                _moveDirection = -1;
            else
                _moveDirection = 0;

            //ANIMACIONES
            _playerAnimator.SetBool("Walking",_moveDirection != 0);

            if (_moveDirection == -1)
            {
                _playerSpriteRenderer.flipX = true;
            }
            else if (_moveDirection == 1) {
                _playerSpriteRenderer.flipX = false;
            }
            //si no hay movimiento el flip se queda como estaba


        }



        // Obtener la dirección actual de movimiento para transferir a otros sistemas
        public float GetCurrentMoveDirection()
        {
            return _moveDirection;
        }

        public bool IsFacingLeft()
        {
            return _playerSpriteRenderer != null && _playerSpriteRenderer.flipX;
        }

        public float GetVerticalVelocity()
        {
            return _kinematicVerticalVelocity;
        }

        public Sprite GetCurrentSprite()
        {
            return _playerSpriteRenderer != null ? _playerSpriteRenderer.sprite : null;
        }

        public void ApplyEndIterationPreviewState(bool facingLeft, float verticalVelocity, Sprite sprite)
        {
            _kinematicVerticalVelocity = verticalVelocity;

            if (_playerSpriteRenderer != null)
            {
                _playerSpriteRenderer.flipX = facingLeft;
                if (sprite != null)
                {
                    _playerSpriteRenderer.sprite = sprite;
                }
            }
        }

        // Parada instantánea en horizontal (ignora suavizado actual)
        public void ForceStopHorizontalImmediately()
        {
            _moveDirection = 0f;
            _currentSpeed = 0f;
            _velocitySmoothing = 0f;

            if (_playerAnimator != null)
            {
                _playerAnimator.SetBool("Walking", false);
            }
        }

        /// <summary>
        /// "Intenta" un salto que se efectuará en el siguiente paso de físicas kinemáticas.
        /// Sólo se salta si estamos en el suelo o en coyote
        /// </summary>
        /// <returns>Si se ha producido el salto</returns>
        public bool TryJump()
        {
            if (IsPaused) return false;
            if (_jumpBufferSettings.enabled)
            {
                // Guardamos intencion de salto en buffer
                _jumpBufferCounter = _jumpBufferSettings.jumpBufferTime;
                return true;
            }
            else if (_onGround || (_coyoteJumpSettings.enabled && _coyoteTimeCounter > 0f))
            {
                // Saltamos de inmediato si estamos en suelo o en coyote time
                _shouldJump = true;
                return true;
            }
            return false;
        }

        // Llamar cuando quieres saltar sí o sí (al salir del globo por ejemplo)
        public void ForceJump()
        {
            if (IsPaused) return;
            _shouldJump = true;
        }

        // Jump cut: si soltamos antes de pico, reducimos velocidad vertical
        public void ApplyJumpCut()
        {
            if (IsPaused) return;
            if (_jumpCutSettings.enabled && _kinematicVerticalVelocity > 0f)
            {
                _kinematicVerticalVelocity *= _jumpCutSettings.jumpCutMultiplier;
            }
        }

        // Metodo helper que lanza un CapsuleCast
        private bool CheckDisplacementCast(Vector2 castOrigin, float distance, Vector2 direction, out RaycastHit2D hit)
        {
            // 1) Prepara los parámetros del cast.
            Vector2 size = _capsuleCollider.size;
            CapsuleDirection2D capsuleDir = _capsuleCollider.direction;
            float angle = transform.eulerAngles.z;
            float castDist = distance + SkinWidth; // Aquí usamos la distancia real a mover, sin el SkinWidth.


            // 2) Ejecutamos el cast múltiple
            RaycastHit2D[] hits = Physics2D.CapsuleCastAll(
                castOrigin,
                size,
                capsuleDir,
                angle,
                direction.normalized,
                castDist,
                _groundLayers
            );

            // 3) Buscamos el primer hit válido y, crucialmente, el más cercano.
            RaycastHit2D? closestValidHit = hits
                .Where(h => h.collider != null && h.collider.gameObject != this.gameObject) // Filtra los hits no válidos
                .OrderBy(h => h.distance)                                                   // Ordena los restantes por distancia (el más cercano primero)
                .Cast<RaycastHit2D?>()                                                      // Convierte a un tipo que puede ser nulo
                .FirstOrDefault();                                                          // Coge el primero (el más cercano) o null si no hay ninguno

            if (closestValidHit.HasValue)
            {
                // Si encontramos un hit, lo asignamos y devolvemos true
                hit = closestValidHit.Value;
                return true;
            }

            // No se encontró nada válido
            hit = default;
            return false;
        }



        private Vector2 ResolvePenetrations()
        {
            Vector2 aggregator = Vector2.zero;
            float maxColliderHeight = 0f;

            // Detectamos todos los colliders que intersectan con nuestro capsule
            Collider2D[] hits = Physics2D.OverlapCapsuleAll(
                _capsuleCollider.bounds.center,
                _capsuleCollider.bounds.size,
                _capsuleCollider.direction,
                0f,
                _groundLayers
            );

            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit == _capsuleCollider) continue;

                // Guardar la altura máxima de los colliders con los que colisionamos (incluyendo escala)
                float colliderHeight = hit.bounds.size.y * hit.transform.lossyScale.y;
                if ((hit.gameObject.CompareTag("Rock") || hit.gameObject.CompareTag("Shadow")) && colliderHeight > maxColliderHeight)  // 4f es una guarrería para qu el sitema solo tenga en cuenta SombrasSolidificadas y rocas, y no el collider del grid.
                {
                    maxColliderHeight = colliderHeight + 0.1f; // un pequeño extra para asegurar que nos separamos completamente
                }

                ColliderDistance2D cd = Physics2D.Distance(_capsuleCollider, hit);
                if (cd.isOverlapped)
                {
                    aggregator += (Vector2)(cd.normal * cd.distance);
                    print("Penetracion resuelta: " + cd.distance);
                }
            }

            // Si detectamos SombrasSolidificadas/rocas (<4f), intentamos primero elevar hacia arriba.
            if (aggregator.magnitude > 0.1f && maxColliderHeight > 0f)
            {
                print(aggregator.magnitude);
                Vector2 upFirst = Vector2.up * maxColliderHeight;
                Vector2 upFirstPosition = virtualPosition + upFirst;

                RaycastHit2D[] upFirstHits = Physics2D.CapsuleCastAll(
                    upFirstPosition,
                    _capsuleCollider.size,
                    _capsuleCollider.direction,
                    0f,
                    Vector2.zero,
                    0f,
                    _groundLayers
                );

                bool upFirstStillColliding = false;
                foreach (RaycastHit2D upFirstHit in upFirstHits)
                {
                    if (upFirstHit.collider != null && upFirstHit.collider != _capsuleCollider)
                    {
                        upFirstStillColliding = true;
                        break;
                    }
                }

                if (!upFirstStillColliding)
                {
                    _kinematicVerticalVelocity = 0f;
                    print("Resolucion de penetracion: elevando player por " + maxColliderHeight + " unidades");
                    return upFirst;
                }
            }

            // Verificar si la resolución nos lleva a otra colisión
            if (aggregator != Vector2.zero)
            {
                Vector2 testPosition = virtualPosition + aggregator;
                
                // Lanzar CapsuleCast para verificar colisiones en la nueva posición
                RaycastHit2D[] testHits = Physics2D.CapsuleCastAll(
                    testPosition,
                    _capsuleCollider.size,
                    _capsuleCollider.direction,
                    0f,
                    Vector2.zero,
                    0f,
                    _groundLayers
                );

                // Verificar si sigue habiendo colisiones (excluyendo el propio collider)
                bool stillColliding = false;
                foreach (RaycastHit2D testHit in testHits)
                {
                    if (testHit.collider != null && testHit.collider != _capsuleCollider)
                    {
                        stillColliding = true;
                        break;
                    }
                }

                // Si sigue colisionando, elevar al player por la altura del collider más alto
                if (stillColliding && maxColliderHeight > 0f)
                {
                    aggregator += Vector2.up * maxColliderHeight;
                    
                    // Resetear velocidad vertical para evitar bugs
                    _kinematicVerticalVelocity = 0f;
                    
                    print("Resolucion de penetracion: elevando player por " + maxColliderHeight + " unidades");
                }
            }

            return aggregator;
        }


        // Gizmos para ver en editor los casts y la forma de la capsula
        private void OnDrawGizmosSelected()
        {
            if (_capsuleCollider == null)
                return;

            // Dibuja collider verde
            Gizmos.color = Color.green;
            DrawWireCapsule(
                virtualPosition + (Vector2)transform.TransformVector(_capsuleCollider.offset),
                _capsuleCollider.size,
                _capsuleCollider.direction,
                transform.eulerAngles.z
            );

            if (!Application.isPlaying)
                return;

            // Dibuja cast horizontal cian
            Vector2 posAfterH = (Vector2)virtualPosition + new Vector2(_currentSpeed * Time.fixedDeltaTime, 0f);
            Gizmos.color = Color.cyan;
            DrawWireCapsule(posAfterH + (Vector2)transform.TransformVector(_capsuleCollider.offset),
                            _capsuleCollider.size,
                            _capsuleCollider.direction,
                            transform.eulerAngles.z);

            // Dibuja cast vertical magenta
            Vector2 posAfterV = posAfterH + new Vector2(0f, _kinematicVerticalVelocity * Time.fixedDeltaTime);
            Gizmos.color = Color.magenta;
            DrawWireCapsule(posAfterV + (Vector2)transform.TransformVector(_capsuleCollider.offset),
                            _capsuleCollider.size,
                            _capsuleCollider.direction,
                            transform.eulerAngles.z);
        }

        // Dibuja alambre de capsula para los Gizmos
        private void DrawWireCapsule(Vector2 center, Vector2 size, CapsuleDirection2D direction, float angle)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 center3 = new Vector3(center.x, center.y, 0f);

            if (direction == CapsuleDirection2D.Vertical)
            {
                float radius = size.x * 0.5f;
                float height = Mathf.Max(0f, size.y - size.x);
                Vector3 point1 = center3 + rotation * new Vector3(0f, height * 0.5f, 0f);
                Vector3 point2 = center3 - rotation * new Vector3(0f, height * 0.5f, 0f);

                Gizmos.DrawWireSphere(point1, radius);
                Gizmos.DrawWireSphere(point2, radius);
                Gizmos.DrawLine(point1 + rotation * Vector3.right * radius,
                                point2 + rotation * Vector3.right * radius);
                Gizmos.DrawLine(point1 - rotation * Vector3.right * radius,
                                point2 - rotation * Vector3.right * radius);
            }
            else
            {
                float radius = size.y * 0.5f;
                float width = Mathf.Max(0f, size.x - size.y);
                Vector3 point1 = center3 + rotation * new Vector3(width * 0.5f, 0f, 0f);
                Vector3 point2 = center3 - rotation * new Vector3(width * 0.5f, 0f, 0f);

                Gizmos.DrawWireSphere(point1, radius);
                Gizmos.DrawWireSphere(point2, radius);
                Gizmos.DrawLine(point1 + rotation * Vector3.up * radius,
                                point2 + rotation * Vector3.up * radius);
                Gizmos.DrawLine(point1 - rotation * Vector3.up * radius,
                                point2 - rotation * Vector3.up * radius);
            }
        }
    }
}
