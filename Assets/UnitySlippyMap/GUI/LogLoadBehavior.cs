using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.UI;
using UnitySlippyMap.Map;
using UnitySlippyMap.Helpers;
using System.Threading;
using System;
using System.Collections.Generic;
using UnitySlippyMap.UserGUI;
using System.IO;
using GcsProject.Controller;
using GcsProject.Model;
public class LogLoadBehavior : MonoBehaviour {
    public Transform droneMarker;
    private Transform newDrone;
    private Text droneKeyField;
    private string dirPath;
    private List<double> longtitude;
    private List<double> latitude;
    private ManualResetEvent replayLogeve;
    private DronePanelBehavior dronePanel;
    private int indexPos;
    private Timer droneTimer;
    private UIManager manager;
    // Use this for initialization
    void Start () {
        latitude = new List<double>();
        longtitude = new List<double>();

	}

	
    void Awake()
    {
        dronePanel = GameObject.Find("GameObject").GetComponent<DronePanelBehavior>();
        droneKeyField = GameObject.Find("Key").GetComponent<Text>();
    }
    public void getKey(int btnKey)
    {
        switch(btnKey)
        {
            case 1:                 // open Log
                dirPath = EditorUtility.OpenFilePanel("open log file", dirPath, "txt");
                readFile(dirPath);
                break;
            case 2:                 // play log
                indexPos = 0;
                newDrone = Instantiate(droneMarker);
                newDrone.name = "Drone";
                Vector3 vec = new Vector3(0, 0, 0);
                newDrone.transform.position = vec;
                droneTimer = new Timer(replayDrone, replayLogeve, 0, 250);
                break;
               
        }
    }
    /// <summary>
    /// 파일 불러오기.
    /// </summary>
    /// <param name="path"></param>
    public void readFile(string path)
    {
        string tokken;
        string[] tknArr;
        //string delimiters = @"\[(\d+)([-])(\d+)([-])(\d+)\s+(\d+)\:(\d+)\:(\d+)\]";
        InputField logNameField = GameObject.Find("Log Name").GetComponent<InputField>();
        StreamReader sr = new StreamReader(path);
        while((tokken=sr.ReadLine())!=null)
        {
            tknArr = tokken.Split(',');
            latitude.Add(double.Parse(tknArr[1])/1E7);
            longtitude.Add(double.Parse(tknArr[2])/1E7);
        }
        logNameField.text = path.Substring(3);
    }
    /// <summary>
    /// Log 파일 복기
    /// </summary>
    /// <param name="path"></param>
    public void replayDrone(object obj)
    {
        double[] pos = new double[2];
        pos[0] = longtitude[indexPos];
        pos[1] = latitude[indexPos];
        MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        pos = GeoHelpers.WGS84ToRaycastHit(map, pos);     // drone 경도,위도를 screen 좌표로 변환
        try
        {
            Vector3 posVec = new Vector3((float)pos[0],0,(float)pos[1]);
            newDrone.transform.position = posVec;
        }
        catch(ArgumentNullException e)
        {
            droneTimer.Change(Timeout.Infinite, System.Threading.Timeout.Infinite);
        }
        indexPos++;
    }

    public void replayTrace(object obj)
    {

        dronePanel.setTraceMarkerByKey(int.Parse(droneKeyField.text), new double[] { longtitude[indexPos], latitude[indexPos] });
    }

}
