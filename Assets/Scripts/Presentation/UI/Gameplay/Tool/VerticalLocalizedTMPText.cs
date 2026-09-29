/*
 * Role hierarchy: Presentation > UI > Gameplay > Tool
 * Primary responsibility: Implements the focused VerticalLocalizedTMPText responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class VerticalLocalizedTMPText : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI _text;

    [Header("Options")]
    [Tooltip("각 글자 사이에 넣을 빈 줄 수입니다. 0이면 바로 다음 줄에 붙습니다.")]
    [SerializeField, Min(0)] private int _extraLineSpacing = 0;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();
    }

    // Purpose: Updates the localized text state.
    public void SetLocalizedText(string localizedText)
    {
        if (_text == null)
            return;

        _text.text = ToVerticalText(localizedText);
    }

    // Purpose: Performs the to vertical text workflow for this component.
    private string ToVerticalText(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        TextElementEnumerator enumerator =
            StringInfo.GetTextElementEnumerator(source);

        bool first = true;

        while (enumerator.MoveNext())
        {
            string textElement = enumerator.GetTextElement();

            if (string.IsNullOrWhiteSpace(textElement))
                continue;

            if (!first)
            {
                builder.Append('\n');

                for (int i = 0; i < _extraLineSpacing; i++)
                    builder.Append('\n');
            }

            builder.Append(textElement);
            first = false;
        }

        return builder.ToString();
    }
}
