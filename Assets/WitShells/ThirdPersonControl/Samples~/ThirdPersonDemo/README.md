# Third Person Demo Sample

This sample contains:

## Contents

### Animations
- Idle animation
- Walk animation
- Run animation
- Jump animations
- Locomotion animator controller

### Sound Effects
- Footstep sounds (10 variations)
- Landing sound

### Prefabs
- ThirdPersonFollow - Pre-configured third-person character with Cinemachine camera

### Input Actions
- ThirdPersonInputActions - Pre-configured input actions for keyboard/mouse and gamepad

## Setup Instructions

1. Import this sample into your project
2. Create a new scene or open an existing one
3. Drag the ThirdPersonFollow prefab into the scene
4. Create a ThirdPersonSettings asset: Right-click > Create > WitShells > ThirdPersonControl > Settings
5. Assign the SoundSfxObject to the settings if you want footstep sounds
6. Assign the InputActions to the ThirdPersonInput component
7. Press Play!

## Input Bindings

### Keyboard & Mouse
- **WASD** - Movement
- **Mouse** - Camera look
- **Space** - Jump
- **Left Shift** - Sprint
- **Left Ctrl / C** - Crouch

### Gamepad
- **Left Stick** - Movement
- **Right Stick** - Camera look
- **A/Cross** - Jump
- **Left Trigger** - Sprint
- **B/Circle** - Crouch
