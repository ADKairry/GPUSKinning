using UnityEngine;

public class Launcher : MonoBehaviour
{
    private GUIStyle _fpsGUIStyle;
    private const float FPS_UPDATE_INTERVAL = 0.33f;
    private int _framesSince;
    private float _timeSince;
    private float _fps;

    private void Awake()
    {
        _fpsGUIStyle = new GUIStyle();
        _fpsGUIStyle.normal.textColor = Color.red;

        Application.targetFrameRate = 0;
        QualitySettings.vSyncCount = 0;
    }

    private void Start()
    {
        GameObject src = (GameObject)Resources.Load("Archer/Role_GPUSkinned");
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 50; j++)
            {
                GameObject obj = Instantiate(src, new Vector3(i, 0, j), Quaternion.identity);
                GPUSkinnedObject gpuSkinnedObject = obj.GetComponentInChildren<GPUSkinnedObject>();
                gpuSkinnedObject.PlayAnimation(gpuSkinnedObject.AnimationInfomations[Random.Range(0, gpuSkinnedObject.AnimationInfomations.Length)].Name,
                    WrapMode.Loop, 1, Random.Range(0, 0.5f));
            }
        }
    }

    private void Update()
    {
        _framesSince++;
        _timeSince += Time.unscaledDeltaTime;
        if (_timeSince >= FPS_UPDATE_INTERVAL)
        {
            _fps = _framesSince / _timeSince;
            _framesSince = 0;
            _timeSince = 0;
        }
    }

    private void OnGUI()
    {
        _fpsGUIStyle.fontSize = Screen.width / 60;
        GUILayout.Label("FPS: " + _fps.ToString("F1"), _fpsGUIStyle);
    }
}
