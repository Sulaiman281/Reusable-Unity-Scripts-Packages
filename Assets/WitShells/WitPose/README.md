# WitPose - Bio-Kinetic Posing Tool

A constraint-based forward kinematics posing tool for Unity Editor that enables direct joint manipulation with anatomical constraints and whole-body reactions for humanoid avatars.

## 🎯 Overview

WitPose is a **pure editor tool** (no runtime components needed) that transforms how you pose and animate humanoid characters in Unity. It treats the body as a connected biomechanical structure, where moving one joint causes natural reactions throughout the skeleton while respecting human anatomical limits.

## ✨ Key Features

- **🦴 Pure Editor Tool**: No runtime components needed - works entirely in the editor
- **🎯 Direct Bone Manipulation**: Click and drag any bone directly in Scene View with custom gizmos
- **🔒 Anatomical Constraints**: Every joint respects realistic human limits
- **🌊 Propagation System**: Rotations flow naturally through connected bones
- **💾 Pose Management**: Save, load, blend, and mirror poses
- **📋 JSON Export/Import**: Share poses across projects and teams
- **🎨 Visual Feedback**: Color-coded gizmos show constraints and selection
- **🔧 Seamless Integration**: Works directly with Unity's Animator system

## 🚀 Quick Start

### Installation

1. Copy the `WitPose` folder to your project's `Assets` directory
2. Open the WitPose Editor: `Window > WitPose > Pose Editor`

### Basic Usage

1. **Setup Character**
   - Select an Animator with a Humanoid avatar in your scene
   - Open the WitPose Editor window

2. **Enter Pose Mode**
   - Drag the Animator into the "Target Animator" field
   - Optional: Assign a Skeleton Profile for custom constraints
   - Click "Enter Pose Mode" - default Unity tools are disabled
   - Yellow sphere gizmos appear on all bones

3. **Pose Your Character**
   - Click any yellow bone sphere in Scene View to select it
   - Use the rotation handle to manipulate the bone
   - Green = selected bone, Red = constrained bone
   - Watch the body react naturally with propagation
   - Use "Save Current Pose" to store your work

## 🏗️ Core Components

### WitPoseEditor (Main Window)
- Pure editor interface - no runtime components
- Direct Animator bone manipulation
- Real-time constraint application
- Visual gizmo system for bone selection

### SkeletonProfile (ScriptableObject)
- Database of anatomical constraints
- Different profiles for age/body types  
- Per-bone rotation limits and stiffness
- Propagation factor settings

### PoseData (JSON Serialization)
- Rig-agnostic pose storage using HumanPose
- JSON export/import for sharing poses
- Pose blending and mirroring support

## 📊 Technical Architecture

### Pure Editor Approach
- **No MonoBehaviour components**: Tool works entirely in editor
- **Direct Transform manipulation**: Works with existing Animators
- **Scene View integration**: Custom gizmos and handles
- **Undo/Redo support**: Full Unity undo system integration

### Constraint System
- **Real-time clamping**: Rotations never exceed anatomical limits
- **Per-axis constraints**: Independent X, Y, Z rotation limits
- **Visual feedback**: Red gizmos when approaching limits

### Propagation Engine
- **Hierarchical influence**: Changes propagate to connected bones
- **Damping factors**: Prevents over-reaction in bone chains
- **Anatomical rules**: Based on real human biomechanics

## 🎨 Usage Examples

### Basic Bone Manipulation
1. Enter Pose Mode in the WitPose Editor
2. Click a bone sphere in Scene View (turns green when selected)
3. Drag the rotation handle to pose
4. Other bones react automatically based on constraints

### Custom Constraints
1. Select a bone in the editor
2. Expand "Constraint Settings"
3. Adjust min/max rotation limits
4. Modify stiffness and propagation factors

### Save and Share Poses
```csharp
// In the WitPose Editor:
// 1. Pose your character
// 2. Click "Save Current Pose"  
// 3. Click "Export JSON" to save to file
// 4. Use "Import JSON" to load in other projects
```

## 📁 Project Structure

```
WitPose/
├── Editor/
│   ├── WitPoseEditor.cs            # Main editor tool
│   ├── WitPoseEditorWindow.cs      # Legacy redirect
│   └── SkeletonProfileEditor.cs    # Profile inspector
├── Runtime/
│   ├── Scripts/
│   │   └── PoseData.cs             # JSON pose data
│   └── Data/
│       └── SkeletonProfile.cs      # Constraint database
├── Doc/                            # Technical documentation  
├── Samples~/                       # Example poses
└── README.md                       # This file
```

## 🔧 Workflow

### Creating Poses
1. Open WitPose Editor: `Window > WitPose > Pose Editor`
2. Drag your Humanoid Animator to "Target Animator"
3. Click "Enter Pose Mode"
4. Click bone spheres in Scene View to select
5. Drag rotation handles to pose
6. Save poses for reuse

### Using Skeleton Profiles
1. Create profile: `Assets > Create > WitPose > Skeleton Profile`
2. Assign to "Skeleton Profile" field in editor
3. Click "Apply Profile Constraints"
4. Customize individual bone constraints as needed

### Sharing Poses
- Export poses as JSON files
- Import JSON poses in other projects  
- Share pose libraries with team members
- Version control friendly format

## 🎯 Best Practices

### Performance
- Exit Pose Mode when not actively posing
- Use appropriate gizmo sizes for your scene scale
- Work with one character at a time

### Workflow
- Start with major joints (spine, shoulders, hips)
- Work from center outward to extremities
- Save incremental poses during complex work
- Use profile constraints for consistent results

### Constraints
- Test profiles on different character rigs
- Adjust propagation factors for desired reactions
- Use higher stiffness for structural bones
- Lower stiffness for secondary motion

## 🐛 Troubleshooting

### Common Issues

**"No bones visible in Scene View"**
- Ensure you're in Pose Mode
- Check that Target Animator has Humanoid avatar
- Try adjusting Gizmo Size slider

**"Bones won't rotate properly"**
- Check constraint limits aren't too restrictive
- Verify Skeleton Profile is applied correctly
- Try resetting the bone first

**"Propagation not working"**
- Check propagation factors aren't set to 0
- Ensure bones have proper parent-child relationships
- Some bones may need higher propagation values

## 🔮 Future Enhancements

- Advanced constraint visualization
- Animation Timeline integration
- Batch pose operations
- Custom bone shapes and colors
- Pose interpolation curves
- Cloud pose sharing service

## 📄 License

Copyright (c) 2026 WitShells. All rights reserved.

## 🤝 Support

For support, feature requests, or bug reports:
- Email: support@witshells.com
- Documentation: See `Doc/` folder for technical details

---

**Built for Unity 2022.3 LTS+ with ❤️ by WitShells**

## 🏗️ System Architecture

### Core Components

#### BioBone
Wraps Unity Transform with anatomical data:
- Rotation constraints (min/max angles)
- Stiffness and propagation factors
- Parent-child hierarchy relationships

#### BioSkeleton
Manages the complete skeleton system:
- Builds hierarchy from Humanoid Animator
- Handles constraint application
- Manages bone selection and updates

#### SkeletonProfile
ScriptableObject database for human limits:
- Standard anatomical constraints
- Different profiles for age/body types
- Editable constraint parameters

#### WitPoseManager
Main coordinator between systems:
- Pose state management
- Animation integration
- Event coordination

#### PoseData
Rig-agnostic pose storage:
- Uses Unity's HumanPose muscle space
- JSON serialization support
- Pose blending and mirroring

### Editor Tools

#### WitPoseEditorWindow
Main editor interface:
- Pose mode controls
- Bone selection UI
- Saved pose management
- Real-time parameter adjustment

#### Scene View Integration
Custom Scene View gizmos:
- Bone visualization
- Constraint indicators
- Direct manipulation handles
- Visual feedback system

## 📊 Technical Details

### Constraint System
- **Local-space rotations**: All constraints applied in bone local space
- **Euler angle limits**: Min/max values for each rotation axis
- **Real-time clamping**: Rotations never exceed anatomical limits

### Propagation Engine
- **Hierarchical influence**: Parent bones affect children
- **Spillover handling**: Excess rotation flows to related bones
- **Spine distribution**: Special logic for natural spine curves
- **Damping factors**: Prevents over-reaction in bone chains

### Data Storage
- **HumanPose integration**: Rig-agnostic muscle space storage
- **JSON format**: Human-readable pose sharing format
- **ScriptableObject profiles**: Reusable constraint databases

## 🎨 Usage Examples

### Creating a Sitting Pose
```csharp
// Get the pose manager
WitPoseManager poseManager = GetComponent<WitPoseManager>();

// Rotate hips down
poseManager.RotateBone(HumanBodyBones.Hips, Quaternion.Euler(-10, 0, 0));

// Bend knees (automatic constraint enforcement)
poseManager.RotateBone(HumanBodyBones.LeftUpperLeg, Quaternion.Euler(-90, 0, 0));
poseManager.RotateBone(HumanBodyBones.RightUpperLeg, Quaternion.Euler(-90, 0, 0));

// Save the pose
poseManager.SaveCurrentPose("Sitting Relaxed");
```

### Loading and Blending Poses
```csharp
// Load saved poses
PoseData standingPose = LoadPoseFromJSON("standing.json");
PoseData sittingPose = LoadPoseFromJSON("sitting.json");

// Blend between them (0.0 = standing, 1.0 = sitting)
poseManager.BlendPoses(standingPose, sittingPose, 0.5f);
```

### Creating Custom Constraints
```csharp
// Get a bone and modify its constraints
BioBone spine = poseManager.BioSkeleton.GetBone(HumanBodyBones.Spine);
spine.minRotation = new Vector3(-20, -15, -15);
spine.maxRotation = new Vector3(20, 15, 15);
spine.stiffness = 0.7f; // More rigid
spine.propagationFactor = 0.4f; // More influence on neighbors
```

## 📁 Project Structure

```
WitPose/
├── Runtime/
│   ├── Scripts/
│   │   ├── BioBone.cs              # Core bone wrapper
│   │   ├── BioSkeleton.cs          # Skeleton management
│   │   ├── PoseData.cs             # Pose storage & JSON
│   │   └── WitPoseManager.cs       # Main coordinator
│   └── Data/
│       └── SkeletonProfile.cs      # Constraint database
├── Editor/
│   ├── WitPoseEditorWindow.cs      # Main editor window
│   └── SkeletonProfileEditor.cs    # Profile inspector
├── Doc/                            # Technical documentation
├── package.json                    # Package manifest
└── README.md                       # This file
```

## 🔧 Configuration

### Skeleton Profiles
Create custom constraint profiles via:
`Assets > Create > WitPose > Skeleton Profile`

Standard profiles included:
- Standard Adult Human
- Elderly (reduced flexibility)
- Athletic (increased range)
- Child (higher flexibility)

### Constraint Parameters

#### Per-Bone Settings:
- **Min/Max Rotation**: Euler angle limits for each axis
- **Stiffness** (0-1): How rigid the bone is (0=jelly, 1=concrete)
- **Propagation Factor** (0-1): How much rotation influences neighbors

#### Global Settings:
- **Default Stiffness**: Applied to bones without custom values
- **Default Propagation**: Applied to bones without custom values

## 🎯 Best Practices

### Performance
- Use pose mode only when needed (disables it when done)
- Limit active bones during complex poses
- Cache frequently used poses as presets

### Workflow
- Start with major joints (hips, spine, shoulders)
- Work from center outward
- Save incremental poses during complex sequences
- Use mirroring for symmetric poses

### Constraints
- Test profiles on different character rigs
- Adjust stiffness for different animation styles
- Use postural bones for key structural elements

## 🐛 Troubleshooting

### Common Issues

**"System not initialized"**
- Ensure Animator has Humanoid avatar assigned
- Check that WitPoseManager has valid Skeleton Profile

**"Bone constraints not working"**
- Verify Skeleton Profile has constraints for the bone
- Check min/max rotation values are valid
- Ensure profile is applied to the skeleton

**"Poses look unnatural"**
- Adjust propagation factors for smoother reactions
- Reduce stiffness on secondary bones
- Check for conflicting constraints

**"Scene View gizmos not showing"**
- Enter Pose Mode in the WitPose Editor window
- Ensure Scene View is focused
- Check that target has valid BioSkeleton

## 🔮 Future Enhancements

- Runtime pose blending system
- Advanced constraint types (cone limits, twist limits)
- Pose interpolation curves
- Cloud pose sharing service
- Animation Timeline integration
- VR/AR pose capture support

## 📄 License

Copyright (c) 2026 WitShells. All rights reserved.

This is a proprietary Unity package. Redistribution and use in source and binary forms, with or without modification, are permitted provided that the copyright notice and this license appear in all copies.

## 🤝 Support

For support, feature requests, or bug reports:
- Email: support@witshells.com
- Documentation: See `Doc/` folder for technical details
- Unity Forum: WitShells WitPose thread

---

**Built for Unity 2022.3 LTS+ with ❤️ by WitShells**