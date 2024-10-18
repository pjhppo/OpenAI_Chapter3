using UnityEngine;
 using UnityEngine.Networking;
 using System.Collections;
 using System.Collections.Generic;
 using System.IO;


public class WhisperTranscription : MonoBehaviour
{
   private string apiUrl = "https://api.openai.com/v1/audio/transcriptions";
   public string apiKey = "YOUR_API_KEY"; // OpenAI API 키를 여기에 입력하세요.
   public string audioFilePath = "Assets/Audio/audio.mp3"; // 오디오 파일 경로

   private void Start(){
     StartCoroutine(UploadAudio());
   }
   IEnumerator UploadAudio()
   {
       // 오디오 파일을 바이트 배열로 읽어옵니다.
       byte[] audioData = File.ReadAllBytes(audioFilePath);


       // 폼 데이터 생성
       List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
       formData.Add(new MultipartFormFileSection("file", audioData, "audio.mp3", "audio/mpeg"));
       formData.Add(new MultipartFormDataSection("model", "whisper-1"));


       // 요청 생성
       UnityWebRequest www = UnityWebRequest.Post(apiUrl, formData);
       www.SetRequestHeader("Authorization", "Bearer " + apiKey);
       // 요청 보내기
       yield return www.SendWebRequest();
       if (www.result != UnityWebRequest.Result.Success)
       {
           Debug.Log("Error: " + www.error);
       }
       else
       {
           // 응답 받기
           string responseText = www.downloadHandler.text;
           Debug.Log("Transcription Result: " + responseText);
       }
   }
}
