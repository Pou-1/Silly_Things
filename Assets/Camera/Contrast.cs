using UnityEngine;

public class CameraPhotoFX : MonoBehaviour
{
    public Material fxMaterial;

    [Range(0f,3f)] public float contrast = 1.35f;
    [Range(0f,1f)] public float vignette = 0.35f;
    [Range(0f,1f)] public float grain = 0.05f;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (fxMaterial == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        fxMaterial.SetFloat("_Contrast", contrast);
        fxMaterial.SetFloat("_Vignette", vignette);
        fxMaterial.SetFloat("_Grain", grain);

        Graphics.Blit(src, dest, fxMaterial);
    }
}