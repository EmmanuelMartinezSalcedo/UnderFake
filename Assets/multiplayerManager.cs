using UnityEngine;
using System.Collections;

public class playerManager : MonoBehaviour
{
    [Header("Prefabs para instanciar")]
    public GameObject player1Prefab; // triángulo
    public GameObject player2Prefab; // cubo

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
            prefabAInstanciar = player1Prefab; // primera instancia: triángulo
        }
        else
        {
            prefabAInstanciar = player2Prefab; // segunda instancia en adelante: cubo
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
