using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>Whether the player is typing into a text field: gameplay keys (movement, skills, window hotkeys) are skipped then.</summary>
    public static class UiFocus
    {
        public static bool IsTyping()
        {
            EventSystem events = EventSystem.current;
            if (events == null || events.currentSelectedGameObject == null)
            {
                return false;
            }
            InputField field = events.currentSelectedGameObject.GetComponent<InputField>();
            return field != null && field.isFocused;
        }
    }
}
