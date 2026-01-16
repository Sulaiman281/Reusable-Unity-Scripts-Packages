using UnityEngine;
using UnityEngine.InputSystem;

namespace WitShells.ShootingSystem
{
    public class WeaponInput : MonoBehaviour
    {
        [Header("Weapon Reference")]
        [SerializeField] private Weapon weapon;

        [Header("Runtime Input Reference")]
        [SerializeField] private InputActionReference shootAction;
        [SerializeField] private InputActionReference reloadAction;

        private void OnEnable()
        {
            if (weapon == null)
                weapon = GetComponent<Weapon>();

            if (weapon == null)
                weapon = GetComponentInChildren<Weapon>();

            if (weapon == null)
            {
                Debug.LogError("No Weapon component found! Please assign a weapon or add a Weapon component.");
                return;
            }

            if (shootAction != null)
            {
                shootAction.action.Enable();
                shootAction.action.started += OnShootStarted;
                shootAction.action.canceled += OnShootCanceled;
            }
            else
            {
                Debug.LogError("Shoot Action is not assigned! Please assign an Input Action Reference for shooting.");
            }

            if (reloadAction != null)
            {
                reloadAction.action.Enable();
                reloadAction.action.performed += OnReloadPerformed;
            }
            else
            {
                Debug.LogError("Reload Action is not assigned! Please assign an Input Action Reference for reloading.");
            }
        }

        private void OnDisable()
        {
            if (shootAction != null)
            {
                shootAction.action.started -= OnShootStarted;
                shootAction.action.canceled -= OnShootCanceled;
                shootAction.action.Disable();
            }

            if (reloadAction != null)
            {
                reloadAction.action.performed -= OnReloadPerformed;
                reloadAction.action.Disable();
            }
        }


        private void OnShootStarted(InputAction.CallbackContext context)
        {
            switch (weapon.FireMode)
            {
                case FireMode.Auto:
                    weapon.StartAutoFire();
                    break;
                case FireMode.Burst:
                    weapon.Fire();
                    break;
                case FireMode.Single:
                    weapon.Fire();
                    break;
            }
        }

        private void OnShootCanceled(InputAction.CallbackContext context)
        {
            if (weapon.FireMode == FireMode.Auto)
            {
                weapon.StopAutoFire();
            }
        }

        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            weapon.Reload();
        }
    }
}