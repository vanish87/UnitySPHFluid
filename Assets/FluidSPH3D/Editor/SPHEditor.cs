
using UnityEditor;
using UnityEngine;

namespace FluidSPH3D
{
    [CustomEditor(typeof(BoundaryConfigure))]
    public class BoundaryConfigureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var configure = target as BoundaryConfigure;
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
    [CustomEditor(typeof(EmitterConfigure))]
    public class EmitterConfigureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var configure = target as EmitterConfigure;
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
