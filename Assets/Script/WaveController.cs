using UnityEngine;

public class WaveController : MonoBehaviour
{
    public int rows = 100;
    public int columns = 100;
    public float spacing = 1.0f;
    public float waveSpeed = 3f;
    public float envelopePeriod = 8f;
    public float minAmplitude = 0.5f;
    public float maxAmplitude = 2f;
    public float waveDensity = 0.5f;
    public float verticalAmplitudeMod = 0.7f;
    public Color baseColor = Color.white;
    public Color peakColor = Color.blue;
    public Color troughColor = Color.yellow;

    private Transform[,] spheres;
    private Renderer[,] renderers;
    private Vector3[,] initialPositions;
    private float[,] previousSineValues;
    private float gridCenterOffsetX;
    private float gridCenterOffsetZ;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        
        rows = Mathf.Max(1, rows);
        columns = Mathf.Max(1, columns);
        
        InitializeGrid();
    }

    void InitializeGrid()
    {
        spheres = new Transform[rows, columns];
        renderers = new Renderer[rows, columns];
        initialPositions = new Vector3[rows, columns];
        previousSineValues = new float[rows, columns];
        
        gridCenterOffsetX = (columns - 1) * spacing * 0.5f;
        gridCenterOffsetZ = (rows - 1) * spacing * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = Vector3.one * 0.3f;
                
                Collider collider = sphere.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                
                sphere.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sphere.GetComponent<Renderer>().receiveShadows = false;
                sphere.GetComponent<Renderer>().lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                sphere.GetComponent<Renderer>().reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                Vector3 position = new Vector3(
                    col * spacing - gridCenterOffsetX, 
                    0, 
                    row * spacing - gridCenterOffsetZ
                );
                
                sphere.transform.position = position;
                spheres[row, col] = sphere.transform;
                renderers[row, col] = sphere.GetComponent<Renderer>();
                initialPositions[row, col] = position;
                
                SetSphereColor(row, col, baseColor);
            }
        }
    }

    void Update()
    {
        if (spheres == null) return;
        
        float time = Time.time;
        float temporalEnvelope = CalculateTemporalEnvelope(time);

        for (int row = 0; row < rows; row++)
        {
            float verticalFactor = CalculateVerticalAmplitude(row);
            float rowAmplitude = temporalEnvelope * verticalFactor;
            
            float rowOffset = row * waveDensity;
            
            for (int col = 0; col < columns; col++)
            {
                if (spheres[row, col] == null || renderers[row, col] == null) continue;
                
                float phase = (waveSpeed * time) - rowOffset;
                float sine = Mathf.Sin(phase);
                float displacement = rowAmplitude * sine;
                
                Vector3 newPos = initialPositions[row, col];
                newPos.x += displacement;
                spheres[row, col].position = newPos;
                
                UpdateSphereColor(row, col, sine);
                
                previousSineValues[row, col] = sine;
            }
        }
    }

    float CalculateTemporalEnvelope(float time)
    {
        float t = 0.5f * (Mathf.Cos(2f * Mathf.PI * time / envelopePeriod) + 1);
        return Mathf.Lerp(minAmplitude, maxAmplitude, t);
    }

    float CalculateVerticalAmplitude(int row)
    {
        float normalizedRow = (float)row / (rows - 1);
        
        return verticalAmplitudeMod + 
               (1 - verticalAmplitudeMod) * Mathf.Sin(normalizedRow * Mathf.PI);
    }

    void UpdateSphereColor(int row, int col, float currentSine)
    {
        float prevSine = previousSineValues[row, col];
        
        bool isPeak = currentSine > 0.9f && prevSine <= currentSine;
        bool isTrough = currentSine < -0.9f && prevSine >= currentSine;
        bool wasPeak = prevSine > 0.9f;
        bool wasTrough = prevSine < -0.9f;
        
        Color targetColor = baseColor;
        bool colorChanged = false;
        
        if (isPeak)
        {
            targetColor = peakColor;
            colorChanged = true;
        }

        else if (isTrough)
        {
            targetColor = troughColor;
            colorChanged = true;
        }

        else if ((wasPeak && !isPeak) || (wasTrough && !isTrough))
        {
            targetColor = baseColor;
            colorChanged = true;
        }
        
        if (colorChanged)
        {
            SetSphereColor(row, col, targetColor);
        }
    }

    static MaterialPropertyBlock propertyBlock;
    void SetSphereColor(int row, int col, Color color)
    {
        if (renderers[row, col] == null) return;
        
        if (renderers[row, col].material == null)
        {
            renderers[row, col].material = new Material(Shader.Find("Standard"));
        }
        
        renderers[row, col].material.color = color;
    }
}