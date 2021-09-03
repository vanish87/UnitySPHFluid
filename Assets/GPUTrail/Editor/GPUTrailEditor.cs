
using UnityEditor;
using UnityEngine;

namespace GPUTrail
{
    [CustomEditor(typeof(GPUTrailConfigure))]
    public class GPUTrailConfigureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var configure = target as GPUTrailConfigure;
            if (configure == null) return;

            if (GUILayout.Button("Save"))
            {
                configure.Save();
            }
            if (GUILayout.Button("Load"))
            {
                configure.Load();
                configure.NotifyChange();
            }
            base.OnInspectorGUI();
        }
    }
}