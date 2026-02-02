using UnityEngine;
using UnityEngine.EventSystems;

public class SliderPointUpSE : MonoBehaviour, IPointerUpHandler
{
    public System.Action OnPointerUpAction;

    public void OnPointerUp(PointerEventData eventData)
    {
        OnPointerUpAction?.Invoke();
    }
}
