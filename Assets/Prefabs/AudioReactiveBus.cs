using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ÚNICO punto de entrada de audio y análisis.
/// - Reproduce un WAV.
/// - Calcula FFT con suavizado y ganancia.
/// - Publica una textura 1D (_FFTtex) reutilizable por varios shaders.
/// - Calcula promedios por bandas (low/mid/high) y expone evento OnBusUpdated.
/// Crea UN solo objeto con este componente en la escena.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioReactiveBus : MonoBehaviour
{
    public static AudioReactiveBus Instance { get; private set; }

    [Header("Audio Load")]
    [Tooltip("Nombre del WAV en Assets/StreamingAssets/")]
    public string wavFileName = "MyAudio.wav";
    public bool playOnStart = true;
    public bool loop = true;
    public bool muteOutput = false;

    [Header("FFT")]
    [Tooltip("Tamaño FFT (potencia de 2). 64-1024 recomendado")]
    public int fftSize = 512;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    [Range(0f, 1f)] public float smoothing = 0.75f; // EMA
    public float gain = 12f;                         // exposición

    [Header("Shared Texture")]
    [Tooltip("Textura 1D que contiene los bins de la FFT (R)")]
    public Texture2D fftTexture { get; private set; }
    [Tooltip("Filtro de la textura al samplear en el shader")]
    public FilterMode textureFilterMode = FilterMode.Bilinear;

    [Header("Bands (averages)")]
    [Tooltip("Frecuencias en Hz (inclusive) para cada banda")]
    public Vector2 lowBand = new Vector2(20, 250);
    public Vector2 midBand = new Vector2(250, 2000);
    public Vector2 highBand = new Vector2(2000, 8000);

    [Tooltip("Valores [0..1] suavizados de cada banda")]
    public float low, mid, high;

    // Evento: se dispara tras actualizar datos (una vez por frame)
    public event Action<AudioReactiveBus> OnBusUpdated;

    // Expuestos para lectura por consumidores
    public float[] FFT => _ema;        // FFT suavizada [0..1]
    public int FFTSize => fftSize;
    public int SampleRate => _sampleRate;

    private AudioSource _src;
    private float[] _spec;             // FFT bruta
    private float[] _ema;              // FFT suavizada
    private Color[] _cols;             // para escribir textura
    private int _sampleRate;

    // Índices precalculados de bandas
    private int _lowMin, _lowMax, _midMin, _midMax, _highMin, _highMax;

    private float AverageRange(int imin, int imax)
    {
        if (imax < imin)
            return 0.0f;

        float sum = 0f;
        int cnt = 0;
        int max = Mathf.Min(imax, _ema.Length - 1);

        for (int i = Mathf.Max(imin, 0); i <= max; i++)
        {
            sum += _ema[i];
            cnt++;
        }

        if (cnt > 0)
            return sum / cnt;
        else
            return 0.0f;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = loop;
        _src.mute = muteOutput;

        if (muteOutput)
            _src.volume = 0f;

        fftSize = Mathf.ClosestPowerOfTwo(Mathf.Max(32, fftSize));
        _spec = new float[fftSize];
        _ema = new float[fftSize];
        _cols = new Color[fftSize];
        _sampleRate = AudioSettings.outputSampleRate;

        CreateOrResetTexture();
        RecomputeBandIndices();
    }

    private void CreateOrResetTexture()
    {
        if (fftTexture != null && fftTexture.width != fftSize)
        {
            Destroy(fftTexture);
            fftTexture = null;
        }

        if (fftTexture == null)
        {
            fftTexture = new Texture2D(fftSize, 1, TextureFormat.RHalf, false, true);
            fftTexture.name = "FFTtex (bus)";
            fftTexture.wrapMode = TextureWrapMode.Clamp;
            fftTexture.filterMode = textureFilterMode;
            for (int i = 0; i < fftSize; i++) _cols[i] = new Color(0, 0, 0, 1);
            fftTexture.SetPixels(_cols);
            fftTexture.Apply(false, false);
        }
    }

    // 🚀 **VERSIÓN CORREGIDA: ahora funciona en ANDROID**
    private IEnumerator LoadAndPlayWavFromStreamingAssets(string fileName, bool autoPlay)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("[AudioReactiveBus] wavFileName vacío.");
            yield break;
        }

        string fullPath;

#if UNITY_ANDROID && !UNITY_EDITOR
        // En Android no se usa "file://"
        fullPath = Application.streamingAssetsPath + "/" + fileName;
#else
        fullPath = "file://" + System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
#endif

        using (var uwr = UnityWebRequestMultimedia.GetAudioClip(fullPath, AudioType.WAV))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AudioReactiveBus] Error WAV: {uwr.error}\nRuta: {fullPath}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(uwr);
            if (clip == null)
            {
                Debug.LogError("[AudioReactiveBus] Clip nulo.");
                yield break;
            }

            _src.clip = clip;
            _src.loop = loop;
            _src.mute = muteOutput;
            if (muteOutput) _src.volume = 0f;

            // Recalcula bandas
            _sampleRate = AudioSettings.outputSampleRate;
            RecomputeBandIndices();

            if (autoPlay)
                _src.Play();

            Debug.Log($"[AudioReactiveBus] WAV cargado: {clip.frequency} Hz, {clip.channels} ch, samples {clip.samples}.");
        }
    }

    private void OnDestroy()
    {
        if (fftTexture != null)
            Destroy(fftTexture);

        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        if (fftSize < 32)
            fftSize = 32;

        fftSize = Mathf.ClosestPowerOfTwo(fftSize);

        if (_spec == null || _spec.Length != fftSize)
        {
            _spec = new float[fftSize];
            _ema = new float[fftSize];
            _cols = new Color[fftSize];

            CreateOrResetTexture();
        }

        if (Application.isPlaying)
            RecomputeBandIndices();
    }

    private void RecomputeBandIndices()
    {
        float nyquist = _sampleRate * 0.5f;
        float hzPerBin = nyquist / fftSize;

        _lowMin = Mathf.FloorToInt(lowBand.x / hzPerBin);
        _lowMax = Mathf.CeilToInt(lowBand.y / hzPerBin);
        _midMin = Mathf.FloorToInt(midBand.x / hzPerBin);
        _midMax = Mathf.CeilToInt(midBand.y / hzPerBin);
        _highMin = Mathf.FloorToInt(highBand.x / hzPerBin);
        _highMax = Mathf.CeilToInt(highBand.y / hzPerBin);

        _lowMin = Mathf.Clamp(_lowMin, 0, fftSize - 1);
        _lowMax = Mathf.Clamp(_lowMax, 0, fftSize - 1);
        _midMin = Mathf.Clamp(_midMin, 0, fftSize - 1);
        _midMax = Mathf.Clamp(_midMax, 0, fftSize - 1);
        _highMin = Mathf.Clamp(_highMin, 0, fftSize - 1);
        _highMax = Mathf.Clamp(_highMax, 0, fftSize - 1);
    }

    private void Start()
    {
        StartCoroutine(LoadAndPlayWavFromStreamingAssets(wavFileName, playOnStart));
    }

    private void Update()
    {
        if (_src.clip == null)
            return;

        // 1) FFT
        _src.GetSpectrumData(_spec, 0, fftWindow);

        float alpha = 1f - Mathf.Clamp01(smoothing);
        for (int i = 0; i < fftSize; i++)
        {
            float v = Mathf.Clamp01(_spec[i] * gain);
            _ema[i] = Mathf.Lerp(_ema[i], v, alpha);
            _cols[i].r = _ema[i];
            _cols[i].g = _ema[i];
            _cols[i].b = _ema[i];
            _cols[i].a = 1f;
        }

        // 2) Texture
        if (fftTexture != null)
        {
            fftTexture.SetPixels(_cols);
            fftTexture.Apply(false, false);
        }

        // 3) Band averages
        low = AverageRange(_lowMin, _lowMax);
        mid = AverageRange(_midMin, _midMax);
        high = AverageRange(_highMin, _highMax);

        // 4) Evento para consumidores
        OnBusUpdated?.Invoke(this);
    }
}
