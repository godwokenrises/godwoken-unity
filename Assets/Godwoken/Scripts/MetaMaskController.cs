using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using Nethereum.Unity.Metamask;
using Nethereum.Unity.Contracts;
using Nethereum.Unity.Rpc;
using UnityEngine.Events;

namespace Godwoken
{
    public class MetaMaskController : MonoBehaviour
    {
        private bool _initialized;
        private string _address;
        private BigInteger _chainId;
        
        private UnityAction<string> _onInitialized;
        private UnityAction<string> _onContractSetup;
        
        void Start()
        {
        
        }

        private void ThrowError(string errorMessage)
        {
            Debug.LogError("<color=red>" + errorMessage + "</color>");
        }
        
        public void Initialize(UnityAction<string> onInitialized)
        {
            if (!MetamaskInterop.IsMetamaskAvailable())
            {
                ThrowError("Metamask not available");
                return;
            }

            _onInitialized = onInitialized;
            MetamaskInterop.EnableEthereum(gameObject.name, nameof(EthereumEnabled), nameof(ThrowError));
        }

        public void SetupContract(string transactionHash, UnityAction<string> setupComplete)
        {
            if (!MetamaskInterop.IsMetamaskAvailable())
            {
                ThrowError("Metamask not available");
                return;
            }

            _onContractSetup = setupComplete;
            StartCoroutine(GetSmartContractAddressFromReceipt(transactionHash));
        }

        public string GetAddress()
        {
            return _address;
        }
        
        public void GetNumberOfExampleNFTs(string contractAddress, UnityAction<int> onComplete)
        {
            StartCoroutine(GetNumberOfExampleNFTsCoroutine(contractAddress, onComplete));
        }

        public void GetExampleNFTs(string contractAddress, UnityAction<List<Sprite>> onComplete)
        {
            StartCoroutine(GetExampleNFTsCoroutine(contractAddress, onComplete));
        }
        
        public IContractTransactionUnityRequest UnityToGodwokenTransaction()
        {
            if (!MetamaskInterop.IsMetamaskAvailable())
            {
                ThrowError("Metamask not available");
                return null;
            }
            return new MetamaskTransactionUnityRequest(_address, GetUnityRpcRequestClientFactory());
        }

        private IEnumerator GetSmartContractAddressFromReceipt(string hash)
        {
            var transactionReceiptPolling = new TransactionReceiptPollingRequest(GetUnityRpcRequestClientFactory());

            yield return transactionReceiptPolling.PollForReceipt(hash, 2);

            var deploymentReceipt = transactionReceiptPolling.Result;
            _onContractSetup.Invoke(deploymentReceipt.ContractAddress);
        }
        
        private void EthereumEnabled(string addressSelected)
        {
            if (!_initialized)
            {
                MetamaskInterop.EthereumInit(gameObject.name, nameof(NewAccount), nameof(ChainUpdate));
                MetamaskInterop.GetChainId(gameObject.name, nameof(ChainUpdate), nameof(ThrowError));
                _initialized = true;
            }
            NewAccount(addressSelected);
        }

        private void ChainUpdate(string chainId)
        {
            _chainId = new HexBigInteger(chainId).Value;
            try
            {
                StartCoroutine(GetBlockNumber());
            }
            catch(Exception ex)
            {
                ThrowError(ex.Message);
            }
        }

        private IEnumerator GetBlockNumber()
        {
            var blockRequest = new EthBlockNumberUnityRequest(GetUnityRpcRequestClientFactory());
            yield return blockRequest.SendRequest();
        }

        private void NewAccount(string accountAddress)
        {
            Debug.Log("MetaMask Login Successful");
            Debug.Log(accountAddress);
            _address = accountAddress;
            _onInitialized.Invoke(_address);
        }

        private IUnityRpcRequestClientFactory GetUnityRpcRequestClientFactory()
        {
            if (!MetamaskInterop.IsMetamaskAvailable())
            {
                ThrowError("Metamask not available");
                return null;
            }
            return new MetamaskRequestRpcClientFactory(_address, null, 5000);
        }
        
        private IEnumerator GetNumberOfExampleNFTsCoroutine(string contractAddress, UnityAction<int> onComplete)
        {
            var nftsOfUser = new ERC721ExampleUnityRequest(GetAddress(), GetUnityRpcRequestClientFactory());
            yield return nftsOfUser.GetAllNFTUris(contractAddress, GetAddress());
            if (nftsOfUser.Exception != null)
            {
                yield break;
            }
            if (nftsOfUser.Result != null)
            {
                onComplete.Invoke(nftsOfUser.Result.Count);
            }
        }

        private IEnumerator GetExampleNFTsCoroutine(string contractAddress, UnityAction<List<Sprite>> onComplete)
        {
            var nftsOfUser = new ERC721ExampleUnityRequest(GetAddress(), GetUnityRpcRequestClientFactory());
            yield return nftsOfUser.GetAllNFTUris(contractAddress, GetAddress());
            if (nftsOfUser.Exception != null)
            {
                yield break;
            }
            if (nftsOfUser.Result != null)
            {
                var metadataUnityRequest = new ExampleNFTMetadataUnityRequest();
                yield return metadataUnityRequest.GetAllMetadata(nftsOfUser.Result);
                if (metadataUnityRequest.Exception != null)
                {
                    ThrowError(metadataUnityRequest.Exception.Message);
                    yield break;
                }

                if (metadataUnityRequest.Result != null)
                {
                    var sprites = new List<Sprite>();
                    var textureAssigner = new ImageDownloaderTextureAssigner();
                    foreach (var item in metadataUnityRequest.Result)
                    {
                        if (string.IsNullOrEmpty(item.image))
                        {
                            continue;
                        }

                        yield return textureAssigner.DownloadAndSetImageTexture(item.image, sprite =>
                            {
                                sprites.Add(sprite);
                            }
                        );
                    }

                    onComplete.Invoke(sprites);
                }
            }
        }
    }
}

