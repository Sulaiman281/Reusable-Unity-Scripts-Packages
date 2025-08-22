namespace WitShells.ThreadingJob
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(ThreadManager))]
    public class ThreadManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Test Example Job"))
            {
                ((ThreadManager)target).EnqueueJob(new ExampleJob(5), result =>
                {
                    Debug.Log($"Job completed with result: {result}");
                }, ex =>
                {
                    Debug.LogError($"Job failed with exception: {ex}");
                });
            }
        }
    }
}