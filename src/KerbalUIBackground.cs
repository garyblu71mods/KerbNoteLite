using UnityEngine;

public static class KerbalUIBackground
{
    private static Texture2D backgroundTexture;

    public static void LoadTexture()
    {
        backgroundTexture = GameDatabase.Instance.GetTexture("KerbCalcProject/BackgroundWindow", false);

        if (backgroundTexture == null)
        {
            Debug.LogWarning("[KerbalUIBackground] Nie udało się załadować BackgroundWindow.png");
        }
    }

    public static void Draw(Rect rect)
    {
        if (backgroundTexture == null)
        {
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            backgroundTexture.Apply();
        }

        GUI.DrawTexture(rect, backgroundTexture);
    }
}