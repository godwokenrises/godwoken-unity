using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.Networking;
using Nethereum.Unity.Rpc;
using UnityEngine;

namespace Godwoken
{
    public class ExampleNFTMetadataUnityRequest : UnityRequest<List<ExampleNFTMetadata>>
    {
        public IEnumerator GetAllMetadata(List<string> metadataUrls)
        {
            var returnData = new List<ExampleNFTMetadata>();

            foreach (var metadataUrl in metadataUrls)
            {
                if (string.IsNullOrEmpty(metadataUrl))
                    continue;
                
                using (UnityWebRequest webRequest = UnityWebRequest.Get(metadataUrl))
                {
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
                                var rootData = JsonUtility.FromJson<RootMetadata>("{\"tokens\":" + webRequest.downloadHandler.text + "}");
                                
                                if (rootData != null)
                                {
                                    foreach (var data in rootData.tokens)
                                    {
                                        returnData.Add(data);
                                    }
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
                Result = returnData;
            }
        }
    }
}