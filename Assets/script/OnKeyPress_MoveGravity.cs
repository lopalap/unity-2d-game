using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 키를 누르면 좌우 이동하고, 회전하지 않으며, 좌우 반전한다
public class OnKeyPress_MoveGravity : MonoBehaviour
{
    public float speed = 3; // 속도 : Inspector에 지정

    float vx = 0;
    bool leftFlag = false; // 왼쪽 방향인지
    Rigidbody2D rbody;

    void Start() // 처음에 시행한다
    {
        // 충돌 시에 회전시키지 않는다
        rbody = GetComponent<Rigidbody2D>();
        rbody.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update() // 계속 시행한다
    {
        vx = 0;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed)
            {
                vx = speed;
                leftFlag = false;
            }
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed)
            {
                vx = -speed;
                leftFlag = true;
            }
        }
    }

    void FixedUpdate() // 계속 시행한다 (일정 시간마다)
    {
        // 이동하다 (중력을 가한 채) - 유니티 6 최신 API 적용
        rbody.linearVelocity = new Vector2(vx, rbody.linearVelocity.y);

        // 왼쪽 오른쪽으로 방향을 바꾼다
        this.GetComponent<SpriteRenderer>().flipX = leftFlag;
    }
}