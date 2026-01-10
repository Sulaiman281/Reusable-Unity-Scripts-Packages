# WitPose Samples

This folder contains example scenes, poses, and workflows to help you get started with the WitPose bio-kinetic posing system.

## Sample Contents

### Basic Poses
- `BasicStanding.json` - Standard standing pose
- `SittingRelaxed.json` - Comfortable sitting position
- `ReachingForward.json` - Natural reaching motion
- `ListeningPose.json` - Attentive listening posture

### Example Scenes
- `WitPoseDemo.unity` - Complete setup with example character
- `PoseLibrary.unity` - Gallery of different poses

## Usage

1. Import the samples into your project
2. Open the demo scene
3. Select the character with WitPoseManager
4. Open `Window > WitPose > Pose Editor`
5. Load sample poses and experiment with modifications

## Creating Your Own Samples

To create custom pose samples:
1. Pose your character in the Scene View
2. Use "Export as JSON" in the WitPose Editor
3. Save the file to your samples directory
4. Import in other projects using "Import from File"

## Sample Pose JSON Format

```json
{
  "meta": {
    "name": "Example Pose",
    "category": "Action",
    "author": "YourName", 
    "description": "Description of the pose",
    "version": "1.0",
    "timestamp": 1704614400
  },
  "pose": {
    "bodyPosition": {"x": 0, "y": 0, "z": 0},
    "bodyRotation": {"x": 0, "y": 0, "z": 0, "w": 1},
    "muscles": [0.0, 0.1, -0.2, ...] // 95 float values
  }
}
```

Happy posing! 🦴✨