using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;

public class DiskPassiveHUD : MonoBehaviour
{
    public DiskPassiveBank bank;
    public TextMeshProUGUI summaryText;

    void Awake()
    {
        if (!bank) bank = FindAnyObjectByType<DiskPassiveBank>();
    }

    void OnEnable()
    {
        if (bank) bank.OnChanged.AddListener(Refresh);
        Refresh();
    }
    void OnDisable()
    {
        if (bank) bank.OnChanged.RemoveListener(Refresh);
    }

    void Refresh()
    {
        if (!summaryText || bank == null) return;

        var groups = bank.GetAcquired().GroupBy(d => d.id);
        var sb = new StringBuilder();
        foreach (var g in groups)
        {
            var any = g.First();
            int stacks = bank.GetStacks(any.id);
            sb.AppendLine($"{any.title} x{stacks}");
        }
        summaryText.text = sb.ToString();
    }
}
