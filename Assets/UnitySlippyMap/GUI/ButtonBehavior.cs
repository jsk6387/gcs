using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnitySlippyMap.Map;
using UnitySlippyMap.Helpers;
namespace UnitySlippyMap.UserGUI
{

    public class ButtonBehavior : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        #region Variables
        public static ButtonBehavior instance=null;
        float rotateAngle = 0.1f;
        public bool isRightDown;
        public bool isLeftDown;
        public bool isZoomIn;
        public bool isZoomOut;
        public int key = 0;
        private GameObject[] gos;
        private float downTime;
        private int i = 0;
        private float destinationAngle;
        private float perspectiveAngle;
        private float animationStartTime;
        public Rect doWindow0;
        public static bool render = false;
        public string str = "";
        private Vector3 lastHitPosition=Vector3.zero;
        #endregion
        void awake()
        {
            instance = this;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            TestMap TM = GameObject.Find("Test").GetComponent<TestMap>();
            MapBehaviour map = GameObject.Find("Test").GetComponent<Map.MapBehaviour>();
            
            switch (key)
            {
                case 1:  //Right
                    this.isRightDown = true;
                    this.downTime = Time.realtimeSinceStartup;
                    break;
                case 2: // Left
                    this.isLeftDown = true;
                    break;
                case 3: // 2D/3D
                    Debug.Log(i);
                    if (i == 0 || i == 1 || i == 2 || i == 3 || i == 4)
                    {
                        i++;
                        TM.setDestAngle(TM.getPerAngle());
                    }
                    else
                    {
                        TM.setDestAngle(-i * TM.getPerAngle());
                        i = 0;
                    }
                    TM.setAniStartTime(Time.time);
                    break;
                case 4:  // Clear
                    doClear();
                    break;
                case 5:  // "Go"
                    // " ___ " 라는 이름의 GameObject에서 InputField Component를 갖고옴.
                    InputField longInput = GameObject.Find("Longtitude_input").GetComponent<InputField>();
                    InputField latInput = GameObject.Find("Latitude_input").GetComponent<InputField>();
                    //입력된 gps정보를 저장
                    double[] gps=new double[] { double.Parse(longInput.text), double.Parse(latInput.text) };
                    
                    //map.saveMarker(gps);
                    //map.drawMarker(gps);
                    //map.drawGPSInfo(gps);
                    goPostion(map, gps);
                    //map.CenterWGS84[0] = gps[0];
                    //map.CenterWGS84[1] = gps[1];
                    //gps = GeoHelpers.WGS84ToRaycastHit(map,gps);
                    //Vector3 gpsvec = new Vector3((float)gps[0], 0, (float)gps[1]);
                    //Camera.main.transform.LookAt(gpsvec);
                    
                    break;
                case 6: // Zoom In
                    isZoomIn = true;    
                    break;
                case 7:  // Zoom OUT
                    isZoomOut = true;
                    break;
            }
            
        }
        /// <summary>
        /// 해당 위치로 이동
        /// </summary>
        /// <param name="map"></param>
        /// <param name="pos"></param>
        public void goPostion(MapBehaviour map, double[] pos)
        {
            pos = GeoHelpers.WGS84ToRaycastHit(map, pos);
            Vector3 posVec = new Vector3((float)pos[0], 0, (float)pos[1]);
            map.UpdatesCenterWithLocation = false;
            
            // apply the movements
            print(posVec.x + "/" + posVec.z);
                Vector3 displacement = posVec;
                if (displacement != Vector3.zero)
                {
                    // update the centerWGS84 property to the new centerWGS84 wgs84 coordinates of the map
                    double[] displacementMeters = new double[2] {
                            displacement.x / map.RoundedScaleMultiplier,
                            displacement.z / map.RoundedScaleMultiplier
                        };
                    double[] centerMeters = new double[2] {
                            map.CenterEPSG900913 [0],
                            map.CenterEPSG900913 [1]
                        };
                    centerMeters[0] -= displacementMeters[0];
                    centerMeters[1] -= displacementMeters[1];
                    map.CenterEPSG900913 = centerMeters;
#if DEBUG_LOG
    					Debug.Log("DEBUG: Map.Update: new centerWGS84 wgs84: " + centerWGS84[0] + ", " + centerWGS84[1]);
#endif
                }
                map.HasMoved = true;
            lastHitPosition = Vector3.zero;
            map.IsDirty = true;
        }
        /// <summary>
        /// Clear the Screen
        /// </summary>
        public void doClear()
        {
            Map.MapBehaviour map = GameObject.Find("Test").GetComponent<Map.MapBehaviour>();
            DeleteObjects("Marker");
            DeleteObjects("GPSfield");
            DeleteObjects("Order");
            DeleteObjects("Trace");
            map.getMarkerLat().Clear();
            map.getMarkerLong().Clear();
        }
        /// <summary>
        /// 운행계획만 지움
        /// </summary>
        public void doClearPlan()
        {
            Map.MapBehaviour map = GameObject.Find("Test").GetComponent<Map.MapBehaviour>();
            DeleteObjects("Marker");
            DeleteObjects("GPSfield");
            DeleteObjects("Order");
            map.getMarkerLat().Clear();
            map.getMarkerLong().Clear();
        }
        /// <summary>
        /// Delete Objects with Tag
        /// </summary>
        public void DeleteObjects(string tag)
        {
            gos = GameObject.FindGameObjectsWithTag(tag);
            foreach(GameObject go in gos)
            {
                Destroy(go);
            }
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            switch (key)
            {
                case 1:
                    this.isRightDown = false; 
                    break;
                case 2:
                    this.isLeftDown = false;
                    break;
                case 6:
                    this.isZoomIn = false;
                    break;
                case 7:
                    this.isZoomOut = false;
                    break;
            }

        }
        void Update()
        {
            switch (key)
            {
                case 1:
                    if (!this.isRightDown) return;
                    break;
                case 2:
                    if (!this.isLeftDown) return;
                    break;
                
            }
           
        }
        

    }
}