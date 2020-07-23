using System;
using System.IO;
using GoogleARCore;
using GoogleARCore.Examples.ComputerVision;
using UnityEngine;

namespace ARCoreDepth.Script
{
    [RequireComponent(typeof(TextureReader))]
    public class PointCloud : MonoBehaviour
    {
        public GameObject cubePrefab;
        public int poolSize;

        private byte[] PixelByteBuffer = new byte[0];
        private int PixelBufferSize;
        
        private GameObject[] PixelObjects;
        private Material[] PixelMaterials;
        private Color[] PixelColors;

        private const int DimensionsInverseScale = 2;
        private const float IntervalTime = 1.0f;
        private float ElapsedTime = 0;
        private int PointsInViewCountRef = 0;

        private void Awake()
        {
            if (cubePrefab.GetComponent<Renderer>() == null)
            {
                Debug.LogError("No renderer on pixel prefab!");
                enabled = false;
                return;
            }

            var textureReader = GetComponent<TextureReader>();
            textureReader.ImageFormat = TextureReaderApi.ImageFormatType.ImageFormatColor;
            textureReader.OnImageAvailableCallback += OnImageAvailable;

            var landscape = Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight;
            var scaledScreenWidth = Screen.width / DimensionsInverseScale;
            var scaledScreenHeight = Screen.height / DimensionsInverseScale;
            
            textureReader.ImageWidth = landscape ? scaledScreenWidth : scaledScreenHeight;
            textureReader.ImageHeight = landscape ? scaledScreenHeight : scaledScreenWidth;

            PixelObjects = new GameObject[poolSize];
            PixelMaterials = new Material[poolSize];
            
            for (var i = 0; i < poolSize; ++i)
            {
                var pixelObj = Instantiate(cubePrefab, transform);
                PixelObjects[i] = pixelObj;
                PixelMaterials[i] = pixelObj.GetComponent<Renderer>().material;
                pixelObj.SetActive(false);
            }
        }
        
        private void FixedUpdate()
        {
            ElapsedTime += Time.deltaTime;
            if (!(ElapsedTime > IntervalTime)) return;
            
            SavePointCloudInfo(PointsInViewCountRef);
            ElapsedTime = 0;
        }
        
        private void OnImageAvailable(TextureReaderApi.ImageFormatType format, int width, int height, IntPtr pixelBuffer, int bufferSize)
        {
            if (format != TextureReaderApi.ImageFormatType.ImageFormatColor) return;

            // Adjust buffer size if necessary.
            if (bufferSize != PixelBufferSize || PixelByteBuffer.Length == 0)
            {
                PixelBufferSize = bufferSize;
                PixelByteBuffer = new byte[bufferSize];
                PixelColors = new Color[width * height];
            }

            // Move raw data into managed buffer.
            System.Runtime.InteropServices.Marshal.Copy(pixelBuffer, PixelByteBuffer, 0, bufferSize);
            
            var bufferIndex = 0;
            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    int r = PixelByteBuffer[bufferIndex++];
                    int g = PixelByteBuffer[bufferIndex++];
                    int b = PixelByteBuffer[bufferIndex++];
                    int a = PixelByteBuffer[bufferIndex++];
                    var color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                    int pixelIndex;
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.LandscapeRight:
                            pixelIndex = y * width + width - 1 - x;
                            break;
                        case ScreenOrientation.Portrait:
                            pixelIndex = (width - 1 - x) * height + height - 1 - y;
                            break;
                        case ScreenOrientation.LandscapeLeft:
                            pixelIndex = (height - 1 - y) * width + x;
                            break;
                        default:
                            pixelIndex = x * height + y;
                            break;
                    }

                    PixelColors[pixelIndex] = color;
                }
            }

            FeaturePointCubes();
        }

        private void FeaturePointCubes()
        {
            foreach (var pixelObj in PixelObjects)
            {
                pixelObj.SetActive(false);
            }

            var index = 0;
            var pointsInViewCount = 0;
            var camera = Camera.main;
            var scaledScreenWidth = Screen.width / DimensionsInverseScale;
            while (index < Frame.PointCloud.PointCount && pointsInViewCount < poolSize)
            {
                // If a feature point is visible, use its screen space position to get the correct color for its cube
                // from our friendly-formatted array of pixel colors.
                var point = Frame.PointCloud.GetPoint(index);
                var screenPoint = camera.WorldToScreenPoint(point);
                if (screenPoint.x >= 0 && screenPoint.x < camera.pixelWidth &&
                    screenPoint.y >= 0 && screenPoint.y < camera.pixelHeight)
                {
                    var pixelObj = PixelObjects[pointsInViewCount];
                    pixelObj.SetActive(true);
                    pixelObj.transform.position = point;
                    var scaledX = (int) screenPoint.x / DimensionsInverseScale;
                    var scaledY = (int) screenPoint.y / DimensionsInverseScale;
                    PixelMaterials[pointsInViewCount].color = PixelColors[scaledY * scaledScreenWidth + scaledX];
                    pointsInViewCount++;
                }

                //added by Limes
                PointsInViewCountRef = pointsInViewCount;
                index++;
            }
        }
        
        private bool SavePointCloudInfo(int index)
        {
            using (var jcEnvironment = new AndroidJavaClass("android.os.Environment"))
            using (var joExDir = jcEnvironment.CallStatic<AndroidJavaObject>("getExternalStorageDirectory"))
            {
                var path = joExDir?.Call<string>("toString") ?? Application.persistentDataPath; 
                var pointCloudDataPath = path + "/pointcloud";
                if (!Directory.Exists(pointCloudDataPath)) Directory.CreateDirectory(pointCloudDataPath);
                var filepath = pointCloudDataPath + "/" + DateTime.Now.ToString("yyyyMMdd") + ".csv";
                Debug.Log($"filepath: {filepath}");
                
                var sw = new StreamWriter(filepath, true);
                for (var i = 0; i < index; i++)
                {
                    var tmp = i + "," +
                              PixelObjects[i].transform.position.x + "," +
                              PixelObjects[i].transform.position.y + "," +
                              PixelObjects[i].transform.position.z + "," +
                              PixelMaterials[i].color.r + "," +
                              PixelMaterials[i].color.g + "," +
                              PixelMaterials[i].color.b + "," +
                              PixelMaterials[i].color.a;
                    sw.WriteLine(tmp);
                }

                sw.Flush();
                sw.Close();
                return true;
            }
        }
    }
}