/*
 * FreeMoCapReceiver.cs  —  Kinect Live Bridge receiver for Unity
 * ─────────────────────────────────────────────────────────────────────────────
 * Connects to the Python kinect_live_bridge.py WebSocket server and drives
 * a Humanoid avatar in real time using Kinect v2 joint data.
 *
 * ── SETUP GUIDE ──────────────────────────────────────────────────────────────
 *
 * 1. Install NativeWebSocket in Unity:
 *    Window → Package Manager → [+] → "Add package from git URL..."
 *    Paste:  https://github.com/endel/NativeWebSocket.git#upm
 *
 * 2. Attach this script to a GameObject that has:
 *    - Animator component with a Humanoid Avatar configured
 *    - (e.g. any Mixamo character, RPM avatar, or VRoid model exported to FBX)
 *
 * 3. In the Inspector set:
 *    - Host: localhost  (or LAN IP if bridge runs on another PC)
 *    - Port: 8765
 *
 * 4. Start kinect_live_bridge.py, then press Play in Unity.
 *
 * ── COORDINATE SYSTEM NOTE ───────────────────────────────────────────────────
 * The Python bridge already converts Kinect (right-handed) → Unity (left-handed)
 * by flipping Z position and negating X/Y of quaternions.
 * Positions arrive in metres, centred on the Kinect sensor.
 *
 * ── IK STRATEGY ──────────────────────────────────────────────────────────────
 * • Hips      — positioned + rotated using SpineBase / SpineMid
 * • Spine/Neck/Head — FK rotation from Kinect orientations
 * • Hands & Feet   — Unity Animator IK (most stable for extremities)
 * • Shoulders/Upper arms/Legs — FK rotation from Kinect orientations
 * ─────────────────────────────────────────────────────────────────────────────
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

[RequireComponent(typeof(Animator))]
public class FreeMoCapReceiver : MonoBehaviour
{
    // ── Inspector fields ──────────────────────────────────────────────────────
    [Header("Connection")]
    public string Host = "localhost";
    public int    Port = 8765;

    [Header("Avatar")]
    [Tooltip("Position scale: Kinect metres → Unity units (1 = 1:1)")]
    public float PositionScale = 1.0f;

    [Tooltip("Smooth factor for rotations (higher = snappier, lower = smoother)")]
    [Range(1f, 30f)]
    public float RotationSmoothSpeed = 12f;

    [Tooltip("Smooth factor for hip position")]
    [Range(1f, 30f)]
    public float PositionSmoothSpeed = 15f;

    [Header("IK Weights")]
    [Range(0f, 1f)] public float IKWeightHands = 1.0f;
    [Range(0f, 1f)] public float IKWeightFeet  = 0.9f;

    [Header("Debug")]
    public bool ShowDebugGizmos = true;

    // ── Private ───────────────────────────────────────────────────────────────
    private WebSocket   _ws;
    private Animator    _animator;

    // Thread-safe message queue (WebSocket callbacks fire off the main thread)
    private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

    // Latest parsed pose (updated each frame from queue)
    private Dictionary<string, JointData> _pose = new Dictionary<string, JointData>();

    // Avatar T-pose references (captured at Start)
    private Dictionary<HumanBodyBones, Quaternion> _tPoseLocal  = new Dictionary<HumanBodyBones, Quaternion>();
    private Dictionary<HumanBodyBones, Quaternion> _tPoseWorld  = new Dictionary<HumanBodyBones, Quaternion>();

    // IK target transforms (invisible GameObjects)
    private Transform _ikLeftHand, _ikRightHand, _ikLeftFoot, _ikRightFoot;

    // ── Joint data struct ─────────────────────────────────────────────────────
    private struct JointData
    {
        public Vector3    position;
        public Quaternion rotation;
    }

    // ── Kinect joint name → Unity HumanBodyBones ──────────────────────────────
    private static readonly Dictionary<string, HumanBodyBones> BoneMap =
        new Dictionary<string, HumanBodyBones>
    {
        { "Head",          HumanBodyBones.Head        },
        { "Neck",          HumanBodyBones.Neck        },
        { "SpineShoulder", HumanBodyBones.UpperChest  },
        { "SpineMid",      HumanBodyBones.Chest       },
        { "SpineBase",     HumanBodyBones.Hips        },
        { "ShoulderLeft",  HumanBodyBones.LeftShoulder    },
        { "ElbowLeft",     HumanBodyBones.LeftUpperArm    },
        { "WristLeft",     HumanBodyBones.LeftLowerArm    },
        { "HandLeft",      HumanBodyBones.LeftHand         },
        { "ShoulderRight", HumanBodyBones.RightShoulder   },
        { "ElbowRight",    HumanBodyBones.RightUpperArm   },
        { "WristRight",    HumanBodyBones.RightLowerArm   },
        { "HandRight",     HumanBodyBones.RightHand        },
        { "HipLeft",       HumanBodyBones.LeftUpperLeg    },
        { "KneeLeft",      HumanBodyBones.LeftLowerLeg    },
        { "AnkleLeft",     HumanBodyBones.LeftFoot         },
        { "HipRight",      HumanBodyBones.RightUpperLeg   },
        { "KneeRight",     HumanBodyBones.RightLowerLeg   },
        { "AnkleRight",    HumanBodyBones.RightFoot        },
    };

    // FK-only bones (not driven by IK goals)
    private static readonly HashSet<HumanBodyBones> FKBones = new HashSet<HumanBodyBones>
    {
        HumanBodyBones.Hips,
        HumanBodyBones.Chest, HumanBodyBones.UpperChest,
        HumanBodyBones.Neck,  HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,  HumanBodyBones.LeftUpperArm,  HumanBodyBones.LeftLowerArm,
        HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
        HumanBodyBones.LeftUpperLeg,  HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
    };

    // ──────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null || _animator.avatar == null || !_animator.avatar.isHuman)
        {
            Debug.LogError("[FreeMoCapReceiver] No Humanoid Animator found!");
            enabled = false;
            return;
        }

        _CaptureTPosets();
        _CreateIKTargets();
        _Connect();
    }

    private void Update()
    {
        // Drain message queue (main thread only)
        while (_messageQueue.TryDequeue(out string msg))
            _ParseMessage(msg);

        // Apply FK pose
        _ApplyFK();

        // Dispatch WebSocket (required by NativeWebSocket on WebGL/non-threaded)
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
#endif
    }

    // Animator IK callback — called after Animator.Update()
    private void OnAnimatorIK(int layerIndex)
    {
        if (_pose.Count == 0) return;

        _SetIKGoal(AvatarIKGoal.LeftHand,  _ikLeftHand,  IKWeightHands);
        _SetIKGoal(AvatarIKGoal.RightHand, _ikRightHand, IKWeightHands);
        _SetIKGoal(AvatarIKGoal.LeftFoot,  _ikLeftFoot,  IKWeightFeet);
        _SetIKGoal(AvatarIKGoal.RightFoot, _ikRightFoot, IKWeightFeet);
    }

    private void OnDestroy() => _ws?.Close();

    // ──────────────────────────────────────────────────────────────────────────
    // Connection
    // ──────────────────────────────────────────────────────────────────────────
    private async void _Connect()
    {
        string url = $"ws://{Host}:{Port}";
        Debug.Log($"[FreeMoCapReceiver] Connecting to {url}");

        _ws = new WebSocket(url);

        _ws.OnOpen    += () => Debug.Log("[FreeMoCapReceiver] Connected!");
        _ws.OnClose   += (code) =>
        {
            Debug.Log($"[FreeMoCapReceiver] Disconnected ({code}). Reconnecting in 2s…");
            Invoke(nameof(_Reconnect), 2f);
        };
        _ws.OnError   += (err)  => Debug.LogWarning($"[FreeMoCapReceiver] WS error: {err}");
        _ws.OnMessage += (data) => _messageQueue.Enqueue(System.Text.Encoding.UTF8.GetString(data));

        await _ws.Connect();
    }

    private void _Reconnect()
    {
        _ws?.Close();
        _Connect();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // JSON parsing
    // ──────────────────────────────────────────────────────────────────────────
    [Serializable] private class JointJson
    {
        public float px, py, pz;   // position (metres, Unity-space)
        public float qx, qy, qz, qw; // rotation quaternion (Unity-space)
        public int   s = 2;        // tracking state (2=tracked)
    }
    [Serializable] private class PoseJson
    {
        public Dictionary<string, JointJson> joints;
    }

    private void _ParseMessage(string json)
    {
        try
        {
            // Unity's JsonUtility can't deserialize arbitrary dictionaries directly.
            // We use a minimal manual approach: rely on Newtonsoft.Json if available,
            // otherwise use a simple substring parser.
            var root = Newtonsoft.Json.Linq.JObject.Parse(json);
            var jointsToken = root["joints"];
            if (jointsToken == null) return;

            foreach (var kv in jointsToken.ToObject<Dictionary<string, JointJson>>())
            {
                var j = kv.Value;
                _pose[kv.Key] = new JointData
                {
                    position = new Vector3(j.px, j.py, j.pz) * PositionScale,
                    rotation = new Quaternion(j.qx, j.qy, j.qz, j.qw),
                };
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FreeMoCapReceiver] Parse error: {e.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FK — apply Kinect rotations to spine / limb bones
    // ──────────────────────────────────────────────────────────────────────────
    private void _ApplyFK()
    {
        if (_pose.Count == 0) return;

        foreach (var (kinectName, bone) in BoneMap)
        {
            if (!FKBones.Contains(bone))              continue;
            if (!_pose.TryGetValue(kinectName, out var jd)) continue;

            Transform t = _animator.GetBoneTransform(bone);
            if (t == null)                            continue;

            if (bone == HumanBodyBones.Hips)
            {
                // Root position from SpineBase
                Vector3 targetPos = jd.position;
                t.position = Vector3.Lerp(t.position, targetPos, Time.deltaTime * PositionSmoothSpeed);
            }

            // Compute local rotation from Kinect world quaternion
            Quaternion parentRot = t.parent != null ? t.parent.rotation : Quaternion.identity;
            Quaternion tPoseCorr = _tPoseLocal.TryGetValue(bone, out var tp) ? tp : Quaternion.identity;

            // The Kinect gives us absolute (world-space) orientation.
            // Convert to local: local = Inv(parent_world) * kinect_world * tpose_correction_factor
            // tpose_correction_factor accounts for difference between Kinect neutral and avatar T-pose.
            // For first-order approximation we use the raw kinect orientation converted to local space.
            Quaternion targetLocal = Quaternion.Inverse(parentRot) * jd.rotation;
            t.localRotation = Quaternion.Slerp(
                t.localRotation,
                targetLocal * tPoseCorr,
                Time.deltaTime * RotationSmoothSpeed
            );
        }

        // Update IK target positions each frame
        _UpdateIKTarget(_ikLeftHand,  "HandLeft");
        _UpdateIKTarget(_ikRightHand, "HandRight");
        _UpdateIKTarget(_ikLeftFoot,  "FootLeft");
        _UpdateIKTarget(_ikRightFoot, "FootRight");
    }

    private void _UpdateIKTarget(Transform target, string jointName)
    {
        if (target == null) return;
        if (!_pose.TryGetValue(jointName, out var jd)) return;
        target.position = Vector3.Lerp(target.position, jd.position, Time.deltaTime * PositionSmoothSpeed);
        target.rotation = Quaternion.Slerp(target.rotation, jd.rotation, Time.deltaTime * RotationSmoothSpeed);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IK goal helper
    // ──────────────────────────────────────────────────────────────────────────
    private void _SetIKGoal(AvatarIKGoal goal, Transform target, float weight)
    {
        if (target == null) return;
        _animator.SetIKPositionWeight(goal, weight);
        _animator.SetIKRotationWeight(goal, weight * 0.5f);
        _animator.SetIKPosition(goal, target.position);
        _animator.SetIKRotation(goal, target.rotation);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Initialisation helpers
    // ──────────────────────────────────────────────────────────────────────────
    private void _CaptureTPosets()
    {
        // Capture world + local rotations of every mapped bone in the T-pose
        foreach (var bone in BoneMap.Values)
        {
            Transform t = _animator.GetBoneTransform(bone);
            if (t == null) continue;
            _tPoseWorld[bone] = t.rotation;
            _tPoseLocal[bone] = t.localRotation;
        }
    }

    private void _CreateIKTargets()
    {
        _ikLeftHand  = _MakeTarget("IK_LeftHand");
        _ikRightHand = _MakeTarget("IK_RightHand");
        _ikLeftFoot  = _MakeTarget("IK_LeftFoot");
        _ikRightFoot = _MakeTarget("IK_RightFoot");
    }

    private Transform _MakeTarget(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform.parent);
        go.hideFlags = HideFlags.HideInHierarchy;
        return go.transform;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Debug gizmos
    // ──────────────────────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (!ShowDebugGizmos || _pose == null || _pose.Count == 0) return;

        Gizmos.color = Color.cyan;
        foreach (var jd in _pose.Values)
            Gizmos.DrawSphere(jd.position, 0.03f);

        // Draw skeleton lines
        _DrawLine("SpineBase",  "SpineMid");
        _DrawLine("SpineMid",   "SpineShoulder");
        _DrawLine("SpineShoulder", "Neck");
        _DrawLine("Neck",       "Head");
        _DrawLine("SpineShoulder", "ShoulderLeft");
        _DrawLine("ShoulderLeft",  "ElbowLeft");
        _DrawLine("ElbowLeft",     "WristLeft");
        _DrawLine("WristLeft",     "HandLeft");
        _DrawLine("SpineShoulder", "ShoulderRight");
        _DrawLine("ShoulderRight", "ElbowRight");
        _DrawLine("ElbowRight",    "WristRight");
        _DrawLine("WristRight",    "HandRight");
        _DrawLine("SpineBase",  "HipLeft");
        _DrawLine("HipLeft",    "KneeLeft");
        _DrawLine("KneeLeft",   "AnkleLeft");
        _DrawLine("SpineBase",  "HipRight");
        _DrawLine("HipRight",   "KneeRight");
        _DrawLine("KneeRight",  "AnkleRight");
    }

    private void _DrawLine(string a, string b)
    {
        if (_pose.TryGetValue(a, out var ja) && _pose.TryGetValue(b, out var jb))
            Gizmos.DrawLine(ja.position, jb.position);
    }
}
