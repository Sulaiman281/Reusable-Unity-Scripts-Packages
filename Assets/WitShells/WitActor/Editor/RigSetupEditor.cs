namespace WitShells.WitActor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Legacy rig setup - redirects to the new WitAnimationRig package.
    /// The Animation Rigging functionality has been moved to a separate package: WitAnimationRig
    /// </summary>
    public static class RigSetupEditorLegacy
    {
        [MenuItem("WitShells/Actor/RigSetup (Legacy)")]
        public static void OpenRigSetup()
        {
            EditorUtility.DisplayDialog("Moved to WitAnimationRig",
                "The Animation Rigging setup has been moved to a separate package.\n\n" +
                "Please use:\n" +
                "• WitShells > Animation Rig > Rig Setup Wizard\n" +
                "• WitShells > Animation Rig > Quick Rig Setup (Auto)\n\n" +
                "The WitAnimationRig package provides improved constraint controls.",
                "OK");
        }
    }
}
