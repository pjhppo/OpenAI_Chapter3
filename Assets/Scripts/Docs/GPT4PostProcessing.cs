using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.IO;

public class GPT4PostProcessing : MonoBehaviour
{
    public string openaiApiKey = "YOUR_API_KEY"; // 여기서 YOUR_API_KEY를 실제 OpenAI API 키로 교체하세요.
    private static string whisperApiUrl = "https://api.openai.com/v1/audio/transcriptions";
    private static string chatApiUrl = "https://api.openai.com/v1/chat/completions";

    public string audioFilePath = "Assets/Audio/audio.wav"; // 음성인식할 오디오 파일의 경로를 지정하세요.

    void Start()
    {
        StartCoroutine(ProcessAudio());
    }

    IEnumerator ProcessAudio()
    {
        // 1. Whisper API를 사용하여 오디오 음성인식
        string transcriptionText = null;
        yield return StartCoroutine(TranscribeAudio(result => transcriptionText = result));

        if (string.IsNullOrEmpty(transcriptionText))
        {
            Debug.LogError("Transcription failed.");
            yield break;
        }

        // 2. GPT-4를 사용하여 음성인식본 후처리
        string correctedText = null;
        yield return StartCoroutine(GenerateCorrectedTranscript(transcriptionText, result => correctedText = result));

        if (string.IsNullOrEmpty(correctedText))
        {
            Debug.LogError("GPT-4 correction failed.");
            yield break;
        }

        // 3. 교정된 텍스트 출력
        Debug.Log("Corrected Text: " + correctedText);
    }

    IEnumerator TranscribeAudio(System.Action<string> callback)
    {
        byte[] audioData = File.ReadAllBytes(audioFilePath);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        string fileName = Path.GetFileName(audioFilePath);
        string mimeType = GetMimeType(audioFilePath);

        formData.Add(new MultipartFormFileSection("file", audioData, fileName, mimeType));
        formData.Add(new MultipartFormDataSection("model", "whisper-1"));
        formData.Add(new MultipartFormDataSection("response_format", "text"));

        UnityWebRequest request = UnityWebRequest.Post(whisperApiUrl, formData);
        request.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Whisper API Error: " + request.error);
            callback(null);
        }
        else
        {
            string responseText = request.downloadHandler.text;
            callback(responseText);
        }
    }

    IEnumerator GenerateCorrectedTranscript(string transcriptionText, System.Action<string> callback)
    {
        // 시스템 프롬프트 정의
        string systemPrompt = "당신은 ZyntriQix 회사의 유용한 어시스턴트입니다. 당신의 임무는 음성인식된 텍스트에서 철자 오류를 교정하는 것입니다. 다음 제품들의 이름이 정확하게 철자되었는지 확인하세요: ZyntriQix, Digique Plus, CynapseFive, VortiQore V8, EchoNix Array, OrbitalLink Seven, DigiFractal Matrix, PULSE, RAPT, B.R.I.C.K., Q.U.A.R.T.Z., F.L.I.N.T. 필요한 구두점(마침표, 쉼표, 대문자 등)만 추가하고, 제공된 컨텍스트만 사용하세요.";

        // 요청 페이로드 생성
        var chatRequest = new ChatCompletionRequest
        {
            model = "gpt-4",
            temperature = 0,
            messages = new List<Message>
            {
                new Message { role = "system", content = systemPrompt },
                new Message { role = "user", content = transcriptionText }
            }
        };

        string jsonData = JsonUtility.ToJson(chatRequest);

        UnityWebRequest request = new UnityWebRequest(chatApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("ChatGPT API Error: " + request.error);
            callback(null);
        }
        else
        {
            string responseText = request.downloadHandler.text;
            GPTResponse response = JsonUtility.FromJson<GPTResponse>(responseText);

            if (response != null && response.choices != null && response.choices.Count > 0)
            {
                string correctedText = response.choices[0].message.content;
                callback(correctedText);
            }
            else
            {
                Debug.LogError("Invalid response from GPT-4.");
                callback(null);
            }
        }
    }

    string GetMimeType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        switch (extension)
        {
            case ".wav": return "audio/wav";
            case ".mp3": return "audio/mpeg";
            // 지원되는 다른 오디오 형식에 대한 MIME 타입 추가 가능
            default: return "application/octet-stream";
        }
    }

    // JSON 직렬화를 위한 클래스 정의
    [System.Serializable]
    public class ChatCompletionRequest
    {
        public string model;
        public float temperature;
        public List<Message> messages;
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
