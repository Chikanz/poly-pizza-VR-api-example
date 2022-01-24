using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using PolyPizza;

namespace PolyPizza
{
    public class APIManager : MonoBehaviour
    {
        public static APIManager instance; 
        
        private const float MIN_BOUNDING_BOX_SIZE_FOR_SIZE_FIT = 0.001f;

        // const string URL = "https://api.poly.pizza/v1/";
        private const string URL = "http://127.0.0.1:3000/v1";
        public string APIKEY;

        public List<GameObject> pooledObjects;
        public int PoolSize = 2;

        private async void Awake()
        {
            if (!instance) instance = this;
            
            if (APIKEY == string.Empty)
            {
                Debug.LogError("API key wasn't set. Please get a key from https://poly.pizza/settings/api");
                enabled = false;
            }

            pooledObjects = new List<GameObject>();
            GameObject tmp;
            for (int i = 0; i < PoolSize; i++)
            {
                tmp = new GameObject();
                tmp.AddComponent<DestroyChildrenOnDisable>();
                var gltf = tmp.AddComponent<GltfBoundsAsset>();
                gltf.createBoxCollider = false;

                tmp.SetActive(false);
                pooledObjects.Add(tmp);
            }
        }

        private GameObject GetPooledObject()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                if (!pooledObjects[i].activeInHierarchy)
                {
                    pooledObjects[i].SetActive(true);
                    return pooledObjects[i];
                }
            }

            return null;
        }

        static bool ComputeScaleFactorToFit(Bounds bounds, float desiredSize, out float scaleFactor)
        {
            float biggestSide = Math.Max(bounds.size.x, Math.Max(bounds.size.y, bounds.size.z));
            if (biggestSide < MIN_BOUNDING_BOX_SIZE_FOR_SIZE_FIT)
            {
                scaleFactor = 1.0f;
                return false;
            }

            scaleFactor = desiredSize / biggestSide;
            return true;
        }


        public async UniTask<GameObject> MakeModel(Model model, float scale = 1)
        {
            var newObj = GetPooledObject();
            var gltf = newObj.GetComponent<GltfBoundsAsset>();
            var task = gltf.Load(model.Download.ToString());
            await task;
        
            if (ComputeScaleFactorToFit(gltf.bounds, scale, out float scaleFactor))
                newObj.transform.localScale = Vector3.one * scaleFactor;
        
            return newObj;
        }

        private UnityWebRequestAsyncOperation GetReq(string url)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.SetRequestHeader("X-Auth-Token", APIKEY);
            return www.SendWebRequest();
        }

        private async UniTask<Model> GetModelByID(string id)
        {
            var res = await GetReq($"{URL}/model/{id}");

            if (res.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(res.downloadHandler.text);
                return null;
            }
            else
            {
                return Model.FromJson(res.downloadHandler.text);
            }
        }
        
        public async UniTask<Model> GetExactModel(string keyword)
        {
            if (keyword.Length <= 2) return null;
            
            var res = await GetReq($"{URL}/search/{keyword}");
            if (res.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(res.downloadHandler.text);
                return null;
            }
            else
            {
                var search = SearchResults.FromJson(res.downloadHandler.text);
                if (search.Total == 0) return null;
                
                foreach (var model in search.Results)
                {
                    if (String.Equals(model.Title, keyword, StringComparison.CurrentCultureIgnoreCase)) return model;
                }

                return search.Results[0];
            }
        }
    }
}