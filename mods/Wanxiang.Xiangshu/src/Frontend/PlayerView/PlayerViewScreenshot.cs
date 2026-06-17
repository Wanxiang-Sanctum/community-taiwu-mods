using Cysharp.Threading.Tasks;
using UnityEngine;
using Wanxiang.Xiangshu.Frontend.Chat;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.PlayerView;

internal static class PlayerViewScreenshot
{
    public static async UniTask<IpcCapturePlayerViewResponse> CaptureAsync(CancellationToken cancellationToken)
    {
        await UniTask.SwitchToMainThread(cancellationToken);

        int width = Screen.width;
        int height = Screen.height;

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("Unity screen dimensions are unavailable.");
        }

        Camera[] cameras =
        [
            .. Camera.allCameras
            .Where(static camera => camera.enabled
                && camera.gameObject.activeInHierarchy
                && camera.targetTexture == null
                && camera.targetDisplay == 0)
            .OrderBy(static camera => camera.depth),
        ];

        if (cameras.Length == 0)
        {
            throw new InvalidOperationException("No active screen camera is available for player-view screenshot.");
        }

        List<Canvas> disabledChatWindowCanvases = DisableChatWindowCanvases();
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            width,
            height,
            depthBuffer: 24,
            RenderTextureFormat.ARGB32);
        Texture2D texture = new(
            width,
            height,
            TextureFormat.RGB24,
            mipChain: false);
        RenderTexture? previousActive = RenderTexture.active;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Canvas.ForceUpdateCanvases();

            RenderTexture.active = renderTexture;
            GL.Clear(clearDepth: true, clearColor: true, Color.black);

            foreach (Camera camera in cameras)
            {
                cancellationToken.ThrowIfCancellationRequested();
                camera.targetTexture = renderTexture;
                camera.Render();
                camera.targetTexture = null;
            }

            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, width, height), destX: 0, destY: 0);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            byte[] png = texture.EncodeToPNG();

            return new IpcCapturePlayerViewResponse(png);
        }
        finally
        {
            foreach (Camera camera in cameras)
            {
                camera.targetTexture = null;
            }

            RestoreCanvases(disabledChatWindowCanvases);
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
            UnityEngine.Object.Destroy(texture);
        }
    }

    private static List<Canvas> DisableChatWindowCanvases()
    {
        List<Canvas> disabledCanvases = [];

        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.name != XiangshuChatWindow.RootGameObjectName
                || !gameObject.activeInHierarchy)
            {
                continue;
            }

            foreach (Canvas canvas in gameObject.GetComponentsInChildren<Canvas>(includeInactive: true))
            {
                if (canvas.enabled)
                {
                    canvas.enabled = false;
                    disabledCanvases.Add(canvas);
                }
            }
        }

        return disabledCanvases;
    }

    private static void RestoreCanvases(List<Canvas> canvases)
    {
        foreach (Canvas canvas in canvases)
        {
            canvas.enabled = true;
        }
    }
}
