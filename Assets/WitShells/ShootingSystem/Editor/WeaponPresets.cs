using UnityEngine;

namespace WitShells.ShootingSystem
{
    [System.Serializable]
    public class WeaponPreset
    {
        public string name;
        [TextArea(2, 4)]
        public string description;
        
        // Ballistics
        public float damage = 25f;
        public float spread = 1.5f;
        public float fireRate = 600f;
        public float range = 100f;
        public float bulletSpeed = 60f;
        public int burstCount = 3;
        public FireMode fireMode = FireMode.Single;
        
        // Ammo
        public int maxAmmo = 30;
        public float reloadTime = 2f;
        
        // Recoil
        public Vector3 recoilKick = new Vector3(0f, 0f, 0.05f);
        public float recoilReturnSpeed = 8f;

        public WeaponPreset(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }

    public static class WeaponPresets
    {
        public static WeaponPreset[] GetPresets()
        {
            return new WeaponPreset[]
            {
                new WeaponPreset("Assault Rifle", "High fire rate, moderate damage, suitable for medium range combat")
                {
                    fireMode = FireMode.Auto,
                    damage = 30f,
                    spread = 2.0f,
                    fireRate = 700f,
                    range = 150f,
                    bulletSpeed = 80f,
                    maxAmmo = 30,
                    reloadTime = 2.5f,
                    recoilKick = new Vector3(0f, 0.1f, 0.08f),
                    recoilReturnSpeed = 6f
                },
                
                new WeaponPreset("Sniper Rifle", "High damage, high accuracy, long range, slow fire rate")
                {
                    fireMode = FireMode.Single,
                    damage = 100f,
                    spread = 0.2f,
                    fireRate = 60f,
                    range = 500f,
                    bulletSpeed = 150f,
                    maxAmmo = 5,
                    reloadTime = 3.5f,
                    recoilKick = new Vector3(0f, 0.3f, 0.2f),
                    recoilReturnSpeed = 4f
                },
                
                new WeaponPreset("SMG", "Very high fire rate, low damage, close range combat")
                {
                    fireMode = FireMode.Auto,
                    damage = 18f,
                    spread = 3.5f,
                    fireRate = 900f,
                    range = 50f,
                    bulletSpeed = 60f,
                    maxAmmo = 40,
                    reloadTime = 1.8f,
                    recoilKick = new Vector3(0f, 0.05f, 0.04f),
                    recoilReturnSpeed = 10f
                },
                
                new WeaponPreset("Shotgun", "High damage, very short range, wide spread")
                {
                    fireMode = FireMode.Single,
                    damage = 80f,
                    spread = 15f,
                    fireRate = 120f,
                    range = 25f,
                    bulletSpeed = 40f,
                    maxAmmo = 8,
                    reloadTime = 4f,
                    recoilKick = new Vector3(0f, 0.2f, 0.15f),
                    recoilReturnSpeed = 5f
                },
                
                new WeaponPreset("Burst Rifle", "3-round burst, balanced stats for tactical gameplay")
                {
                    fireMode = FireMode.Burst,
                    damage = 35f,
                    spread = 1.2f,
                    fireRate = 800f,
                    range = 200f,
                    bulletSpeed = 90f,
                    burstCount = 3,
                    maxAmmo = 24,
                    reloadTime = 2.2f,
                    recoilKick = new Vector3(0f, 0.08f, 0.06f),
                    recoilReturnSpeed = 7f
                },
                
                new WeaponPreset("Pistol", "Low damage, high accuracy, fast reload")
                {
                    fireMode = FireMode.Single,
                    damage = 25f,
                    spread = 1.0f,
                    fireRate = 300f,
                    range = 75f,
                    bulletSpeed = 70f,
                    maxAmmo = 15,
                    reloadTime = 1.5f,
                    recoilKick = new Vector3(0f, 0.06f, 0.03f),
                    recoilReturnSpeed = 8f
                },
                
                new WeaponPreset("Machine Gun", "Extremely high fire rate, high damage, heavy recoil")
                {
                    fireMode = FireMode.Auto,
                    damage = 45f,
                    spread = 4f,
                    fireRate = 1200f,
                    range = 200f,
                    bulletSpeed = 100f,
                    maxAmmo = 100,
                    reloadTime = 5f,
                    recoilKick = new Vector3(0f, 0.15f, 0.12f),
                    recoilReturnSpeed = 4f
                }
            };
        }

        public static void ApplyPreset(Weapon weapon, WeaponPreset preset)
        {
            if (weapon == null || preset == null) return;

            // Use reflection to set private fields since we need to access serialized fields
            var weaponType = typeof(Weapon);
            
            // Ballistics
            SetField(weapon, weaponType, "damage", preset.damage);
            SetField(weapon, weaponType, "spread", preset.spread);
            SetField(weapon, weaponType, "fireRate", preset.fireRate);
            SetField(weapon, weaponType, "range", preset.range);
            SetField(weapon, weaponType, "bulletSpeed", preset.bulletSpeed);
            SetField(weapon, weaponType, "burstCount", preset.burstCount);
            SetField(weapon, weaponType, "fireMode", preset.fireMode);
            
            // Ammo
            SetField(weapon, weaponType, "maxAmmo", preset.maxAmmo);
            SetField(weapon, weaponType, "ammo", preset.maxAmmo); // Set current ammo to max
            SetField(weapon, weaponType, "reloadTime", preset.reloadTime);
            
            // Recoil
            SetField(weapon, weaponType, "recoilKick", preset.recoilKick);
            SetField(weapon, weaponType, "recoilReturnSpeed", preset.recoilReturnSpeed);

            UnityEditor.EditorUtility.SetDirty(weapon);
        }

        private static void SetField(object target, System.Type type, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}