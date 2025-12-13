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
        [SerializeField] private float lifeAfterHit = 0.1f;
        [SerializeField] private float lifeTime = 8f;

        [Header("Impact Effects")]
        [SerializeField] private bool spawnHitEffect = true;
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Events")]
        public UnityEvent<HitInfo> OnHitDetected;

        private void Start()
        {
            // If this projectile is pooled, the pool owner should manage its lifetime.
            // Scheduling a Destroy here can destroy pooled instances while they're returned
            // to the pool. Only schedule automatic destruction for non-pooled projectiles.
            if (TryGetComponent<PooledProjectile>(out var pp) && pp.Owner != null)
                return;

            Destroy(gameObject, lifeTime);
        }

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

                HandleHitEffect(hitInfo);
            }
            // If this projectile is pooled, return it to its owner's pool; otherwise destroy it.
            if (TryGetComponent<PooledProjectile>(out var pp) && pp.Owner != null)
            {
                pp.Owner.ReturnProjectile(gameObject);
            }
            else
            {
                CancelInvoke();
                Destroy(gameObject, lifeAfterHit);
            }

            OnHitDetected.RemoveAllListeners();
        }

        private void HandleHitEffect(HitInfo hitInfo)
        {
            if (spawnHitEffect && hitEffectPrefab != null)
            {
                var effectInstance = Instantiate(hitEffectPrefab, hitInfo.Point, Quaternion.LookRotation(hitInfo.Normal));
                Destroy(effectInstance, 5f); // Cleanup after 5 seconds
            }
        }
    }
}