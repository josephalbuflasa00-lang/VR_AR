using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;

public class VRVideoColorPicker : MonoBehaviour
{
    [Header("Video Setup")]
    public VideoPlayer videoPlayer;
    public RenderTexture videoTexture;
    public GameObject screenObject;
    
    [Header("Color Picking")]
    public XRRayInteractor leftHandRay;
    public XRRayInteractor rightHandRay;
    public Material chromaKeyMaterial;
    public Image selectedColorDisplay;
    
    [Header("UI")]
    public Text videoNameText;
    public Slider thresholdSlider;
    public Slider softnessSlider;
    
    private List<string> videoPaths = new List<string>();
    private int currentVideoIndex = 0;
    
    void Start()
    {
        // 1. Find all videos in Quest Movies folder
        FindAllVideos();
        
        // 2. Setup video player
        SetupVideoPlayer();
        
        // 3. Setup color picking
        SetupColorPicking();
    }
    
    void FindAllVideos()
    {
        string videosFolder = "/storage/emulated/0/Movies/";
        
        if (Directory.Exists(videosFolder))
        {
            // Find all video files
            string[] allFiles = Directory.GetFiles(videosFolder);
            
            foreach (string file in allFiles)
            {
                string extension = Path.GetExtension(file).ToLower();
                if (extension == ".mp4" || extension == ".mov" || extension == ".avi")
                {
                    videoPaths.Add(file);
                }
            }
            
            Debug.Log($"Found {videoPaths.Count} videos");
        }
    }
    
    void SetupVideoPlayer()
    {
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoTexture;
        
        // Apply to screen
        screenObject.GetComponent<Renderer>().material = chromaKeyMaterial;
        screenObject.GetComponent<Renderer>().material.mainTexture = videoTexture;
        
        // Load first video if exists
        if (videoPaths.Count > 0)
            LoadVideo(0);
    }
    
    void SetupColorPicking()
    {
        // Make rays visible for picking
        leftHandRay.enabled = true;
        rightHandRay.enabled = true;
    }
    
    void LoadVideo(int index)
    {
        if (videoPaths.Count == 0) return;
        
        currentVideoIndex = index;
        string videoPath = videoPaths[currentVideoIndex];
        
        videoPlayer.url = "file://" + videoPath;
        videoPlayer.Prepare();
        
        videoNameText.text = Path.GetFileName(videoPath);
        
        StartCoroutine(PlayWhenReady());
    }
    
    IEnumerator PlayWhenReady()
    {
        while (!videoPlayer.isPrepared)
            yield return null;
            
        videoPlayer.Play();
    }
    
    // === PUBLIC METHODS FOR UI BUTTONS ===
    
    public void PlayPause()
    {
        if (videoPlayer.isPlaying)
            videoPlayer.Pause();
        else
            videoPlayer.Play();
    }
    
    public void NextVideo()
    {
        if (videoPaths.Count == 0) return;
        currentVideoIndex = (currentVideoIndex + 1) % videoPaths.Count;
        LoadVideo(currentVideoIndex);
    }
    
    public void PreviousVideo()
    {
        if (videoPaths.Count == 0) return;
        currentVideoIndex = (currentVideoIndex - 1 + videoPaths.Count) % videoPaths.Count;
        LoadVideo(currentVideoIndex);
    }
    
    public void PickColorFromVideo()
    {
        // Get which controller is pointing at screen
        XRRayInteractor activeRay = GetRayPointingAtScreen();
        
        if (activeRay != null)
        {
            RaycastHit hit;
            if (activeRay.TryGetCurrent3DRaycastHit(out hit))
            {
                if (hit.collider.gameObject == screenObject)
                {
                    // Get the exact pixel color from video texture
                    Vector2 uv = hit.textureCoord;
                    Color pickedColor = GetColorFromTexture(videoTexture, uv);
                    
                    // Apply to shader
                    chromaKeyMaterial.SetColor("_KeyColor", pickedColor);
                    selectedColorDisplay.color = pickedColor;
                    
                    Debug.Log($"Picked color: {pickedColor}");
                }
            }
        }
    }
    
    Color GetColorFromTexture(RenderTexture rt, Vector2 uv)
    {
        // Create a temporary texture to read from
        RenderTexture.active = rt;
        Texture2D tempTex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tempTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tempTex.Apply();
        RenderTexture.active = null;
        
        // Get color at UV coordinates
        Color color = tempTex.GetPixelBilinear(uv.x, uv.y);
        Destroy(tempTex);
        
        return color;
    }
    
    XRRayInteractor GetRayPointingAtScreen()
    {
        // Check which controller is pointing at the screen
        RaycastHit hit;
        
        if (leftHandRay.TryGetCurrent3DRaycastHit(out hit))
            if (hit.collider.gameObject == screenObject)
                return leftHandRay;
                
        if (rightHandRay.TryGetCurrent3DRaycastHit(out hit))
            if (hit.collider.gameObject == screenObject)
                return rightHandRay;
                
        return null;
    }
    
    // Update shader values from UI sliders
    public void UpdateThreshold(float value)
    {
        chromaKeyMaterial.SetFloat("_Threshold", value);
    }
    
    public void UpdateSoftness(float value)
    {
        chromaKeyMaterial.SetFloat("_Softness", value);
    }
    
    void Update()
    {
        // Quick testing in editor
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
            PlayPause();
        if (Input.GetKeyDown(KeyCode.RightArrow))
            NextVideo();
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            PreviousVideo();
        if (Input.GetKeyDown(KeyCode.C))
            PickColorFromVideo();
        #endif
    }
}
