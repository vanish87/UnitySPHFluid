using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Rendering;

public class KWTest : MonoBehaviour
{
    [System.Flags]
    public enum Keyword
    {
		None = 0,
		Red = 1 << 1,
		Green = 1 << 2,
		Blue = 1 << 3,
	}
	public DisposableMaterial<Keyword> material;
	public Shader shader;

    enum Base
    {
        A, B ,C
    }
	void Start()
	{
		this.material = new DisposableMaterial<Keyword>(this.shader);
        var mrend = this.GetComponent<MeshRenderer>();
        mrend.sharedMaterial = this.material;

	}

	// Update is called once per frame
	void Update()
	{
        this.material.UpdateKeyword();
        foreach (var kws in this.material.Data.enabledKeywords)
        {
            Debug.Log(kws);
        }
	}

	void OnDestory()
	{
		this.material?.Dispose();
	}
}
