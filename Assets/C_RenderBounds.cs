using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class C_RenderBounds : MonoBehaviour {

    public GameObject boundsMax;
    public GameObject boundsMin;
	// Use this for initialization
	void Start () {
        Bounds bounds = getBounds(this.gameObject);
        boundsMax.transform.position = bounds.max;
        boundsMin.transform.position = bounds.min;
	}
	
	// Update is called once per frame
	void Update () {
  
	}

    Bounds getBounds(GameObject objeto){
        Bounds bounds;
        Renderer childRender;
        bounds = getRenderBounds(objeto);
        if(bounds.extents.x == 0){
            bounds = new Bounds(objeto.transform.position,Vector3.zero);
            foreach (Transform child in objeto.transform) {
                childRender = child.GetComponent<Renderer>();
                if (childRender) {
                    bounds.Encapsulate(childRender.bounds);
                }else{
                    bounds.Encapsulate(getBounds(child.gameObject));
                }
            }
        }
        return bounds;
    }


    Bounds getRenderBounds(GameObject objeto)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        Renderer render = objeto.GetComponent<Renderer>();
        if (render != null)
        {
            return render.bounds;
        }
        return bounds;
    }

}
