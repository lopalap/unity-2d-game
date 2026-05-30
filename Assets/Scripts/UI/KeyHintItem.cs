using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 키 힌트 하나 — 눌리면 pressed 스프라이트로 교체
/// </summary>
public class KeyHintItem : MonoBehaviour
{
    public Sprite   normalSprite;
    public Sprite   pressedSprite;
    public Key      key;           // UnityEngine.InputSystem.Key

    Image _img;

    void Awake()
    {
        _img = GetComponent<Image>();
        if (_img != null && normalSprite != null)
            _img.sprite = normalSprite;
    }

    void Update()
    {
        if (_img == null) return;
        bool isPressed = Keyboard.current != null && Keyboard.current[key].isPressed;
        _img.sprite = isPressed ? pressedSprite : normalSprite;
    }
}
