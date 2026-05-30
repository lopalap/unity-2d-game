using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    public Transform target;           // 플레이어 Transform

    [Header("이동 설정")]
    public float smoothSpeed = 5f;     // 부드러움 (높을수록 딱 붙음)
    public Vector3 offset = new Vector3(0f, 0f, -10f); // 카메라 오프셋

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
