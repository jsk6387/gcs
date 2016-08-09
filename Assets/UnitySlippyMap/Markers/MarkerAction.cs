using UnityEngine;
using UnitySlippyMap.Map;
using UnityEngine.UI;
using UnitySlippyMap.Helpers;
using UnitySlippyMap.UserGUI;
using System;
namespace UnitySlippyMap.Markers
{
    public class MarkerAction : MonoBehaviour
    {
        
        
        /// <summary>
        /// The last raycast hit position.
        /// </summary>
        private static Vector3 lastHitPosition = Vector3.zero;
        private GameObject[] gos;
        private bool isShow = false;
        private Rect windowDelete;
        public Rect doWindowDelete;
        private double[] pos = new double[2];
        private double lastCameraScale;
        private float zoomScale = 1.05f;
        // Use this for initialization
        void Start()
        {
            lastCameraScale = Camera.main.transform.position.y;   
        }
        public void OnMouseUp()
        {
            windowDelete = new Rect(Input.mousePosition.x,
              Screen.height - Input.mousePosition.y - 80,
               220, 80);
            isShow = true;
            print(Input.mousePosition.y);
            print(windowDelete.min.x);
        }
        void OnGUI()
        {
            if (isShow)
            {
                doWindowDelete = GUI.Window(1, windowDelete, DoWindowDelete, "Delete Mark");
            }
            
        }
        /// <summary>
        /// marker delete windows
        /// </summary>
        /// <param name="WindowID"></param>
        void DoWindowDelete(int WindowID)
        {
            MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
            SaveLoadBehavior SLB = GameObject.Find("GameObject").GetComponent<SaveLoadBehavior>();
            ButtonBehavior btn = GameObject.Find("GameObject").GetComponent<ButtonBehavior>();
            SLB.usingUI = true;

            GUI.Label(new Rect(10, 20, 200, 20), "Do you want to delete the marker?");
            
            if (GUI.Button(new Rect(30,50,40,20),"Yes"))
            {
                
                SLB.usingUI = false;
                pos = GeoHelpers.ScreenpointToWGS84(map,gameObject.transform.position );
                pos[0] = Math.Round(pos[0], 6, MidpointRounding.AwayFromZero);
                pos[1] = Math.Round(pos[1], 6, MidpointRounding.AwayFromZero);
                map.getMarkerLong().Remove(pos[0]);
                map.getMarkerLat().Remove(pos[1]);
                Destroy(gameObject);
                print("pos : " +pos[0]+" / "+pos[1]);
               doReorder(map.getMarkerLong().Count);
                
            }
            else if(GUI.Button(new Rect(80,50,40,20),"No"))
            {
                isShow = false;
                SLB.usingUI = false;
            }
            if (!(Input.mousePosition.x > windowDelete.min.x && Input.mousePosition.x < windowDelete.max.x
                && Screen.height - Input.mousePosition.y > windowDelete.min.y && Screen.height - Input.mousePosition.y < windowDelete.max.y
                ) && Input.GetMouseButtonDown(0))
            {
                isShow = false;
                SLB.usingUI = false;
            }
        }
        /// <summary>
        /// draw GPSfield by order 
        /// </summary>
        /// <param name="index"></param>
        public void doReorder(int index)
        {
            gos = GameObject.FindGameObjectsWithTag("GPSfield");
            foreach(GameObject go in gos)
            {
                Destroy(go);
            }
            gos = GameObject.FindGameObjectsWithTag("Order");
            foreach (GameObject go in gos)
            {
                Destroy(go);
            }
            MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
            for (int i=0;i<index;i++)
            {
                print("i : " + i);
                pos[0] = map.getMarkerLong(i);
                pos[1] = map.getMarkerLat(i);
                print(map.getMarkerLong(i) + " / " + map.getMarkerLat(i));
                map.drawGPSInfo(pos);
            }
        }
        // Update is called once per frame
        
        void Update()
        {
            //지도 움직일 때 마커의 움직임
            SaveLoadBehavior SLB = GameObject.Find("GameObject").GetComponent<SaveLoadBehavior>();

            if (UnityEngine.Input.GetMouseButton(0)&&!SLB.usingUI)
            {

                gos = GameObject.FindGameObjectsWithTag("Marker");
                Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo))
                {
                    Vector3 displacement = Vector3.zero;
                    if (lastHitPosition != Vector3.zero)
                    {
                        displacement = hitInfo.point - lastHitPosition;
                    }
                    lastHitPosition = new Vector3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);

                    if (displacement != Vector3.zero)
                    {
                        // update the marker position
                        foreach (GameObject go in gos)
                        {
                            go.transform.position += new Vector3(displacement.x, 0, displacement.z);
                            
                        }
                    }
                }
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                // reset the last hit position
                lastHitPosition = Vector3.zero;
            }

            // 지도 축소, 확대할 떄 마커의 크기비율
            if(Camera.main.transform.position.y>lastCameraScale)
            {
                gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x * zoomScale,
                    gameObject.transform.localScale.y *zoomScale, gameObject.transform.localScale.z *zoomScale);
                print(gameObject.transform.localScale.x);

                lastCameraScale = Camera.main.transform.position.y;
            }
            else if(Camera.main.transform.position.y<lastCameraScale)
            {
                gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x *(1/zoomScale),
                    gameObject.transform.localScale.y *(1/zoomScale), gameObject.transform.localScale.z *(1/zoomScale));
                lastCameraScale = Camera.main.transform.position.y;

                print(gameObject.transform.localScale.x);
            }

        }
    }
}