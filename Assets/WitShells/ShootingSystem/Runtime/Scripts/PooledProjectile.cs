using System.Collections;
using UnityEngine;

namespace WitShells.ShootingSystem
{
    public class PooledProjectile : MonoBehaviour
    {
        public Weapon Owner;
        public float LifeTime = 8f;

        private Coroutine _life;

        private void OnEnable()
        {
            if (_life != null) StopCoroutine(_life);
            _life = StartCoroutine(Life());
        }

        private IEnumerator Life()
        {
            yield return new WaitForSeconds(LifeTime);
            if (Owner != null) Owner.ReturnProjectile(gameObject);
            else Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Owner != null) Owner.ReturnProjectile(gameObject);
            else Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Owner != null) Owner.ReturnProjectile(gameObject);
            else Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (_life != null) StopCoroutine(_life);
            _life = null;
        }
    }
}
