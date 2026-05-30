using UnityEngine;
using System.Collections.Generic;

public class RoomDoor : MonoBehaviour
{
    public List<GameObject> monsters; // 방에 있는 몬스터들을 여기에 넣으세요

    void Update()
    {
        // 몬스터 리스트에서 null인(죽은) 녀석들을 제거
        monsters.RemoveAll(m => m == null);

        // 몬스터가 하나도 없으면 문 비활성화
        if (monsters.Count == 0)
        {
            if (gameObject.activeSelf) // 문이 활성화 상태일 때만
            {
                Debug.Log("방의 모든 몬스터를 처치했습니다! 문이 열립니다.");
                gameObject.SetActive(false); // 문 삭제(열림)
            }
        }
    }
}