using UnityEngine;
using System.Collections;
public class AudioRecorder : MonoBehaviour
{
    public int maxRecordingTime = 60; // 최대 녹음 시간 (초)
    private AudioClip recordedClip;
    IEnumerator StartRecording()
    {
        string microphone = Microphone.devices[0]; // 마이크 입력 장치 이름 가져오기
        
        // 녹음 시작
        recordedClip = Microphone.Start(microphone, false, maxRecordingTime, 44100);
        Debug.Log("Recording started...");
        yield return new WaitForSeconds(maxRecordingTime); // 최대 녹음 시간까지 대기      
        Microphone.End(microphone); // 녹음 중지
        Debug.Log("Recording stopped.");
        // 녹음된 오디오 처리 (저장 또는 전송)
    }
}
