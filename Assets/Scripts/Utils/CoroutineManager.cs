using UnityEngine;
using System.Collections;

namespace ChessGame.Utils
{
    /// <summary>
    /// 协程管理器 - 用于在非MonoBehaviour类中启动协程
    /// </summary>
    public class CoroutineManager : MonoBehaviour
    {
        private static CoroutineManager _instance;
        
        public static CoroutineManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CoroutineManager");
                    _instance = go.AddComponent<CoroutineManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 启动协程
        /// </summary>
        public Coroutine StartCoroutineEx(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
        
        /// <summary>
        /// 停止协程
        /// </summary>
        public void StopCoroutineEx(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
    }
} 