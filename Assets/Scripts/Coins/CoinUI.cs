using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private string _prefix = "Монеты: ";

    private TextMeshProUGUI _coinText;
    private PlayerWallet _playerWallet;

    private void Awake()
    {
        _coinText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (_coinText == null)
        {
            Debug.LogError("На этом объекте нет TextMeshProUGUI!");
            return;
        }

        _playerWallet = FindFirstObjectByType<PlayerWallet>();

        if (_playerWallet != null)
        {
            _playerWallet.CoinsChanged += UpdateCoinDisplay;
            UpdateCoinDisplay(_playerWallet.Coins);
        }
        else
        {
            _coinText.text = $"{_prefix}0";
        }
    }

    private void OnDisable()
    {
        if (_playerWallet != null)
            _playerWallet.CoinsChanged -= UpdateCoinDisplay;
    }

    private void UpdateCoinDisplay(int coins)
    {
        _coinText.text = $"{_prefix}{coins}";
    }
}