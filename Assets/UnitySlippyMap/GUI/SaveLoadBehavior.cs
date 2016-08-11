using UnityEngine;
using System.Collections.Generic;
using UnitySlippyMap.Map;
using UnitySlippyMap.Markers;
using GcsProject.Controller;
using UnitySlippyMap.UserGUI;
using UnityEngine.UI;
public class SaveLoadBehavior : MonoBehaviour {
    public Rect doWindowSave;
    public Rect doWindowLoad;
    public bool usingUI = false;
    public static bool renderSave = false;  // to show Save windows
    public static bool renderLaod = false;   // to show Load windows
    private List<string> savePathName = new List<string>(); // path name 저장
    private double[][]savePathLat =new double[100][]; //지도상의 Marker의 위도 모두 저장
    private double[][] savePathLong = new double[100][]; // 지도상의 Marker의 경도 모두 저장
    private int[] pathLength = new int[100];    // 저장된 path의 길이
    private int pathCnt = 0;
    private int selected = 0;
    string strSave = "";
    private int key = 0;
    private int droneKey=0;
    private bool runOnce=true;
    private List<PositionDouble> markerPos = new List<PositionDouble>();

    //string str = "";
    public void getKey(int key)
    {
        MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        switch(key)
        {
            case 1: // Save
                renderSave = true;
                runOnce = true;
                break;
            case 2: // Load
                renderLaod = true;
                runOnce = true;
                break;
            case 3: //Apply
                sendPlan(map);
                break;

        }
    }
    /// <summary>
    /// send plan to controller by giving dronekey and drone Position
    /// </summary>
    /// <param name="map"></param>
    public void sendPlan(MapBehaviour map)
    {
        Text keyVal = GameObject.Find("Key").GetComponent<Text>();
        GcsController controller = GameObject.Find("GameObject").GetComponent<GcsController>();
        droneKey = int.Parse(keyVal.text);
        for(int i=0; i<map.getMarkerCnt();i++)
        {
            markerPos.Add(new PositionDouble(map.getMarkerLong(i), map.getMarkerLat(i), map.getMarkerAlt(i)));
        }
       controller.SetPlan(droneKey, markerPos);
       markerPos.Clear();
    }
    void OnGUI()
    {
        if (renderSave)
        {
            usingUI = true;
            //make windows
            GUI.color = Color.cyan;
            doWindowSave = GUI.Window(0, new Rect(Screen.width-600, 100, 120, 105), DoWindowSave, "Save Track");
        }
        if (renderLaod)
        {
            usingUI = true;
            GUI.color = Color.cyan;
            doWindowLoad = GUI.Window(1, new Rect(Screen.width - 600, 100-10*pathCnt, 130, 100 + 20 * pathCnt), DoWindowLoad, "Load Track");
        }
    }
    void DoWindowSave(int windowID)
    {
        MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        strSave = GUI.TextField(new Rect(20, 30, 80, 20), strSave);
        if (GUI.Button(new Rect(35, 57, 50, 20), "Save")) // Save 버튼 클릭
        {
            Debug.Log("Save CLicked");
            savePathName.Add(strSave);
            print(savePathName[pathCnt] + " : " + pathCnt);
            savePathLong[pathCnt] = new double[map.getMarkerCnt()];
            savePathLat[pathCnt] = new double[map.getMarkerCnt()];
            for (int i = 0; i < map.getMarkerCnt(); i++)
            {
                savePathLong[pathCnt][i] = map.getMarkerLong(i);
                savePathLat[pathCnt][i] = map.getMarkerLat(i);
                print(savePathName[pathCnt] + " : " + savePathLong[pathCnt][ i] + "  " + savePathLat[pathCnt][i]);
            }
            //print("save pathname coutn : " + savePathName.Count);
            pathLength[pathCnt] = map.getMarkerCnt();
            pathCnt=savePathName.Count;
            strSave = "";
            renderSave = false;
            notUseUI();
        }
        if (GUI.Button(new Rect(35, 81, 50, 20), "Close"))
        {
            notUseUI();
            renderSave = false;
        }
    }
    /// <summary>
    /// 여행경로 Load 창
    /// </summary>
    /// <param name="windowID"></param>
    void DoWindowLoad(int windowID)
    {
        MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        double[] tempPos = new double[2];
        selected =GUI.SelectionGrid(new Rect(20, 25, 90, 20*pathCnt), selected, savePathName.ToArray(), 1);
        if(GUI.Button(new Rect(30,55+20*pathCnt,70,20),"Load"))
        {
            ButtonBehavior delete = GameObject.Find("GameObject").GetComponent<ButtonBehavior>();
            //print(map.CenterWGS84[0] + "  "+map.CenterWGS84[1]);
            delete.doClear();
            notUseUI();
            for(int i=0;i< pathLength[selected]; i++)
            {
                print(" i :" + i);
                tempPos[0] = savePathLong[selected][i];
                tempPos[1] = savePathLat[selected][i];
                map.saveMarker(tempPos);
                map.drawGPSInfo(tempPos);
                map.drawMarker(tempPos);
            }
            tempPos.Initialize();
            renderLaod = false;
            
        }
        if(GUI.Button(new Rect(30,78+20*pathCnt,70,20),"Close"))
        {
            notUseUI();
            renderLaod = false;
            tempPos.Initialize();
        }
        
    }
    void notUseUI()
    {
        if (runOnce)
        {
            
            //Debug.Log("~~" + usingUI);
            usingUI = false;
            runOnce = false;
           // Debug.Log("~~" + usingUI);
        }
    }
}
    // Use this for initialization



