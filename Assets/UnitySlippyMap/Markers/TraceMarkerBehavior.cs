﻿using UnityEngine;
using System.Collections;
using GcsProject.Model;
using UnitySlippyMap.DroneStruct;
namespace UnitySlippyMap.Markers
{
    public class TraceMarkerBehavior : MonoBehaviour
    {
        private bool renderPop = false;
        private Rect doWindowPop;
        private DroneInfo drone;
        public GUIStyle style;
        private static Vector3 lastHitPosition=Vector3.zero;
        private GameObject[] gos;
        private double longtitude;
        private double latitude;
        private double altitude;
        private double groundSpeed;
        // Use this for initialization
        void Start()
        {
            style.normal.textColor = Color.white;
        }
        void OnMouseEnter()
        {
            renderPop = true;
        }
        void OnMouseExit()
        {
            renderPop = false;
        }
        void OnGUI()
        {
            if (renderPop)
            {
                doWindowPop = GUI.Window(0, new Rect(Input.mousePosition.x, Input.mousePosition.y + 30, 180, 100), DoWindowPop, "Info");
            }
        }
        void DoWindowPop(int WindowID)
        {
            GUI.Label(new Rect(20, 20, 80, 80), " Altitude : " + altitude + "\n Longtitude : " + longtitude
                + "\n Latitude : " + latitude + "\n Speed : "+groundSpeed, style);
        }
        // Update is called once per frame
        void Update()
        {

            SaveLoadBehavior SLB = GameObject.Find("GameObject").GetComponent<SaveLoadBehavior>();

            if (UnityEngine.Input.GetMouseButton(0) && !SLB.usingUI)
            {

                gos = GameObject.FindGameObjectsWithTag("Trace");
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

        }
    }
}