using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class DrawOnScreen : MonoBehaviour
{
    public Camera cam; 
    public RectTransform canvasRect;
    public RawImage drawOutput;

    // Canvas dimensions
    public int textureWidth = 1024;
    public int textureHeight = 512;

    // Brush properties
    public int brushSize = 4;
    public Color brushColor;

    // Drawing settings
    public bool useInterpolation = true;

    private Texture2D generatedTexture;
    private Color[] colorMap;

    private int xPixel = 0, yPixel = 0;
    private bool pressedLastFrame = false;
    private int lastX = 0, lastY = 0;

    void Start()
    {
        
        colorMap = new Color[textureWidth * textureHeight];
        generatedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        generatedTexture.filterMode = FilterMode.Point;
        drawOutput.texture = generatedTexture; 
        ResetColor();
    }

    void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            CalculatePixel();
        }   

        else
        {
            pressedLastFrame = false;
        }
            
    }

    void CalculatePixel()
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, cam, out localPoint);

        // Convert local point to pixel coordinates
        Vector2 rectSize = canvasRect.rect.size;
        localPoint += rectSize / 2; // Offset local point to positive space
        xPixel = (int)((localPoint.x / rectSize.x) * textureWidth);
        yPixel = (int)((localPoint.y / rectSize.y) * textureHeight);

        if (xPixel >= 0 && xPixel < textureWidth && yPixel >= 0 && yPixel < textureHeight)
        {
            ChangePixelsAroundPoint();
        }
        else
        {
            pressedLastFrame = false;
        }
    }

    void ChangePixelsAroundPoint()
    {
       const int baseInterpolationDistance = 100;
       int maxInterpolationDistance = (int)(baseInterpolationDistance * Time.deltaTime * 60); // adjust with frame rate so u can still "draw" on lower fps

        if (useInterpolation && pressedLastFrame)
        {
            int dist = (int)Mathf.Sqrt((xPixel - lastX) * (xPixel - lastX) + (yPixel - lastY) * (yPixel - lastY));

            if (dist <= maxInterpolationDistance)
            {
                for (int i = 1; i <= dist; i++)
                {
                    int interpolatedX = (i * xPixel + (dist - i) * lastX) / dist;
                    int interpolatedY = (i * yPixel + (dist - i) * lastY) / dist;
                    DrawBrush(interpolatedX, interpolatedY);
                }
            }
            else
            {
                // Skip interpolation for large jumps
                DrawBrush(xPixel, yPixel);
            }
        }
        else
        {
            DrawBrush(xPixel, yPixel);
        }

        pressedLastFrame = true;
        lastX = xPixel;
        lastY = yPixel;

        SetTexture();
    }

    void DrawBrush(int xPix, int yPix)
    {
        int i = Mathf.Max(0, xPix - brushSize + 1);
        int j = Mathf.Max(0, yPix - brushSize + 1);
        int maxi = Mathf.Min(textureWidth - 1, xPix + brushSize - 1);
        int maxj = Mathf.Min(textureHeight - 1, yPix + brushSize - 1);

        for (int x = i; x <= maxi; x++)
        {
            for (int y = j; y <= maxj; y++)
            {
                if ((x - xPix) * (x - xPix) + (y - yPix) * (y - yPix) <= brushSize * brushSize)
                    colorMap[x * textureHeight + y] = brushColor;
            }
        }
    }

    void SetTexture()
    {
        generatedTexture.SetPixels(colorMap);
        generatedTexture.Apply();
    }

    public void ResetColor()
    {
        for (int i = 0; i < colorMap.Length; i++)
            colorMap[i] = Color.white;
        SetTexture();
    }

   

    public void SaveTexture()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "SavedAutographs");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = $"Drawing_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string filePath = Path.Combine(folderPath, fileName);

        // Rotate 90 degrees clockwise, then flip vertically
        Texture2D transformedTexture = FlipTextureHorizontally(
            RotateTexture90Clockwise(generatedTexture)
        );

        // Resize the corrected texture to 500x300
        Texture2D resizedTexture = ResizeTexture(transformedTexture, 400, 240);

        byte[] bytes = resizedTexture.EncodeToPNG();

        File.WriteAllBytes(filePath, bytes);

        Debug.Log($"Drawing saved to: {filePath}");
    }
   
    private Texture2D FlipTextureHorizontally(Texture2D original)
    {
        Texture2D flippedTexture = new Texture2D(original.width, original.height);
        for (int x = 0; x < original.width; x++)
        {
            for (int y = 0; y < original.height; y++)
            {
                flippedTexture.SetPixel(x, y, original.GetPixel(original.width - 1 - x, y));
            }
        }
        flippedTexture.Apply();
        return flippedTexture;
    }


    private Texture2D RotateTexture90Clockwise(Texture2D original)
    {
        Texture2D rotatedTexture = new Texture2D(original.height, original.width);
        for (int x = 0; x < original.width; x++)
        {
            for (int y = 0; y < original.height; y++)
            {
                rotatedTexture.SetPixel(original.height - 1 - y, x, original.GetPixel(x, y));
            }
        }
        rotatedTexture.Apply();
        return rotatedTexture;
    }

    private Texture2D ResizeTexture(Texture2D original, int newWidth, int newHeight)
    {
        // Create a temporary RenderTexture for resizing
        RenderTexture rt = new RenderTexture(newWidth, newHeight, 24);
        RenderTexture.active = rt;

        // Copy the original texture into the RenderTexture
        Graphics.Blit(original, rt);

        // Create a new Texture2D with the desired dimensions
        Texture2D resizedTexture = new Texture2D(newWidth, newHeight);
        resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resizedTexture.Apply();

        // Cleanup
        RenderTexture.active = null;
        rt.Release();

        return resizedTexture;
    }


}

