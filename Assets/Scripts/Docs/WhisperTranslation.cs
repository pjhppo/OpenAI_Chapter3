using UnityEngine;
  using UnityEngine.Networking;
  using System.Collections;
  using System.Collections.Generic;
  using System.IO;


  public class WhisperTranslation : MonoBehaviour
  {
    private string apiUrl = "https://api.openai.com/v1/audio/translations";
    private string apiKey = "YOUR_API_KEY";
    public string audioFilePath = "Assets/Audio/german.mp3"; // 예시로 독일어 오디오 파일


    IEnumerator UploadAudio()
    {
       byte[] audioData = File.ReadAllBytes(audioFilePath);
       List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
       formData.Add(new MultipartFormFileSection("file", audioData, "german.mp3", "audio/mpeg"));
       formData.Add(new MultipartFormDataSection("model", "whisper-1"));
       UnityWebRequest www = UnityWebRequest.Post(apiUrl, formData);
       www.SetRequestHeader("Authorization", "Bearer " + apiKey);


       yield return www.SendWebRequest();


       if (www.result != UnityWebRequest.Result.Success)
       {
           Debug.Log("Error: " + www.error);
       }
       else
       {
           string responseText = www.downloadHandler.text;
           Debug.Log("Translation Result: " + responseText);
       }
    }
  }
