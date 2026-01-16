using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns.Core;

namespace WitShells.ShootingSystem
{
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
    }

    public enum FireMode { Single, Burst, Auto }

    [RequireComponent(typeof(AudioSource))]
    public class Weapon : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField] private FireMode fireMode = FireMode.Single;
        [SerializeField] private bool useProjectile = false;

        [Header("Ballistics")]
        [SerializeField] private float damage = 25f;
        [SerializeField, Tooltip("Spread in degrees")]
        private float spread = 1.5f;
        [SerializeField, Tooltip("Rounds per minute")]
        private float fireRate = 600f;
        [SerializeField] private float range = 100f;
        [SerializeField] private float bulletSpeed = 60f;
        [SerializeField] private int burstCount = 3;

        [Header("Ammo")]
        [SerializeField] private int maxAmmo = 30;
        [SerializeField] private int ammo = 30;
        [SerializeField] private float reloadTime = 2f;
        [SerializeField] private bool autoReload = true;

        [Header("Recoil")]
        [SerializeField] private Transform recoilTransform;
        [SerializeField] private Vector3 recoilKick = new Vector3(0f, 0f, 0.05f);
        [SerializeField] private float recoilReturnSpeed = 8f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private GameObject muzzlePrefab;
        [SerializeField] private AudioClip shootSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private AudioClip weaponClickSound;
        [SerializeField] private Transform muzzleTransform;

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int poolSize = 16;

        [Header("Trajectory Preview")]
        [SerializeField] private Trajectory trajectoryPreview;

        [Header("Layers")]
        [SerializeField] private LayerMask hitMask = ~0;

        // Events
        [Header("Events")]
        public UnityEvent OnShoot;
        public UnityEvent OnReload;
        public UnityEvent<float> OnReloadProgress;
        public UnityEvent<RaycastHit> OnRaycastHit;
        public UnityEvent<Transform> OnProjectileLaunched;

        private int currentAmmo;
        private bool isReloading;
        private float lastFireTime;
        private bool isFiringAuto;

        private ObjectPool<GameObject> pool;

        private AudioSource audioSource;

        public FireMode FireMode => fireMode;
        public int CurrentAmmo => currentAmmo;

        private void Awake()
        {
            currentAmmo = ammo = Mathf.Clamp(ammo, 0, maxAmmo);
            audioSource = GetComponent<AudioSource>();
            if (recoilTransform == null) recoilTransform = transform;

            if (useProjectile && projectilePrefab != null)
            {
                pool = new ObjectPool<GameObject>(() =>
                {
                    var go = Instantiate(projectilePrefab);
                    var pp = go.GetComponent<PooledProjectile>();
                    if (pp == null) pp = go.AddComponent<PooledProjectile>();
                    pp.Owner = this;
                    if (go.TryGetComponent<Rigidbody>(out var rb))
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }
                    go.SetActive(false);
                    return go;
                }, poolSize);
            }
        }

        public bool CanFire()
        {
            if (isReloading) return false;
            if (currentAmmo <= 0)
            {
                if (autoReload)
                {
                    Reload();
                }
                return false;
            }
            float delay = 60f / Mathf.Max(1f, fireRate);
            return Time.time >= lastFireTime + delay;
        }

        public void StartAutoFire()
        {
            if (isFiringAuto) return;
            isFiringAuto = true;
            StartCoroutine(AutoFireCoroutine());
        }

        public void StopAutoFire()
        {
            isFiringAuto = false;
        }

        public void Fire()
        {
            if (fireMode == FireMode.Auto)
            {
                // For auto mode, use StartAutoFire/StopAutoFire
                return;
            }

            // For Single and Burst modes
            if (CanFire())
            {
                StartCoroutine(FireSingleShot());
            }
            else
            {
                // Play weapon click sound when trying to fire but can't
                PlayWeaponClickSound();
            }
        }

        private IEnumerator AutoFireCoroutine()
        {
            while (isFiringAuto)
            {
                if (CanFire())
                {
                    yield return FireSingleShot();
                }
                else yield return null;
            }
        }

        private IEnumerator FireSingleShot()
        {
            if (!CanFire()) yield break;
            lastFireTime = Time.time;

            if (currentAmmo <= 0) yield break;
            currentAmmo--;

            HandleFireLogic();

            OnShoot?.Invoke();
            PlayEffects();
            StartCoroutine(DoRecoil());

            if (fireMode == FireMode.Burst)
            {
                for (int i = 1; i < burstCount; i++)
                {
                    float delay = 60f / Mathf.Max(1f, fireRate);
                    yield return new WaitForSeconds(delay);
                    if (currentAmmo <= 0) break;
                    if (isReloading) break;
                    if (useProjectile) HandleProjectileShoot(); else HandleRaycastShoot();
                    currentAmmo--;
                    OnShoot?.Invoke();
                    PlayEffects();
                    StartCoroutine(DoRecoil());
                }
            }

            yield break;
        }

        private IEnumerator DoRecoil()
        {
            Vector3 start = recoilTransform.localPosition;
            Vector3 target = start - recoilKick;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * recoilReturnSpeed;
                recoilTransform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }
            // return
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * recoilReturnSpeed;
                recoilTransform.localPosition = Vector3.Lerp(target, start, t);
                yield return null;
            }
        }

        private void HandleFireLogic()
        {
            if (useProjectile) HandleProjectileShoot(); else HandleRaycastShoot();
        }

        private void HandleRaycastShoot()
        {
            Vector3 origin = (muzzleTransform != null) ? muzzleTransform.position : transform.position;
            Vector3 dir = GetSpreadDirection((muzzleTransform != null) ? muzzleTransform.forward : transform.forward);
            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask))
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(damage, hit.point, hit.normal);
                OnRaycastHit?.Invoke(hit);
            }
        }

        private GameObject CreateBulletInstance()
        {
            if (projectilePrefab == null) return null;
            var go = Instantiate(projectilePrefab);
            var pp = go.GetComponent<PooledProjectile>();
            if (pp == null) pp = go.AddComponent<PooledProjectile>();
            pp.Owner = this;
            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            go.SetActive(false);
            return go;
        }

        private void HandleProjectileShoot()
        {
            if (projectilePrefab == null) return;
            GameObject go = null;
            go = pool != null ? pool.Get() : CreateBulletInstance();
            if (go == null) return;

            Vector3 spawnPos = (muzzleTransform != null ? muzzleTransform.position : transform.position) + transform.TransformDirection(Vector3.zero);
            if (muzzleTransform != null) spawnPos = muzzleTransform.position;

            go.transform.position = spawnPos;
            go.transform.rotation = (muzzleTransform != null ? muzzleTransform.rotation : transform.rotation);
            go.SetActive(true);

            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                Vector3 dir = GetSpreadDirection((muzzleTransform != null) ? muzzleTransform.forward : transform.forward);
                rb.AddForce(dir * bulletSpeed, ForceMode.VelocityChange);
            }

            OnProjectileLaunched?.Invoke(go.transform);
        }

        /// <summary>
        /// Return a projectile instance back to the pool.
        /// </summary>
        public void ReturnProjectile(GameObject go)
        {
            if (go == null) return;
            if (pool == null)
            {
                Destroy(go);
                return;
            }
            // disable physics and return to pool
            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            go.SetActive(false);
            pool.Release(go);
        }

        private Vector3 GetSpreadDirection(Vector3 forward)
        {
            if (spread <= 0f) return forward.normalized;
            float angle = Random.Range(0f, 360f);
            float magnitude = Random.Range(0f, spread);
            Quaternion q = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f);
            return (q * forward).normalized;
        }

        public void Reload()
        {
            if (isReloading || currentAmmo >= maxAmmo) return;
            StartCoroutine(ReloadCoroutine());
        }

        private IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            OnReload?.Invoke();

            // Play reload sound
            PlayReloadSound();

            float elapsed = 0f;
            OnReloadProgress?.Invoke(0f);

            while (elapsed < reloadTime)
            {
                yield return new WaitForSeconds(0.1f); // Update every 0.1 seconds for smoother progress
                elapsed += 0.1f;
                float progress = elapsed / reloadTime;
                OnReloadProgress?.Invoke(progress);
            }

            OnReloadProgress?.Invoke(1f); // Ensure we end at 100%
            currentAmmo = maxAmmo;
            isReloading = false;
        }

        public void AddAmmo(int amount)
        {
            currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, maxAmmo);
        }

        public void SetTrajectoryEnabled(bool enabled)
        {
            if (trajectoryPreview != null) trajectoryPreview.enabled = enabled;
        }

        private void PlayEffects()
        {
            if (muzzleFlash != null)
                muzzleFlash?.Play();
            else if (muzzlePrefab != null)
            {
                Instantiate(muzzlePrefab, muzzleTransform);
            }

            if (shootSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(shootSound);
            }
        }

        private void PlayReloadSound()
        {
            if (reloadSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
        }

        private void PlayWeaponClickSound()
        {
            if (weaponClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(weaponClickSound);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isFiringAuto = false;
        }


#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 origin = (muzzleTransform != null) ? muzzleTransform.position : transform.position;
            Vector3 forward = (muzzleTransform != null) ? muzzleTransform.forward : transform.forward;
            Gizmos.DrawLine(origin, origin + forward * range);

            // Draw spread cone
            int sampleCount = 10;
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3 dir = GetSpreadDirection(forward);
                Gizmos.DrawLine(origin, origin + dir * range);
            }
        }

#endif
    }
}
