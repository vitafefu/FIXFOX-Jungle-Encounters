using UnityEngine;
using System;

public class PlayerWallet : MonoBehaviour
{
    public event Action<int> CoinsChanged;

    [SerializeField] private int _coins = 0;

    public int Coins => _coins;

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        _coins += amount;
        CoinsChanged?.Invoke(_coins);
    }

    public bool TrySpendCoins(int amount)
    {
        if (_coins < amount) return false;

        _coins -= amount;
        CoinsChanged?.Invoke(_coins);
        return true;
    }
}