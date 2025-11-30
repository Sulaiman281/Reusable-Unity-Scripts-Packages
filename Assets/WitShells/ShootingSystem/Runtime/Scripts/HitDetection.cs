using UnityEngine;
using UnityEngine.Events;

namespace WitShells.ShootingSystem
{
    [RequireComponent(typeof(Collider))]
    public class HitDetection : MonoBehaviour
    {
        [Header("Hit Detection Settings")]
        [SerializeField] private float detectionRadius = 0.5f;
        [SerializeField] private LayerMask hitLayers;

        [Header("Events")]
        public UnityEvent<HitInfo> OnHitDetected;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & hitLayers) != 0)
            {
                ContactPoint contact = collision.contacts[0];
                HitInfo hitInfo = new HitInfo
                {
                    Point = contact.point,
                    Normal = contact.normal,
                    HitObject = collision.gameObject
                };
                OnHitDetected?.Invoke(hitInfo);
            }
            // If this projectile is pooled, return it to its owner's pool; otherwise destroy it.
            if (TryGetComponent<PooledProjectile>(out var pp) && pp.Owner != null)
            {
                pp.Owner.ReturnProjectile(gameObject);
            }
            else
            {
                Destroy(gameObject, 0.5f);
            }
        }
    }
}