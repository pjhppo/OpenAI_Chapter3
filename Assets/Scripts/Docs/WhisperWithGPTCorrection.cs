using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Text;

public class WhisperWithGPTCorrection : MonoBehaviour
{
    private string openaiApiKey = "YOUR_API_KEY"; // OpenAI API 키를 여기에 입력하세요.
    private string whisperApiUrl = "https://api.openai.com/v1/audio/transcriptions";
    private string chatApiUrl = "https://api.openai.com/v1/chat/completions";

    public string audioFilePath = "Assets/Audio/Postprocessing.mp3"; // 오디오 파일의 경로를 지정하세요.

    private void Start()
    {
        StartCoroutine(ProcessAudio());
    }

    private IEnumerator ProcessAudio()
    {
        // 1. Whisper API로 오디오 전사
        string transcriptionText = "";
        byte[] audioData = File.ReadAllBytes(audioFilePath);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        formData.Add(new MultipartFormFileSection("file", audioData, "speech.mp3", "audio/mpeg"));
        formData.Add(new MultipartFormDataSection("model", "whisper-1"));

        UnityWebRequest whisperRequest = UnityWebRequest.Post(whisperApiUrl, formData);
        whisperRequest.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);

        yield return whisperRequest.SendWebRequest();

        if (whisperRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Whisper API Error: " + whisperRequest.error);
            yield break;
        }
        else
        {
            transcriptionText = whisperRequest.downloadHandler.text;
            Debug.Log("Transcription: " + transcriptionText);
        }

        // 2. GPT-4로 후처리
        string systemPrompt = "당신은 ZyntriQix 회사의 유용한 어시스턴트입니다. " +
            "당신의 임무는 전사된 텍스트에서 철자 오류를 교정하는 것입니다. " +
            "다음 제품들의 이름이 정확하게 철자되었는지 확인하세요: " +
            "ZyntriQix, Digique Plus, CynapseFive, VortiQore V8, EchoNix Array, " +
            "OrbitalLink Seven, DigiFractal Matrix, PULSE, RAPT, B.R.I.C.K., " +
            "Q.U.A.R.T.Z., F.L.I.N.T. " +
            "필요한 구두점(마침표, 쉼표, 대문자 등)만 추가하고, 제공된 컨텍스트만 사용하세요.";

        // ChatGPT 요청 생성
        string postData = CreateChatCompletionRequest(systemPrompt, transcriptionText);

        UnityWebRequest chatRequest = new UnityWebRequest(chatApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
        chatRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        chatRequest.downloadHandler = new DownloadHandlerBuffer();
        chatRequest.SetRequestHeader("Content-Type", "application/json");
        chatRequest.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);

        yield return chatRequest.SendWebRequest();

        if (chatRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("ChatGPT API Error: " + chatRequest.error);
        }
        else
        {
            string responseText = chatRequest.downloadHandler.text;
            Debug.Log("Corrected Text: " + responseText);

            // JSON 파싱하여 최종 텍스트 추출
            GPTResponse gptResponse = JsonUtility.FromJson<GPTResponse>(responseText);
            string correctedText = gptResponse.choices[0].message.content;

            Debug.Log("Final Corrected Text: " + correctedText);
        }
    }

    string CreateChatCompletionRequest(string systemPrompt, string userMessage)
    {
        ChatCompletionRequest request = new ChatCompletionRequest
        {
            model = "gpt-4",
            messages = new List<Message>
            {
                new Message { role = "system", content = systemPrompt },
                new Message { role = "user", content = userMessage }
            },
            temperature = 0.0f
        };

        return JsonUtility.ToJson(request);
    }

    // 클래스 정의
    [System.Serializable]
    public class ChatCompletionRequest
    {
        public string model;
        public List<Message> messages;
        public float temperature;
    }

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class GPTResponse
    {
        public List<Choice> choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }
}
