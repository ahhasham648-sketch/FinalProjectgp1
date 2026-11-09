using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonSounds : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.4f;

    private void Awake()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            button.onClick.AddListener(() => Sfx.Play(clickSound, clickVolume));

            if (hoverSound != null)
            {
                var trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entry.callback.AddListener(_ => { if (button.interactable) Sfx.Play(hoverSound, hoverVolume); });
                trigger.triggers.Add(entry);
            }
        }
    }
}
