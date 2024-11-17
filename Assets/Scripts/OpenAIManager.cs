using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class OpenAIManager : MonoBehaviour
{
    private const string apiUrl = "https://api.openai.com/v1/chat/completions";    
    public string apiKey;
    public string currentPrompt = "입력된 텍스트를 기반으로 회의록 작성. 구조는 회의 제목, 날짜 및 장소, 주요 의제, 회의내용, 주요 논의 사항 등 요약";
    public Text uiText;
    public event Action OnReceivedMessage;
    public static OpenAIManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start(){
        WhisperManager.Instance.OnReceivedWhisper += RecievedWhisper;
    }

    private void RecievedWhisper(string transcribedText){
        StartCoroutine(SendOpenAIRequest(currentPrompt, transcribedText, uiText));
    }

    // OpenAI API에 요청을 보내는 코루틴 함수
    public IEnumerator SendOpenAIRequest(string prompt, string message, Text resultText)
    {
        // JSON 형식의 데이터를 생성
        string jsonData = @"{
            ""model"": ""gpt-4o"",
            ""messages"": [
                {
                    ""role"": ""system"",
                    ""content"": """ + prompt + @"""
                },
                {
                    ""role"": ""user"",
                    ""content"": """ + message + @"""
                }
            ]
        }";

        // UTF-8 인코딩으로 JSON 데이터를 바이트 배열로 변환
        byte[] postData = Encoding.UTF8.GetBytes(jsonData);

        // UnityWebRequest를 사용하여 POST 요청을 생성
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            // 요청 헤더 설정
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            // 요청 데이터 설정
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 요청 전송
            yield return request.SendWebRequest();

            // 에러 핸들링
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                // 응답 처리
                string responseText = request.downloadHandler.text;
                Debug.Log("Response: " + responseText);

                // 응답 데이터에서 assistant의 메시지 추출
                var responseData = JsonUtility.FromJson<TextGenerationResponse>(responseText);
                if (responseData.choices != null && responseData.choices.Length > 0)
                {
                    string assistantMessage = responseData.choices[0].message.content;
                    resultText.text = assistantMessage;
                    OnReceivedMessage?.Invoke();
                }
                else
                {
                    Debug.LogWarning("No valid response from the assistant.");
                }
            }
        }
    }
}

[System.Serializable]
public class TextGenerationResponse
{
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

[System.Serializable]
public class Message
{
    public string content;
}