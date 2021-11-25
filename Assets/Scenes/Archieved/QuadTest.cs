using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Rendering;

public class QuadTest : MonoBehaviour
{
    public Shader quad;
    public DisposableMaterial material;
    // Start is called before the first frame update
    void Start()
    {
        this.material = new DisposableMaterial(this.quad);
        
    }

    // Update is called once per frame
    void Update()
    {
		Material mat = this.material;
		var b = new Bounds(Vector3.zero, Vector3.one * 10000);
		Graphics.DrawProcedural(mat, b, MeshTopology.Points, 1024);
    }

    void OnDestory()
    {
        this.material?.Dispose();
    }
}
