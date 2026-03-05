using UnityEngine;

public class DustManager : MonoBehaviour, IPickup
{
    [Header("Models")]
    [SerializeField] private GameObject groundDustModel; // Dust model on the ground
    [SerializeField] private GameObject airDustModel;   // Dust model when picked up

    public void Drop()
    {
        EnableGround();
    }

    public void Pickup()
    {
        EnableAir();
    }

    private void EnableGround()
    {
        groundDustModel.SetActive(true);
        airDustModel.SetActive(false);
    }
    private void EnableAir()
    {
        groundDustModel.SetActive(false);
        airDustModel.SetActive(true);
    }
}
