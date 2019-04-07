using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Script1 : MonoBehaviour {

    private Texture2D renderTexture;
    private Camera cam;
    private int rayDistance;
    bool RealTime = false;
    private Light[] lights;
    private LayerMask collisionMask = 1 << 31;
    private List<Vector3> FirstHit;
    private Vector3 CameraPos;
    Material matt;
    /*
    void Awake() {
        rayDistance = 1000;
        Debug.Log("awake");
    }

    void RayTrace()
    {
        for(int x = 0; x < renderTexture.width; x++)
        {
            for(int y = 0; y < renderTexture.height; y++)
            {
                Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y, 0.0f));
                renderTexture.SetPixel(x, y, TraceRay(ray));
                //if (x % 100 == 0 && y % 100 == 0)
                //{
                //    Debug.Log(x);
                //    Debug.Log(y);
                //}
            }
        }
        renderTexture.Apply();
    }

    Color TraceRay(Ray ray)
    {
        if (Physics.Raycast(ray, rayDistance))
        {
            return Color.white;
        }
        else
        {
            return Color.black;
        }
    }
    // Use this for initialization
    void Start () {
        if (renderTexture) Destroy(renderTexture);
        renderTexture = new Texture2D(Screen.height, Screen.width);
        RayTrace();
    }
	
	// Update is called once per frame
	void Update () {
		
	}
    void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
    }
    */


    float resolution; // the size of the result will be (1/resolution) * Screen dimensions
    Texture2D outTex ; // the result image
    private int width ; // we don't need to set width
    private int height; // and height in the inspector

    void Start()
    {
        FirstHit = new List<Vector3>();
        // we want to start over, so if there already is an outTex, destroy it
        if (outTex) Destroy(outTex);
        // so the dimensions are smaller than the actual screen size
        resolution = 1;
        width = System.Convert.ToInt32(Screen.width * resolution);
        height = System.Convert.ToInt32(Screen.height * resolution);
        cam = FindObjectOfType<Camera>();
        CameraPos = cam.transform.position;
        Debug.Log(CameraPos);
        rayDistance = 1000;
        //Debug.Log(Camera.main.)
        // create a blank outTex
        outTex = new Texture2D(width, height);
        //GenerateColliders();
        // start raytracing
        if (!RealTime)
            Raytrace();
    }

    void OnGUI()
    {
        // draw outTex to the screen
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), outTex);
    }

    void Raytrace()
    {
        lights = FindSceneObjectsOfType(typeof(Light)) as Light[];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                outTex.SetPixel(x, y, TracePixel(x, y));
            }
        }
        outTex.Apply();

    }

    Color TracePixel(int x ,int  y )
    {
        // get the ray from the data we have
        // we multiply x and y by resolution so what we render covers the whole screen;
        // otherwise, if resolution was 2, we would only render half the screen!
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(x / resolution, y / resolution, 0));
        RaycastHit hit;
        Color returnColor = Color.black;
        if (Physics.Raycast(ray, out hit, rayDistance))
        //if (Physics.Raycast(ray, out hit,rayDistance , collisionMask))
        {
            //把hit的交點加入list
            FirstHit.Add(hit.point);
            Material mat;
            mat = hit.collider.GetComponent<Renderer>().material;
            if (mat.mainTexture)
            {
                returnColor += (mat.mainTexture as Texture2D).GetPixelBilinear(hit.textureCoord.x, hit.textureCoord.y);
                //return the color of the pixel at the pixel coordinate of the hit
            }
            else
            {
                returnColor += mat.color;
            }
            returnColor *= TraceLight(hit.point + hit.normal * 0.0001f , hit.normal);
        }
        return returnColor;
    }
    void Update()
    {
        if (RealTime)
        {
            Raytrace();
        }

        DrawRay();
    }

    Color TraceLight(Vector3 pos , Vector3 normal)
    {
        Color returnColor = RenderSettings.ambientLight;
        foreach(Light light in lights)
        {
            if (light.enabled)
                returnColor += LightTrace(light, pos , normal);
        }
        return returnColor;
    }

    Color LightTrace(Light light , Vector3 pos , Vector3 normal)
    {
        float dot;
        if (light.type == LightType.Directional)
        {
            dot = Vector3.Dot(-light.transform.forward, normal);
            if (dot > 0)
            {
                //if (Physics.Raycast(pos, -light.transform.forward , collisionMask))
                if (Physics.Raycast(pos, -light.transform.forward))
                    return Color.black;
                return light.color * light.intensity * dot;
            }
            return Color.black;
        }
        else
        {
            Vector3 direction = (light.transform.position - pos).normalized;
            dot = Vector3.Dot(normal, direction);
            float distance = Vector3.Distance(pos, light.transform.position);
            if (distance < light.range && dot > 0)
            {
                if (light.type == LightType.Point)
                {
                    //Raycast as we described
                    if (Physics.Raycast(pos, direction, distance, collisionMask))
                    {
                        return Color.black;
                    }
                    return light.color * light.intensity * dot * (1 - distance / light.range);
                }
                //Lets check weather we are in the spot or not
                else if (light.type == LightType.Spot)
                {
                    float dot2 = Vector3.Dot(-light.transform.forward, direction);
                    if (dot2 > (1 - light.spotAngle / 180))
                    {
                        if (Physics.Raycast(pos, direction, distance, collisionMask))
                        {
                            return Color.black;
                        }

                        //We multiply by the multiplier we defined above
                        return light.color * light.intensity * dot * (1 - distance / light.range) * ((dot2 / (1 - light.spotAngle / 180)));
                    }
                }
            }
        }
        return Color.black;
    }
    //未完成
    void GenerateColliders()
    {
        MeshFilter[] meshfilter = FindSceneObjectsOfType(typeof(MeshFilter)) as MeshFilter[];
        foreach(MeshFilter mf in meshfilter)
        {
            if (mf.GetComponent<MeshRenderer>() && !mf.GetComponent<Collider>())
            {
                GameObject tmpGO = new GameObject("RTRMeshRenderer");
                tmpGO.AddComponent<MeshCollider>().sharedMesh = mf.mesh;
                tmpGO.transform.parent = mf.transform;
                tmpGO.transform.localPosition = Vector3.zero;
                tmpGO.transform.localScale = Vector3.one;
                tmpGO.transform.localRotation = Quaternion.identity;
                tmpGO.GetComponent<Collider>().isTrigger = true;
                tmpGO.layer = 31;
            }
        }
    }
    void DrawRay()
    {
        //matt.SetPass(0);
        //GL.Begin(GL.LINES);
        //GL.Color(Color.green);
        //foreach (Vector3 hitpoint in FirstHit)
        //{
        //    GL.Vertex(CameraPos);
        //    GL.Vertex(hitpoint);
        //}
        //GL.End();
        foreach (Vector3 hitpoint in FirstHit)
        {
            Debug.DrawLine(CameraPos, hitpoint, Color.green);
        }
    }
}
