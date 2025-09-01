using UnityEditor;
using UnityEngine;

namespace WitShells.SimpleCarControls
{
    public class SetupCarDriver : MonoBehaviour
    {
        [MenuItem("WitShells/SimpleCarController/Setup AI Car Driver")]
        public static void SetupAICar()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject in the hierarchy.", "OK");
                return;
            }

            GameObject selected = Selection.activeGameObject;

            if (!selected.TryGetComponent(out SimpleCarDriver carDriver))
            {
                carDriver = Undo.AddComponent<SimpleCarDriver>(selected);
            }

            if (!selected.TryGetComponent(out SimpleCarDriverAI carDriverAI))
            {
                carDriverAI = Undo.AddComponent<SimpleCarDriverAI>(selected);
            }
        }
    }
}