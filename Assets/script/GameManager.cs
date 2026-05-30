using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 필요

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 어디서든 접근 가능한 인스턴스

    public bool isGameOver = false;

    void Awake()
    {
        // 싱글톤 패턴: 어디서든 GameManager.instance로 접근 가능
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // 게임 시작 (버튼 클릭 시 연결)
    public void GameStart()
    {
        SceneManager.LoadScene("MainScene"); // 게임 씬 이름으로 변경
    }

    // 게임 클리어
    public void GameClear()
    {
        Debug.Log("게임 클리어!");
        SceneManager.LoadScene("ClearScene"); // 클리어 씬으로 이동
    }

    // 게임 오버
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("게임 오버!");
        SceneManager.LoadScene("GameOverScene"); // 게임 오버 씬으로 이동
    }
}