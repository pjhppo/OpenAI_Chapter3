using System.Collections;
using UnityEngine;

public class NPCManager : MonoBehaviour
{    
    public Animator anim; // NPC의 애니메이터
    public GameObject balloon; //말풍선 프리팹
    public static NPCManager Instance; // 싱글톤 인스턴스

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 객체 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // WhisperManager에서 녹음이 시작되면 OnInputFieldChanged 메서드가 호출됩니다.
        WhisperManager.Instance.OnStartRecording += OnInputFieldChanged;

        // WhisperManager에서 녹음이 중지되면 OnInputFieldSubmit 메서드가 호출됩니다.
        WhisperManager.Instance.OnStopRecording += OnInputFieldSubmit;
        
        // WhisperManager의 OnReceivedWhisper 이벤트에 StartTalk 메서드를 구독합니다.
        OpenAIManager.Instance.OnReceivedMessage += StartTalk;
    }

    // InputField에서 입력이 발생했을 때 호출되는 메서드
    private void OnInputFieldChanged()
    {
        // 애니메이터에 "listening" 트리거 설정
        anim.SetTrigger("listen");
        Debug.Log("OnInputFieldEdited");
    }

    // InputField에서 입력이 완료되었을 때 호출되는 메서드    
    private void OnInputFieldSubmit()
    {
        balloon.SetActive(true);
        anim.SetTrigger("think");
    }

    private void StartTalk()
    {
        StartCoroutine(TalkThenIdle());
    }

    // 말풍선을 비활성화 하고, 5초 동안 "talk" 애니메이션 실행 후 "Idle" 애니메이션으로 전환하는 코루틴
    public IEnumerator TalkThenIdle()
    {
        balloon.SetActive(false);

        anim.SetTrigger("talk");

        yield return new WaitForSeconds(5f);

        anim.SetTrigger("idle");
    }
}
