using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CustomButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    private Color enteredColor;
    [SerializeField]
    private Color clickedColor;

    private Color _originalColor;
    private TextMeshProUGUI _text;

    public UnityEvent onClick;

    private void Start()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _originalColor = _text.color;
    }
    // Start is called before the first frame update
    public void OnPointerEnter(PointerEventData eventData)
    {
        _text.color = enteredColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _text.color = _originalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _text.color = clickedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _text.color = _originalColor;
        onClick.Invoke();
    }
}

