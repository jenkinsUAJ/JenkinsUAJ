using UnityEngine;
using System.Collections.Generic;

namespace Cronopunk.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KinematicMover : PausableMonoBehaviour
    {
        private Rigidbody2D _rb;
        private Vector2 accumulatedDelta = Vector2.zero;


        public Vector2 LastDelta { get; private set; } = Vector2.zero;
        public Vector2 Position => _rb.position;
        public float Rotation => _rb.rotation;
        public Vector2 Velocity => LastDelta / Time.fixedDeltaTime;


        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.gravityScale = 0f;
        }

        private void FixedUpdate() 
        {
            if (this.IsPaused) return;

            // Aplica el movimiento acumulado al Rigidbody
            if (accumulatedDelta != Vector2.zero) {
                _rb.MovePosition(_rb.position + accumulatedDelta);
            }

            // Guardamos el delta aplicado para poder consultarlo
            LastDelta = accumulatedDelta;

            // Resetea el delta para el siguiente FixedUpdate
            accumulatedDelta = Vector2.zero;
        }

        // Este m�todo es p�blico para que otros scripts lo puedan llamar
        public void AddMovement(Vector2 delta) {
            accumulatedDelta += delta;
        }

        public void SetPosition(Vector2 newPosition) {
            _rb.position = newPosition;
        }
    }
}
