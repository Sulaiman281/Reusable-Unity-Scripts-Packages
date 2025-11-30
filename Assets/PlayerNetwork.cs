using Unity.Netcode;
using WitShells.DesignPatterns;
using WitShells.ThirdPersonControl;

public class PlayerNetwork : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            var inputManager = FindFirstObjectByType<InputManage>();
            var camLookInput = FindFirstObjectByType<CinemachineCamLookInput>();

            var thirdPersonControl = GetComponent<ThirdPersonControl>();
            inputManager.RegisterPlayerController(thirdPersonControl, camLookInput);
            camLookInput.SetTarget(thirdPersonControl.CameraTarget);
            WitLogger.Log("Registered Player Controller for local player.");
        }
    }
}
