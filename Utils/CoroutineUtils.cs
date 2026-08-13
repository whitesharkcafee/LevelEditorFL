
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    public static class CoroutineUtils
    {
        static Dictionary<string, List<object>> executingCoroutinesWithIDs = new Dictionary<string, List<object>>();

        public static void Start(IEnumerator coroutine)
        {
            NativeModLoader.Instance.StartCoroutine(coroutine);
        }
        public static object Start(IEnumerator coroutine, string id)
        {
            if (!executingCoroutinesWithIDs.ContainsKey(id))
                executingCoroutinesWithIDs.Add(id, new List<object>());

            object coroutineToken = NativeModLoader.Instance.StartCoroutine(coroutine);
            executingCoroutinesWithIDs[id].Add(coroutineToken);

            return coroutineToken;
        }

        public static void Stop(object coroutineToken)
        {
            foreach (var keyPair in executingCoroutinesWithIDs)
            {
                if (keyPair.Value.Remove(coroutineToken))
                {
                    break;
                }
            }

            if (coroutineToken is Coroutine coroutine)
            {
                NativeModLoader.Instance.StopCoroutine(coroutine);
            }
            else if (coroutineToken is System.Collections.IEnumerator enumerator)
            {
                NativeModLoader.Instance.StopCoroutine(enumerator);
            }
            else if (coroutineToken is string methodName)
            {
                NativeModLoader.Instance.StopCoroutine(methodName);
            }
        }
        public static void StopAllCoroutines(string coroutinesID)
        {
            if (!executingCoroutinesWithIDs.ContainsKey(coroutinesID))
                return;

            foreach (var coroutine in executingCoroutinesWithIDs[coroutinesID])
            {
                if (coroutine == null) continue;

                if (coroutine is Coroutine coroutineRef)
                {
                    NativeModLoader.Instance.StopCoroutine(coroutineRef);
                }
                else if (coroutine is System.Collections.IEnumerator enumerator)
                {
                    NativeModLoader.Instance.StopCoroutine(enumerator);
                }
                else if (coroutine is string methodName)
                {
                    NativeModLoader.Instance.StopCoroutine(methodName);
                }
            }

            executingCoroutinesWithIDs.Remove(coroutinesID);
        }
    }
}
