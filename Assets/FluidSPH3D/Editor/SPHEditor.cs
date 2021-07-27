
using UnityEditor;
using UnityEngine;

namespace FluidSPH3D
{
    [CustomEditor(typeof(FluidSPH3DConfigure))]
    public class FluidSPH3DConfigureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var configure = target as FluidSPH3DConfigure;
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
