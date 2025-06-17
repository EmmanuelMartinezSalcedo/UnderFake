using UnityEngine;
using System.Collections;

public class BlasterSpawner : MonoBehaviour
{
    public GameObject laserPrefab;      // Prefab del láser
    public Transform firePoint;         // Punto desde donde se disparará el láser
    public float laserSpeed = 10f;      // Velocidad de expansión
    public float maxLaserLength = 10f;  // Longitud máxima del láser
    public float chargeDelay = 1f;      // Tiempo antes de disparar
    public float destroyDelay = 1.5f;   // Tiempo después del disparo para destruir todo

    void Start()
    {
        StartCoroutine(FireAfterDelay());
    }

    IEnumerator FireAfterDelay()
    {
        // Espera antes de disparar
        yield return new WaitForSeconds(chargeDelay);

        // Asegúrate de que firePoint esté asignado
        if (firePoint == null)
        {
            Debug.LogError("BlasterSpawner: Fire Point no asignado.");
            yield break;
        }

        // Instanciar el láser en la posición y rotación del firePoint
        GameObject laser = Instantiate(laserPrefab, firePoint.position, firePoint.rotation);
        Transform laserTransform = laser.transform;

        // Copiar escala del blaster (o puedes usar firePoint.localScale si prefieres)
        Vector3 baseScale = transform.localScale;

        // Iniciar con longitud 0 en Y (para expandir después)
        laserTransform.localScale = new Vector3(baseScale.x, 0f, baseScale.z);

        float currentLength = 0f;

        // Expandir el láser en eje Y
        while (currentLength < maxLaserLength)
        {
            float step = laserSpeed * Time.deltaTime;
            currentLength += step;
            laserTransform.localScale = new Vector3(baseScale.x, currentLength, baseScale.z);
            yield return null;
        }

        // Destruir el láser después de un tiempo
        Destroy(laser, 0.5f);

        // Destruir el blaster también si quieres
        Destroy(gameObject, destroyDelay);
    }
}
