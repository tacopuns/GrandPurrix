using UnityEngine;
 
public class Draw : MonoBehaviour
{
 
    public Camera cam;//Reference to the camera in the scene
 
    //Canvas dimensions
    public int totalXPixels = 1024;
    public int totalYPixels = 512;
 
    //Brush properties
    public int brushSize = 4;
    public Color brushColor;
 
    //Wether the drawing system will use interpolation to make smoother lines(will affect the performance)
    public bool useInterpolation = true;
 
    //References to the points on our drawable face
    public Transform topLeftCorner;
    public Transform bottomRightCorner;
    public Transform point;
 
    //Reference to the material which will use this texture
    public Material material;
 
    //The generated texture
    public Texture2D generatedTexture;
 
    //The array which contains the color of the pixels
    Color[] colorMap;
 
    //The current coordinates of the cursor in the current frame
    int xPixel = 0;
    int yPixel = 0;
 
    //Variables necessary for interpolation
    bool pressedLastFrame = false;//This bool remembers wether we click over the drawable area in the last frame
    int lastX = 0;//These variables remember the coordinates of the cursor in the last frame
    int lastY = 0;
 
    //These variables hold constants which are precalculated in order to save performance
    float xMult;
    float yMult;
 
    private void Start()
    {
        //Initializing the colorMap array with width * height elements
        colorMap = new Color[totalXPixels * totalYPixels];
        generatedTexture = new Texture2D(totalYPixels, totalXPixels, TextureFormat.RGBA32, false); //Generating a new texture with width and height
        generatedTexture.filterMode = FilterMode.Point;
        material.SetTexture("_MainTex", generatedTexture);  //Giving our material the new texture
 
        ResetColor(); //Resetting the color of the canvas to white
 
        xMult = totalXPixels / (bottomRightCorner.localPosition.x - topLeftCorner.localPosition.x);//Precalculating constants
        yMult = totalYPixels / (bottomRightCorner.localPosition.y - topLeftCorner.localPosition.y);
    }
 
    private void Update()
    {
        if (Input.GetMouseButton(0))//If the mouse is pressed, call the function
            CalculatePixel();
        else //Else, we did not draw, so on the next frame we should not apply interpolation
            pressedLastFrame = false;
    }
 
    void CalculatePixel()//This function checks if the cursor is currently over the canvas and, if it is, it calculates which pixel on the canvas it is on
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);//Get a ray from the center of the camera to the mouse position
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10f))//Check if the ray hits something
        {
            point.position = hit.point;//Move to pointer to the place where the mouse intersects the canvas
            xPixel = (int)((point.localPosition.x - topLeftCorner.localPosition.x) * xMult); //Calculate the position in pixels
            yPixel = (int)((point.localPosition.y - topLeftCorner.localPosition.y) * yMult);
            ChangePixelsAroundPoint(); //Call the next function
        }
        else
            pressedLastFrame = false; //We did not draw, so the next frame we should not apply interpolation

        
    }
 
    void ChangePixelsAroundPoint() //This function checks wether interpolation should be applied and if it should, it applies it
    {
        if(useInterpolation && pressedLastFrame && (lastX != xPixel || lastY != yPixel)) //Check if we should use interpolation
        {
            int dist = (int)Mathf.Sqrt((xPixel - lastX) * (xPixel - lastX) + (yPixel - lastY) * (yPixel - lastY)); //Calculate the distance between the current pixel and the pixel from last frame
            for (int i = 1; i <= dist; i++) //Loop through the points on the determined line
                DrawBrush((i * xPixel + (dist - i) * lastX) / dist, (i * yPixel + (dist - i) * lastY) / dist); //Call the DrawBrush method on the determined points
        }
        else //We shouldn't apply interpolation
            DrawBrush(xPixel, yPixel); //Call the DrawBrush method
        pressedLastFrame = true; //We should apply interpolation on the next frame
        lastX = xPixel;
        lastY = yPixel;
        SetTexture();//Updating the texture
    }
 
    void DrawBrush(int xPix, int yPix) //This function takes a point on the canvas as a parameter and draws a circle with radius brushSize around it
    {
        int i = xPix - brushSize + 1, j = yPix - brushSize + 1, maxi = xPix + brushSize - 1, maxj = yPix + brushSize - 1; //Declaring the limits of the circle
        if (i < 0) //If either lower boundary is less than zero, set it to be zero
            i = 0;
        if (j < 0)
            j = 0;
        if (maxi >= totalXPixels) //If either upper boundary is more than the maximum amount of pixels, set it to be under
            maxi = totalXPixels - 1;
        if (maxj >= totalYPixels)
            maxj = totalYPixels - 1;
        for(int x=i; x<=maxi; x++)//Loop through all of the points on the square that frames the circle of radius brushSize
        {
            for(int y=j; y<=maxj; y++)
            {
                if ((x - xPix) * (x - xPix) + (y - yPix) * (y - yPix) <= brushSize * brushSize) //Using the circle's formula(x^2+y^2<=r^2) we check if the current point is inside the circle
                    colorMap[x * totalYPixels + y] = brushColor;
            }
        }
    }
 
    void SetTexture() //This function applies the texture
    {
        generatedTexture.SetPixels(colorMap);
        generatedTexture.Apply();
    }
 
    void ResetColor() //This function resets the color to white
    {
        for (int i = 0; i < colorMap.Length; i++)
            colorMap[i] = Color.white;
        SetTexture();
    }
 
}
