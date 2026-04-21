using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project.Scripts.Client.Systems
{
    public class LatencyUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ClientRuntimeBridge runtimeBridge;
        [SerializeField] private Slider latencySlider;
        [SerializeField] private TextMeshProUGUI latencyValueLabel;

        [Header("Ping Thresholds (ms)")]
        [SerializeField, Min(0f)] private float goodPingMax = 120f;
        [SerializeField, Min(0f)] private float warningPingMax = 250f;

        [Header("Label Colors")]
        [SerializeField] private Color goodPingColor = Color.green;
        [SerializeField] private Color warningPingColor = Color.yellow;
        [SerializeField] private Color badPingColor = Color.red;

        private void Awake()
        {
            if (latencySlider != null)
            {
                latencySlider.onValueChanged.AddListener(HandleLatencyChanged);
                HandleLatencyChanged(latencySlider.value);
            }
        }

        private void OnDestroy()
        {
            if (latencySlider != null)
            {
                latencySlider.onValueChanged.RemoveListener(HandleLatencyChanged);
            }
        }

        private void HandleLatencyChanged(float latencyMs)
        {
            runtimeBridge?.SetUserLatencyMilliseconds(latencyMs);
            if (latencyValueLabel != null)
            {
                latencyValueLabel.text = $"{latencyMs:0} ms";
                latencyValueLabel.color = GetPingColor(latencyMs);
            }
        }

        private Color GetPingColor(float latencyMs)
        {
            float goodThreshold = Mathf.Min(goodPingMax, warningPingMax);
            float warningThreshold = Mathf.Max(goodPingMax, warningPingMax);

            if (latencyMs <= goodThreshold)
            {
                return goodPingColor;
            }

            if (latencyMs <= warningThreshold)
            {
                return warningPingColor;
            }

            return badPingColor;
        }
    }
}
