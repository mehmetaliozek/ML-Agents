using UnityEngine;

[ExecuteAlways]
public class AutoTile : MonoBehaviour
{
    public float scaleFactor = 0.5f;

    // Eksen seçimi: Duvarýn uzunluðu hangi eksende?
    // Çoðu dik duvar için X ve Y doðrudur. Zeminler için X ve Z olabilir.
    public bool useZAxisInsteadOfY = false;

    private Vector3 lastScale;
    private MaterialPropertyBlock _propBlock;
    private int _mainTexSTId;
    private int _baseMapSTId;

    void OnEnable()
    {
        // Property ID'leri performans için önbelleðe alýyoruz
        _mainTexSTId = Shader.PropertyToID("_MainTex_ST"); // Standard Shader
        _baseMapSTId = Shader.PropertyToID("_BaseMap_ST"); // URP Shader

        UpdateTiling(); // Script aktif olur olmaz çalýþtýr
    }

    void Update()
    {
        if (transform.localScale != lastScale)
        {
            UpdateTiling();
            lastScale = transform.localScale;
        }
    }

    void OnValidate()
    {
        UpdateTiling();
    }

    void UpdateTiling()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();

        rend.GetPropertyBlock(_propBlock);

        // Hangi eksenleri kullanacaðýmýzý seçiyoruz
        float sizeX = transform.localScale.x * scaleFactor;
        float sizeY = useZAxisInsteadOfY ? transform.localScale.z * scaleFactor : transform.localScale.y * scaleFactor;

        Vector4 tilingValue = new Vector4(sizeX, sizeY, 0, 0);

        // Hem Standard hem URP için deneme yapýyoruz
        // (Hangisi varsa ona uygular, zararý yoktur)
        _propBlock.SetVector(_mainTexSTId, tilingValue);
        _propBlock.SetVector(_baseMapSTId, tilingValue);

        rend.SetPropertyBlock(_propBlock);
    }
}