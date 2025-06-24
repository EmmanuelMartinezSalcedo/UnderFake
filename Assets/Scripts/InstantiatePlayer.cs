using System.Collections;
using UnityEngine;

public class InstantiatePlayer : MonoBehaviour
{
    [SerializeField] public GameObject heartPrefab;
    [SerializeField] public GameObject handsPrefab;

    private static int instanciasCreadas = 0;

    private void Start()
    {
        Invoke(nameof(InstanciarJugador), 0.5f);
    }

    void InstanciarJugador()
    {
        GameObject prefabAInstanciar;

        if (instanciasCreadas == 0)
        {
            prefabAInstanciar = heartPrefab;
        }
        else
        {
            prefabAInstanciar = handsPrefab;
        }

        GameObject instancia = Instantiate(prefabAInstanciar, transform.position, Quaternion.identity);
        instanciasCreadas++;

        StartCoroutine(SetAsChildNextFrame(instancia.transform));
    }

    private IEnumerator SetAsChildNextFrame(Transform hijo)
    {
        yield return null;
        hijo.SetParent(transform);
    }
}
