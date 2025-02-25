using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class WhisperTranscriptionWithTimestamps : MonoBehaviour
{
    private string apiUrl = "https://api.openai.com/v1/audio/transcriptions";
    public string apiKey = "YOUR_API_KEY"; // OpenAI API 키를 여기에 입력하세요.
    public string audioFilePath = "Assets/Audio/audio.mp3";

    private void Start()
    {
        StartCoroutine(UploadAudio());
    }

    private IEnumerator UploadAudio()
    {
        byte[] audioData = File.ReadAllBytes(audioFilePath);
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("file", audioData, "audio.mp3", "audio/mpeg"));
        formData.Add(new MultipartFormDataSection("model", "whisper-1"));
        formData.Add(new MultipartFormDataSection("response_format", "verbose_json"));
        formData.Add(new MultipartFormDataSection("timestamp_granularities[]", "word"));
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
            Debug.Log("Transcription with Timestamps: " + responseText);
        }
    }
}
