using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalPos;
    private bool isShaking = false;
    private Transform camTransform;

    private void Awake()
    {
        Instance = this;
        camTransform = GetComponent<Transform>();
    }

    public void Shake(float duration, float magnitude)
    {
        if (isShaking)
        {
            StopAllCoroutines();
            camTransform.localPosition = originalPos; 
        }

        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        isShaking = true;
        originalPos = camTransform.localPosition;

        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            camTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        camTransform.localPosition = originalPos;
        isShaking = false;
    }
}