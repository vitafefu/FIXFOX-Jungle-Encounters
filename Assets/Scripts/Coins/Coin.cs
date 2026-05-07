using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int _value = 1;

    private void OnTriggerEnter2D(Collider2D other)  // для 2D
    {
        if (other.TryGetComponent(out PlayerWallet wallet))
        {
            Collect(wallet);
        }
    }

    private void Collect(PlayerWallet wallet)
    {
        wallet.AddCoins(_value);
        Destroy(gameObject);
    }
}