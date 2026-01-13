using UnityEngine;
using UnityEngine.InputSystem;
using WitShells.ShootingSystem;

public class WeaponTest : MonoBehaviour, IDamageable
{
    [Header("Weapon Reference")]
    [SerializeField] private Weapon weapon;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private InputActionReference reloadAction;
    
    private bool isAutoFiring = false;

    private void Start()
    {
        // Get weapon component if not assigned
        if (weapon == null)
            weapon = GetComponent<Weapon>();
            
        if (weapon == null)
            weapon = GetComponentInChildren<Weapon>();
            
        if (weapon == null)
        {
            Debug.LogError("No Weapon component found! Please assign a weapon or add a Weapon component.");
            return;
        }

        // Validate and enable input actions
        if (shootAction == null)
        {
            Debug.LogError("Shoot Action is not assigned! Please assign an Input Action Reference for shooting.");
            return;
        }
        
        if (reloadAction == null)
        {
            Debug.LogError("Reload Action is not assigned! Please assign an Input Action Reference for reloading.");
            return;
        }

        // Enable input actions
        shootAction.action.Enable();
        shootAction.action.started += OnShootStarted;
        shootAction.action.canceled += OnShootCanceled;
        
        reloadAction.action.Enable();
        reloadAction.action.performed += OnReloadPerformed;
        
        // Subscribe to weapon events
        weapon.OnReloadProgress.AddListener(OnReloadProgressTest);
    }

    private void OnDestroy()
    {
        // Unsubscribe from weapon events
        if (weapon != null)
        {
            weapon.OnReloadProgress.RemoveListener(OnReloadProgressTest);
        }
        
        // Disable input actions
        if (shootAction != null && shootAction.action != null)
        {
            shootAction.action.started -= OnShootStarted;
            shootAction.action.canceled -= OnShootCanceled;
            shootAction.action.Disable();
        }
        
        if (reloadAction != null && reloadAction.action != null)
        {
            reloadAction.action.performed -= OnReloadPerformed;
            reloadAction.action.Disable();
        }
    }

    private void OnShootStarted(InputAction.CallbackContext context)
    {
        if (weapon == null) return;
        Debug.Log("Shooting weapon...");

        // Handle different fire modes
        FireMode currentFireMode = GetWeaponFireMode();
        
        switch (currentFireMode)
        {
            case FireMode.Single:
                // Single shot - fire once on button press
                weapon.Fire();
                break;
                
            case FireMode.Burst:
                // Burst fire - fire burst on button press
                weapon.Fire();
                break;
                
            case FireMode.Auto:
                // Auto fire - start continuous firing
                if (!isAutoFiring)
                {
                    isAutoFiring = true;
                    weapon.StartAutoFire();
                }
                break;
        }
    }

    private void OnShootCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Stopped shooting.");
        if (weapon == null) return;

        // Stop auto fire when button is released
        if (isAutoFiring)
        {
            isAutoFiring = false;
            weapon.StopAutoFire();
        }
    }

    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        if (weapon == null) return;
        
        weapon.Reload();
        Debug.Log("Reloading weapon...");
    }

    // Helper method to get fire mode using reflection since it's private
    private FireMode GetWeaponFireMode()
    {
        // Default to Single if we can't access the fire mode
        // In a real scenario, you might want to expose this in the Weapon class
        return FireMode.Single;
    }



    public void OnReloadProgressTest(float progress)
    {
        // Convert to percentage for cleaner display
        float percentage = progress * 100f;
        Debug.Log($"Reload Progress: {percentage:F1}%");
        
        // You can add UI updates here, for example:
        // UpdateReloadProgressBar(progress);
    }

    public void RayCastEventTest(RaycastHit hitInfo)
    {
        Debug.Log("RayCastEventTest triggered. Hit object: " + hitInfo.collider.name);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        Debug.Log($"TakeDamage called. Amount: {amount}, HitPoint: {hitPoint}, HitNormal: {hitNormal}");
    }
}
