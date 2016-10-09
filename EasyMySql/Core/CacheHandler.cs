using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Web.Caching;

namespace EasyMySql.Core
{
    internal static class CacheHandler
    {
        private static ObjectCache cache = MemoryCache.Default;
        private static CacheItemPolicy policy = new CacheItemPolicy();

        public static void AddToCache(string DataHandlerName, string MethodName, string[] Parameters, object DataObject)
        {
            try
            {
                policy.AbsoluteExpiration = DateTimeOffset.Now.AddMonths(3);

                string key = GetKey(DataHandlerName, MethodName, Parameters);

                //Get key from cache
                object Object = cache[key];

                //if key exists, replace data with the new data
                if (Object != null)
                {
                    cache.Set(key, DataObject, CacheHandler.policy);
                    return;
                }

                //GetCacheList
                List<string> DataHandlerCacheList = GetDataHandlerCacheList(DataHandlerName);

                //add to cache
                cache.Set(key, DataObject, CacheHandler.policy);
                DataHandlerCacheList.Add(key);
                cache.Set(DataHandlerName, DataHandlerCacheList, CacheHandler.policy);
            }
            catch (Exception e)
            {
                if (DataHandlerName != EasyMySqlExceptionHandler.instance.ToString())
                {
                    new EasyMySqlException(DataHandlerName, e);
                }
            }
        }

        public static object GetData(string DataHandlerName, string MethodName, string[] Parameters)
        {
            try
            {
                policy.AbsoluteExpiration = DateTimeOffset.Now.AddMonths(3);
                string key = GetKey(DataHandlerName, MethodName, Parameters);

                if (cache[key] != null)
                {
                    return cache[key];
                }
            }
            catch (Exception e)
            {
                if (DataHandlerName != EasyMySqlExceptionHandler.instance.ToString())
                {
                    new EasyMySqlException(DataHandlerName, e);
                }
            }

            return null;
        }

        public static void ClearData(string DataHandlerName)
        {
            try
            {
                policy.AbsoluteExpiration = DateTimeOffset.Now.AddMonths(3);
                List<string> DataHandlerCacheList = GetDataHandlerCacheList(DataHandlerName);

                foreach (string s in DataHandlerCacheList)
                {
                    try
                    {
                        cache.Remove(s);
                    }
                    catch (Exception)
                    {

                    }
                }

                cache.Remove(DataHandlerName);
            }
            catch (Exception e)
            {
                if (DataHandlerName != EasyMySqlExceptionHandler.instance.ToString())
                {
                    new EasyMySqlException(DataHandlerName, e);
                }
            }
        }

        public static void Clear()
        {
            while(true)
            {
                try
                {
                    cache.Remove(cache.First().Key);
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private static string GetKey(string DataHandlerName, string MethodName, string[] Parameters)
        {
            string Key = DataHandlerName + "-" + MethodName;

            foreach (string s in Parameters)
            {
                Key += s;
            }

            return Key;
        }

        private static List<string> GetDataHandlerCacheList(string DataHandlerName)
        {
            List<string> DataHandlerCacheList = (List<string>)cache[DataHandlerName];

            if (DataHandlerCacheList == null)
            {
                DataHandlerCacheList = new List<string>();
            }

            return DataHandlerCacheList;
        }
    }
}
