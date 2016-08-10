/*
 * UI Manager는 Unity 3D가 싱글 스레드 형태로 동작하기 때문에,
 * 별도의 스레드 상에서 UI를 수정하는(Unity를 건드리는) 작업을 할 경우
 * 스레드 관련 오류가 발생하게 됨.
 * 따라서, 이를 해결하기 위해 별도의 작업 Queue를 만들고,
 * Unity 3D에서 제공하는 코루틴(Coroutine)으로 메인 스레드에서
 * Queue를 계속 확인하고 처리하는 방법으로 문제를 해결함.
 * 작업 Queue로 ConcurrentQueue를 사용하므로, Queue를 등록하고 처리하는 과정에서의
 * 동시성 제어 문제를 예방함. - Kero Kim -
 */
using GcsProject.Model;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnitySlippyMap.DroneStruct;
using UnitySlippyMap.Map;
using UnitySlippyMap.UserGUI;

class UIManager : MonoBehaviour
{
    private DronePanelBehavior dronePanel;
    private MapBehaviour mapBehavior;

    /// <summary>
    /// UIMessage 메시지 타입 구분
    /// </summary>
    public enum UIMessageType
    {
        // 이곳에 UI 작업과 관련된 메시지를 추가
        // 아래의 ProcessQueue에 추가한 메시지에 대한 처리 코드를 작성하면 됨.
        DrawDroneMarker, // 드론 Marker 출력 요청
        DrawTraceMarker, // 드론 자취 Marker 출력 요청
        DrawPlanMarker, // 운행 계획 Marker 출력 요청
        PrintDroneInfo, // 드론 운행 정보 출력 요청
        PrintPlanList, // 운행 계획 리스트 출력 요청
        ConnectedComplete, // 연결 성공에 대한 이벤트 발생 요청
        PrintConnectList, // 연결 정보 리스트를 출력
        SendID  // 드론 팝업 정보 요청
    }
    public struct UIMessage
    {
        public UIMessageType id;
        public object[] parameters; // 전달할 데이터 배열
    }
    private ConcurrentQueue<UIMessage> queue; // 작업 Queue

    void Awake()
    {
        queue = new ConcurrentQueue<UIMessage>();
        dronePanel = GameObject.Find("GameObject").GetComponent<DronePanelBehavior>();
        mapBehavior = GameObject.Find("Test").GetComponent<MapBehaviour>();
        StartCoroutine(CheckQueue()); // 지속적으로 Queue를 확인
    }
    /// <summary>
    /// 작업 Queue에 수행할 작업을 등록
    /// </summary>
    /// <param name="msg"></param>
    public void Push(UIMessage msg)
    {
        queue.Enqueue(msg);
    }
    /// <summary>
    /// 작업 Queue를 확인
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckQueue()
    {
        while(true)
        {
            UIMessage msg;
            if (queue.TryDequeue(out msg)) // Queue에서 수행할 작업이 있는 경우 True
            {
                ProcessQueue(msg);
            }
            yield return new WaitForSeconds(0.001f); // Queue 확인 주기 : 0.001초
        }
    }
    /// <summary>
    /// 작업을 처리
    /// </summary>
    /// <param name="msg"></param>
    private void ProcessQueue(UIMessage msg)
    {
        // 작업 Queue에 등록된 작업들을 처리하는 부분
        // 이곳에 메시지에 대한 UI 작업을 등록하면 됨.
        switch (msg.id)
        {
            case UIMessageType.DrawDroneMarker:
                dronePanel.setDronePosByKey((int)msg.parameters[0], (double[])msg.parameters[1]);
                break;
            case UIMessageType.DrawPlanMarker:
                mapBehavior.drawMarker((PositionDouble)msg.parameters[0]);
                break;
            case UIMessageType.DrawTraceMarker:
                dronePanel.setTraceMarkerByKey((int)msg.parameters[0], (double[])msg.parameters[1]);
                break;
            case UIMessageType.PrintDroneInfo:
                dronePanel.setDroneInfo((DroneInfo)msg.parameters[0]);
                break;
            case UIMessageType.PrintPlanList:
                mapBehavior.drawGPSInfo((PositionDouble)msg.parameters[0]);
                mapBehavior.setListCnt(0); // 운행 계획 리스트 출력 후 카운트를 초기화 해 주어야 함
                    break;
            case UIMessageType.ConnectedComplete:
                dronePanel.connectComplete((int)msg.parameters[0]);
                break;
            case UIMessageType.PrintConnectList:
                dronePanel.getLoadList((List<ConnectList.ConnectStruct>)msg.parameters[0]);
                break;
            case UIMessageType.SendID:
                dronePanel.getID((byte)msg.parameters[0], (byte)msg.parameters[1]);
                break;
        }
    }
}
