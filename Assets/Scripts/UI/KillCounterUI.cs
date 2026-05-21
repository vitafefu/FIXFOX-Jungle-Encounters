using UnityEngine;
using TMPro;

public class KillCounterUI : MonoBehaviour
{
    [SerializeField] private string _prefix = "Убито врагов: ";

    private TextMeshProUGUI _killText;
    private PlayerKillCounter _playerKillCounter;

    private void Awake()
    {
        _killText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (_killText == null)
        {
            Debug.LogError("На этом объекте нет TextMeshProUGUI!");
            return;
        }

        _playerKillCounter = FindFirstObjectByType<PlayerKillCounter>();

        if (_playerKillCounter != null)
        {
            _playerKillCounter.KillsChanged += UpdateKillDisplay;
            UpdateKillDisplay(_playerKillCounter.KilledEnemies);
        }
        else
        {
            _killText.text = $"{_prefix}0";
        }
    }

    private void OnDisable()
    {
        if (_playerKillCounter != null)
            _playerKillCounter.KillsChanged -= UpdateKillDisplay;
    }

    private void UpdateKillDisplay(int kills)
    {
        _killText.text = $"{_prefix}{kills}";
    }
}