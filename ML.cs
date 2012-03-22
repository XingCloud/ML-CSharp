/*-------------------------------------------------------------------------------
 * CSharp SDK 
 * SDK包装了行云多语言REST翻译接口，主要实现了，即时翻译和精确翻译，以及单文件更新
 * 和简单缓存管理功能等。
 * 
 -------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.Security;
using System.Net;
using System.Collections;
using System.Web;
using System.IO;
using System.Data;

namespace Com.XingCloud.ML
{

    public class ML
    {
        public const string APIPREFIX = "http://i.xingcloud.com/api/v1";
        public const string SNAPSHOT = "snapshot.txt";

        private bool mTransType = false;

        private string mServiceName;

        private string mApiKey;

        private string mSourceLang;

        private string mTargetLang;

        private int mQueueNumber = 10;

        private string mCacheDir = "";

        private Dictionary<string, Dictionary<string, string>> mTransHash = new Dictionary<string, Dictionary<string, string>>();

        private Queue<string> mCacheQueue = new Queue<string>();

        /// <summary>
        /// 初始化多语言对象
        /// </summary>
        /// <param name="serviceName">是在行云平台申请多语言服务的服务名称</param>
        /// <param name="apiKey">行云平台的每个多语言服务都会有一个给定的apiKey，服务的唯一标识</param>
        /// <param name="sourceLang">未翻译词条对应语言的缩写（详细请查看行云平台具体文档）</param>
        /// <param name="targetLang">翻译结果对应语言的缩写</param>
        /// <param name="cacheDir">本地缓存目录地址，默认是当前文件目录下</param>
        /// <param name="queueNumber">本地缓存队列长度，默认是10</param>
        /// <returns></returns>
        public ML(string serviceName, string apiKey, string sourceLang, string targetLang) : this(serviceName, apiKey, sourceLang, targetLang, "", 10) {}

        public ML(string serviceName, string apiKey, string sourceLang, string targetLang, int queueNumber) : this(serviceName, apiKey, sourceLang, targetLang, "", queueNumber) {}

        public ML(string serviceName, string apiKey, string sourceLang, string targetLang, string cacheDir) : this(serviceName, apiKey, sourceLang, targetLang, cacheDir, 10) {}

        public ML(string serviceName, string apiKey, string sourceLang, string targetLang, string cacheDir, int queueNumber)
        {
            this.mServiceName = serviceName;
            this.mApiKey = apiKey;
            this.mSourceLang = sourceLang;
            this.mTargetLang = targetLang;
            this.mCacheDir = cacheDir;
            this.mQueueNumber = queueNumber;

            if (this.mSourceLang == this.mTargetLang)
            {
                this.mTransType = false;
            }
            else
            {
                this.mTransType = true;
                this.UpdateLocalCache();
            }

        }

        /// <summary>
        /// 翻译词条接口，输入词条，返回翻译结果，如果本地缓存中没有翻译结果，则返回原词条
        /// </summary>
        /// <param name="words">需要翻译的词条</param>
        /// <param name="fileName">本地缓存文件后缀名,以.json结尾,如果文件名后缀不是.json,那么会强行加以个.json后缀</param>
        /// <param name="timelyTrans">是否是及时翻译，这种翻译及时返回翻译结果，但是不会在项目列表中记录词条，不保证翻译的精确性</param>
        /// <returns></returns>
        public string Trans(string words)
        {
            string fileName = "xc_words.json";
            bool timelyTrans = false;
            return this.Trans(words, fileName, timelyTrans);
        }

        public string Trans(string words, bool timelyTrans)
        {
            string fileName = "xc_words.json";
            return this.Trans(words, fileName, timelyTrans);
        }

        public string Trans(string words, string fileName)
        {
            bool timelyTrans = false;
            return this.Trans(words, fileName, timelyTrans);
        }

        public string Trans(string words, string fileName, bool timelyTrans)
        {
            if (!fileName.EndsWith(".json"))
            {
                fileName = fileName + ".json";
            }
            if (words.Trim() != "" && this.mTransType)
            {
                if (timelyTrans)
                {
                    string transWords = this.TimelyTrans(words);
                    return transWords;
                }
                else
                {
                    string transWords = this.ExactTrans(words, fileName);
                    return transWords;
                }
            }
            else 
            {
                return words;
            }
        }

        private string GetLocalCachePath(string fileName)
        {
            string filePath = this.mCacheDir + "\\" + this.mServiceName + "_" + this.mTargetLang + "_" + fileName;
            filePath = filePath.Trim().Trim("\\".ToCharArray());
            return filePath;
        }

        private string ReadLocalCache(string fileName)
        {
            string filePath = this.GetLocalCachePath(fileName);
            if (!File.Exists(filePath))
            {
                return "";
            }
            StreamReader sr = new StreamReader(filePath);
            string json = sr.ReadToEnd();
            sr.Close();
            return json;
        }

        private bool WriteLocalCache(string fileName, string content)
        {
            string filePath = this.GetLocalCachePath(fileName);
            try
            {
                Stream fs = File.Open(filePath, FileMode.Create);
                TextWriter fileWriter = new StreamWriter(fs);
                fileWriter.Write(content);
                fileWriter.Close();
                fs.Close();
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.LogWrite(ex);
                return false;
            }
        }

        private bool UpdateLocalCache()
        {
            string snapShot = this.GetFilesInfo();
            Dictionary<string, string> outDatedFileList = this.GetOutDatedFileList(snapShot);
            this.BatchUpdateLocalFile(outDatedFileList);
            return true;
        }

        private bool BatchUpdateLocalFile(Dictionary<string, string> fileList)
        {
            if (fileList.Count() == 0)
                return true;
            try
            {
                foreach (KeyValuePair<string, string> item in fileList)
                {
                    this.DownloadFile(item);
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.LogWrite(ex);
                return false;
            }
        }

        private Dictionary<string, string> GetOutDatedFileList(string snapShot)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Dictionary<string, object> newFileInfosHash = (Dictionary<string, object>)serializer.DeserializeObject(snapShot);
                Dictionary<string, object> newDataHash = (Dictionary<string, object>)newFileInfosHash["data"];
                string remotePrefix = newFileInfosHash["request_prefix"].ToString();

                Dictionary<string, string> fileList = new Dictionary<string, string>();
                string localJson = this.ReadLocalCache(SNAPSHOT);
                if (localJson == "")
                {
                    foreach (KeyValuePair<string, object> item in newDataHash)
                    {
                        fileList.Add(this.GetRemoteFilePath(item, remotePrefix), item.Key);
                    }
                    this.WriteLocalCache(SNAPSHOT, snapShot);
                    return fileList;
                }
                Dictionary<string, object> oldDataHash = (Dictionary<string, object>)((Dictionary<string, object>)serializer.DeserializeObject(localJson))["data"];

                foreach (KeyValuePair<string, object> item in newDataHash)
                {
                    if (oldDataHash.ContainsKey(item.Key) && item.Value.ToString() == oldDataHash[item.Key].ToString())
                    {
                        continue;
                    }
                    else
                    {
                        fileList.Add(this.GetRemoteFilePath(item, remotePrefix), item.Key);
                    }
                }
                this.WriteLocalCache(SNAPSHOT, snapShot);
                return fileList;
            }
            catch (Exception ex)
            {
                ErrorLog.LogWrite(ex);
                Dictionary<string, string> empty = new Dictionary<string, string>();
                return empty;
            }
        }

        private string GetRemoteFilePath(KeyValuePair<string, object> item, string remotePrefix)
        {
            string remotePath = remotePrefix + "/" + item.Key + "?md5=" + item.Value.ToString();
            return remotePath;
        }

        private string GetFilesInfo()
        { 
            string url = APIPREFIX + "/file/snapshot";
            string timeStamp = this.GetTimeStamp();
            string authorizer = timeStamp + this.mApiKey;
            string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(authorizer, "MD5").ToLower();
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("service_name", this.mServiceName);
            data.Add("timestamp", timeStamp);
            data.Add("hash", hash);
            data.Add("locale", this.mTargetLang);
            string ret = this.GetRequest(url, data);
            return ret;
        }

        private string TimelyTrans(string words)
        {
            string url = APIPREFIX + "/string/translate";
            string timeStamp = this.GetTimeStamp();
            string authorizer = timeStamp + this.mApiKey;
            string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(authorizer, "MD5").ToLower();
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("service_name", this.mServiceName);
            data.Add("timestamp", timeStamp);
            data.Add("hash", hash);
            data.Add("source", this.mSourceLang);
            data.Add("target", this.mTargetLang);
            data.Add("query", words);
            string ret = this.GetRequest(url, data);
            return ret;
        }

        private string ExactTrans(string words, string fileName)
        {
            if (words.Trim() != "" && this.mTransType)
            {
                string transWords = this.GetTransWords(words, fileName);
                return transWords;
            }
            else
            {
                return words;
            }

        }

        private string GetTransWords(string words, string fileName)
        {
            if (!File.Exists(this.GetLocalCachePath(fileName)))
            {
                this.AddString(words, fileName);
                return words;
            }
            if (!this.mTransHash.ContainsKey(fileName))           //判断是否该文件已被加载
            {
                if (this.mCacheQueue.Count() >= this.mQueueNumber)
                {
                    string outDateFileName = this.mCacheQueue.Dequeue();
                    this.mTransHash.Remove(outDateFileName);
                }
                string localJson = this.ReadLocalCache(fileName);
                if (localJson == "")
                {
                    this.AddString(words, fileName);
                    return words;
                }
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Dictionary<string, object> tmpWordsDic = new Dictionary<string, object>();
                tmpWordsDic = (Dictionary<string, object>)serializer.DeserializeObject(localJson);
                Dictionary<string, string> cacheDic = new Dictionary<string, string>();
                cacheDic = this.DictTransform(tmpWordsDic);
                this.mTransHash.Add(fileName, cacheDic);
                this.mCacheQueue.Enqueue(fileName);
            }
            Dictionary<string, string> cache = (Dictionary<string, string>)this.mTransHash[fileName];
            if (cache.ContainsKey(words))
            {
                return this.mTransHash[fileName][words];
            }
            else
            {
                this.AddString(words, fileName);
                return words;
            }
        }

        private Dictionary<string, string> DictTransform(Dictionary<string, object> objDic)
        {
            Dictionary<string, string> strDic = new Dictionary<string, string>();
            foreach (KeyValuePair<string, object> item in objDic)
            {
                strDic.Add(item.Key, item.Value.ToString());
            }
            return strDic;
        }

        /// <summary>
        /// 访问Rest接口，添加字符串
        /// </summary>
        /// <param name="words">需要加入到项目文件列表中的词条</param>
        /// <param name="fileName">词需要加入到的文件名</param>
        private void AddString(string words, string fileName)
        {
            string timeStamp = this.GetTimeStamp();
            string authorizer = timeStamp + this.mApiKey;
            string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(authorizer, "MD5").ToLower();
            string url = APIPREFIX + "/string/add";
            Dictionary<string, string> urlParams = new Dictionary<string, string>();
            urlParams.Add("service_name", this.mServiceName);
            urlParams.Add("data", words);
            urlParams.Add("timestamp", timeStamp);
            urlParams.Add("hash", hash);
            urlParams.Add("file_path", fileName);
            urlParams.Add("create", "1");
            PostRequest(url, urlParams);
        }

        /// <summary>
        /// 以POST的方式发送请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="urlParams">用来存放请求的参数</param>
        /// <returns></returns>
        private string PostRequest(string url, Dictionary<string, string> urlParams)
        {
            try
            {
                Encoding encoding = Encoding.GetEncoding("utf-8");
                string param = "";
                foreach (KeyValuePair<string, string> item in urlParams)
                {
                    param += HttpUtility.UrlEncode(item.Key, encoding) +
                            "=" + HttpUtility.UrlEncode(item.Value, encoding) + "&";
                }
                byte[] postBytes = Encoding.ASCII.GetBytes(param);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
                request.ContentLength = postBytes.Length;
                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(postBytes, 0, postBytes.Length);
                }
                using (WebResponse response = (WebResponse) request.GetResponse())
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader retReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        return retReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.LogWrite(ex);
                return "";
            }
        }

        private string GetTimeStamp()
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            DateTime nowTime = DateTime.Now;
            long unixTime = (long)Math.Round((nowTime - startTime).TotalMilliseconds, MidpointRounding.AwayFromZero);
            string timeStamp = unixTime.ToString();
            return timeStamp;
        }

        /// <summary>
        /// Get请求方式
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="dic">请求需要的参数字典</param>
        /// <returns></returns>
        private string GetRequest(string url, Dictionary<string, string> dic)
        {
            string data = "";
            foreach (KeyValuePair<string, string> item in dic)
            {
                data += "&" + item.Key + "=" + item.Value;
            }

            string reqUrl = url + "?" + data;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(reqUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader retReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string ret = retReader.ReadToEnd();
                return ret;
            }
            catch (Exception ex)
            {
                ErrorLog.LogWrite(ex);
                return "";
            }
        }
        
        /// <summary>
        /// 获取服务器中对应文件
        /// </summary>
        /// <param name="item">文件信息对:文件地址，文件名</param>
        /// <returns></returns>
        private bool DownloadFile(KeyValuePair<string, string> item)
        {
            string url = item.Key;
            string fileName = item.Value;
            StringBuilder stringBuilder = new StringBuilder();
            string rLine = string.Empty;
            try
            {
                HttpWebRequest httpWebRequest = System.Net.WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.KeepAlive = false;
                httpWebRequest.AllowAutoRedirect = false;
                httpWebRequest.UserAgent = "Mozilla/5.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727)";
                httpWebRequest.Timeout = 10000;
                httpWebRequest.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
                using (HttpWebResponse res = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    if (res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.PartialContent)
                    {
                        System.IO.Stream strem = res.GetResponseStream();
                        System.IO.StreamReader r = new System.IO.StreamReader(strem);
                        while (rLine != null)
                        {
                            rLine = r.ReadLine();
                            stringBuilder.Append(rLine);
                        }
                    }
                }
                this.WriteLocalCache(fileName, stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                ErrorLog.LogWrite(ex);
                return false;
            }
            return true;
        }
    }

    public class ErrorLog
    {
        public static void CreateLogFile()
        {
            if (!File.Exists("ML.log"))
            {
                try
                {
                    FileStream file = File.Create("ML.log");
                    file.Dispose();
                    file.Close();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static void LogWrite(Exception ex)
        {
            CreateLogFile();
            StreamWriter sw = new StreamWriter("ML.log", true, System.Text.Encoding.UTF8);

            try
            {
                sw.WriteLine("日期：" + System.DateTime.Now.ToString());
                sw.WriteLine("错误源：" + ex.Source);
                sw.WriteLine("错误信息：" + ex.Message);
                sw.WriteLine();
                sw.Flush();
                sw.Dispose();
                sw.Close();
            }
            catch (Exception exc)
            {
                sw.Dispose();
                sw.Close();
            }
        }
    }
}
