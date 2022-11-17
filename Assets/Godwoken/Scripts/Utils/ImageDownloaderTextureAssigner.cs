using Nethereum.Unity.Rpc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nethereum.Unity;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Godwoken
{
    public class ImageDownloaderTextureAssigner : UnityRequest<bool>
    {
        public IEnumerator DownloadAndSetImageTexture(string url, UnityAction<Sprite> callback)
        {
            // resolve ipfs if its an ipfs image
            if (url.StartsWith("ipfs"))
            {
                url = IpfsUrlService.ResolveIpfsUrlGateway(url);
            }

            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                DownloadHandler handle = webRequest.downloadHandler;
                yield return webRequest.SendWebRequest();


                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Exception = new Exception(webRequest.error);
                        yield break;

                    case UnityWebRequest.Result.ProtocolError:
                        Exception = new Exception("Http Error: " + webRequest.error);
                        yield break;

                    case UnityWebRequest.Result.Success:
                        try
                        {
                            Texture2D texture2d = DownloadHandlerTexture.GetContent(webRequest);

                            Sprite sprite = null;
                            sprite = Sprite.Create(texture2d, new Rect(0, 0, texture2d.width, texture2d.height), UnityEngine.Vector2.zero);

                            if (sprite != null)
                            {
                                callback.Invoke(sprite);
                            }
                        }
                        catch (Exception e)
                        {
                            Exception = e;
                            yield break;
                        }
                        break;
                }
            }
        }
    }
}