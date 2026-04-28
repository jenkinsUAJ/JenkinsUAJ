using UnityEngine;

public class ExplosionDestructible : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ActivateDestruction() {
        Destroy(gameObject);
    }
}
