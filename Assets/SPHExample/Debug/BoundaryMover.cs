using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSPH
{
	public class BoundaryMover : MonoBehaviour
	{
        protected Vector3 startPos;
        protected bool moving = false;
        protected void OnEnable()
        {
            this.startPos = this.transform.localPosition;
        }

        protected void Update()
        {
            if(Input.GetKeyDown(KeyCode.M)) this.moving = !this.moving;
            if(this.moving) this.transform.localPosition = this.startPos + new Vector3(0,0,Mathf.Sin(Time.timeSinceLevelLoad * 0.5f)) * 10;
        }
	}
}