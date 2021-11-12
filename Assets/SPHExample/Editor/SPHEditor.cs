
using UnityEditor;
using UnityEngine;

namespace FluidSPH
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
            if(GUILayout.Button("Open File"))
            {
                configure.OpenFile();
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
            if(GUILayout.Button("Open File"))
            {
                configure.OpenFile();
            }
            base.OnInspectorGUI();
        }
    }
    [CustomEditor(typeof(SPHConfigure))]
    public class FluidSPH3DConfigureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var configure = target as SPHConfigure;
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
            if(GUILayout.Button("Open File"))
            {
                configure.OpenFile();
            }
            base.OnInspectorGUI();
        }
    }
}
