using UnityEngine;
using System;

public class PlayerKillCounter : MonoBehaviour
{
    public event Action<int> KillsChanged;

    [SerializeField] private int _killedEnemies = 0;

    public int KilledEnemies => _killedEnemies;

    public void AddKill()
    {
        _killedEnemies++;
        KillsChanged?.Invoke(_killedEnemies);
    }
}