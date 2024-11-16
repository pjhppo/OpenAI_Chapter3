using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;

public class TranslatorManager : MonoBehaviour
{
    public static TranslatorManager Instance;
    private string currentPrompt = "다음 언어로 번역을 해주세요 : English";
    private const string apiUrl = "https://api.openai.com/v1/chat/completions";
    public string apiKey;
    public Dropdown dropdown;
    private string[] languages = { "English", "Korean", "Japanese", "Chinese" };
    public Text uiText;
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

    private void Start()
    {
        WhisperManager.Instance.OnReceivedWhisper += RecievedWhisper;

        InitializeDropdown();
    }

    private void RecievedWhisper(string transcribedText)
    {
        Debug.Log(transcribedText);
        StartCoroutine(SendOpenAIRequest(currentPrompt, transcribedText, uiText));
    }

    void InitializeDropdown()
    {
        // Dropdown 옵션 설정
        dropdown.ClearOptions();
        List<string> options = new List<string>(languages);
        dropdown.AddOptions(options);

        // 초기 선택된 언어 설정
        currentPrompt = "다음 언어로 번역을 해주세요 :" + languages[0];

        // Dropdown 값 변경 시 호출될 리스너 추가
        dropdown.onValueChanged.AddListener(delegate { DropdownValueChanged(dropdown); });
    }

    void OnDropdownValueChanged(Dropdown change)
    {
        // 선택된 언어의 이름으로 targetLanguage 업데이트
        currentPrompt = "다음 언어로 번역을 해주세요 :" + change.options[change.value].text;
        Debug.Log($"선택된 언어: {change.value}");
    }

    void DropdownValueChanged(Dropdown change)
    {
        currentPrompt = "다음 언어로 번역 :" + change.options[change.value].text;
        Debug.Log(currentPrompt);
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
                }
                else
                {
                    Debug.LogWarning("No valid response from the assistant.");
                }
            }
        }
    }
}