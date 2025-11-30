namespace WitShells.ShootingSystem
{
    using System;
    using UnityEngine;

    [Serializable]
    public struct HitInfo
    {
        public Vector3 Point;
        public Vector3 Normal;
        public GameObject HitObject;
    }
}