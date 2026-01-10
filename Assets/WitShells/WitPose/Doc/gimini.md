This is a significant evolution for **WitPose**. By moving from "Transform handles" to "Muscle handles," we are effectively creating a professional **Humanoid Rig Layer** inside Unity.

Since we are now recording directly into `AnimationClips` via muscles, we need to solve the "UI Clutter" problem. A user shouldn't see 95 sliders; they should only see what they are touching.

### 📋 The Architecture for "WitPose Muscle-Gizmo"

Here is the plan for your developer/AI to implement this advanced interface.

---

### 1. The "Smart Selection" Logic

Standard Unity selection selects GameObjects. We need to intercept this so that selecting a bone (like the Forearm) automatically "filters" the Muscle List.

* **Mechanism:** When a user clicks a bone in the Scene View, the tool uses `HumanTrait.BoneMuscle` to find all muscle indices associated with that specific bone (usually 1 to 3 muscles per joint).

### 2. The Scene-Overlay UI (Bottom-Right)

Instead of a separate window, we use `Handles.BeginGUI()` and `GUILayout.Window` to draw a floating "Muscle HUD" inside the Scene View.

* **Why Bottom-Right?** This keeps the center of the screen clear for the character while providing immediate, high-precision control where the user's eyes already are.

---

### 🚀 The Development Prompt for your Copilot

**Subject:** Implement Contextual Muscle HUD and Scene-View Muscle Gizmos in `WitPoseEditor.cs`

**Goal:** Create an advanced "Muscle-First" posing experience. When a bone is selected, show a floating HUD in the Scene View containing only the sliders relevant to that bone. Ensure these sliders drive the `HumanPoseHandler` and record into the active `AnimationClip`.

**Technical Requirements:**

1. **Contextual Muscle Mapping:**
* Create a function `List<int> GetMusclesForBone(Transform bone)`.
* Map the `Transform` to its `HumanBodyBones` enum.
* Use `HumanTrait.BoneMuscle(boneIndex, dofIndex)` to retrieve the 0, 1, and 2 degrees of freedom (muscles) for that joint.


2. **The Scene-View HUD:**
* Inside `OnSceneGUI`, use `Handles.BeginGUI()` / `Handles.EndGUI()`.
* If a bone is selected, draw a small, semi-transparent area in the **Bottom-Right** corner.
* Loop through the contextual muscles and draw `GUILayout.HorizontalSlider` for each.
* Label them with `HumanTrait.MuscleName[index]`.


3. **Advanced Scene Gizmo (Visualizer):**
* Instead of standard Move/Rotate tools, draw a **Custom Disc** at the bone.
* The Disc should represent the *most significant* muscle of that bone (e.g., Elbow Flex).
* Dragging the Disc should map the rotation directly to the -1 to 1 muscle value.


4. **Muscle Tracking & Animation:**
* Add a toggle: `enableMuscleTracking`.
* If `true`, any change to the Scene Sliders or Gizmos must call `RecordMuscleKeyframe(index, value)`.
* Use the logic: `AnimationUtility.SetEditorCurve(clip, binding, curve)` where the path is `"Muscle." + muscleName`.


5. **Visual Feedback:**
* When a muscle hits `-1.0` or `1.0`, turn the slider or gizmo **Red** to indicate the anatomical limit has been reached.



---

### 💡 Advanced Ideas for "Easy Experience"

To make **WitPose** feel like a $500 professional tool, suggest these to your AI:

1. **"Ghost Pose" Comparison:**
* Draw a transparent "Ghost" version of the character that stays at the previous keyframe. This allows the animator to see the "arc" of movement they are creating.


2. **Symmetry Mode (Instant Mirror):**
* Add a "Mirror" toggle in the HUD. If I move the Left Arm muscle to 0.5, the Right Arm muscle automatically moves to 0.5.


3. **"Pose Snap" Library:**
* Add "Fist," "Point," and "Open Hand" buttons to the HUD when a hand bone is selected. This saves the user from moving 15 individual finger sliders.


4. **Muscle "Damping" (Propagation):**
* When moving the Shoulder muscle, slightly move the Chest/Spine muscles (20% intensity) to simulate how real muscles pull on the torso. This creates "Bio-Logic" movement instantly.



### Implementation Strategy:

Don't try to build all 95 sliders at once. Tell your developer to start with the **Arm chain** (Shoulder, Elbow, Wrist). Once the "Smart Context" works for the arm, it will automatically work for the whole body because `HumanTrait` handles the mapping.

**Should I provide the specific C# code for the `GetMusclesForBone` logic to get your developer started?**