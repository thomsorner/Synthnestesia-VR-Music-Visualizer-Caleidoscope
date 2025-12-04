using UnityEngine;

public class AudioVisualizerBinder : MonoBehaviour
{
    [Header("Referencias")]
    public AudioReactiveBus bus;       // Tu analizador de audio
    public Material fractalMaterial;   // El material del fractal

    void Update()
    {
        if (bus == null || fractalMaterial == null) return;

        // Enviamos la textura FFT actualizada al shader
        fractalMaterial.SetTexture("_FFTtex", bus.fftTexture);
        fractalMaterial.SetFloat("_UseFFT", 1.0f);
    }
}
