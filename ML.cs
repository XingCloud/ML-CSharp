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

        private bool mTransType = false;

        private string mServiceName;

        private string mApiKey;

        private string mSourceLang;

        private string mTargetLang;

        private bool mDebug;

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
        /// <param name="debug">debug错误日志输出</param>
        /// <returns></returns>
        public ML(string serviceName, string apiKey, string sourceLang, string targetLang) : this(serviceName, apiKey, sourceLang, targetLang, false) { }
        
        public ML(string serviceName, string apiKey, string sourceLang, string targetLang, bool debug)
        {
            this.mServiceName = serviceName;
            this.mApiKey = apiKey;
            this.mSourceLang = sourceLang;
            this.mTargetLang = targetLang;
            this.mDebug = debug;
            if (this.mSourceLang == this.mTargetLang)
            {
                this.mTransType = false;
            }
            else
            {
                this.mTransType = true;
            }

        }

        /// <summary>
        /// 人工翻译，将词条提交到多语言平台，对其可进行管理，最终可以获取精确翻译结果。
        /// </summary>
        /// <param name="words">需要翻译的词条</param>
        /// <returns></returns>
        public string Trans(string words)
        {
            string fileName = "xc_words.json";
            bool timelyTrans = false;
            return this.Trans(words, fileName, timelyTrans);
        }

        /// <summary>
        /// 机器翻译，立刻获取翻译结果
        /// </summary>
        /// <param name="words">需要翻译的词条</param>
        /// <returns></returns>
        public string Translate(string words)
        {
            string fileName = "xc_words.json";
            bool timelyTrans = true;
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

        private string TimelyTrans(string words)
        {
            string url = APIPREFIX + "/string/translate";
            string timeStamp = this.GetTimeStamp();
            string authorizer = timeStamp + this.mApiKey;
            string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(authorizer, "MD5").ToLower();
            Dictionary<string, string> urlParams = new Dictionary<string, string>();
            urlParams.Add("service_name", this.mServiceName);
            urlParams.Add("timestamp", timeStamp);
            urlParams.Add("hash", hash);
            urlParams.Add("source", this.mSourceLang);
            urlParams.Add("target", this.mTargetLang);
            urlParams.Add("query", words);
            string ret = this.PostRequest(url, urlParams);
            return ret;
        }

        private string ExactTrans(string words, string fileName)
        {
            if (words.Trim() != "" && this.mTransType)
            {
                string querySet = this.GetTransWords(words, fileName);
                if (querySet == "")
                {
                    return words;
                }
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Dictionary<string, object> transHash = (Dictionary<string, object>)serializer.DeserializeObject(querySet);
                Dictionary<string, object> newDataHash = (Dictionary<string, object>)((Dictionary<string, object>)transHash["data"])["data"];
                if (newDataHash.ContainsKey(words))
                {
                    if (newDataHash[words].ToString() == words)
                    {
                        this.AddString(words, fileName);
                    }
                    return newDataHash[words].ToString();
                }
                else
                {
                    return words;
                }
            }
            else
            {
                return words;
            }

        }

        private string GetTransWords(string words, string fileName)
        {
            string timeStamp = this.GetTimeStamp();
            string authorizer = timeStamp + this.mApiKey;
            string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(authorizer, "MD5").ToLower();
            string url = APIPREFIX + "/string/get";

            Dictionary<string, string> urlParams = new Dictionary<string, string>();
            urlParams.Add("service_name", this.mServiceName);
            urlParams.Add("query", words);
            urlParams.Add("timestamp", timeStamp);
            urlParams.Add("hash", hash);
            urlParams.Add("file_path", fileName);
            urlParams.Add("target", this.mTargetLang);
            return this.GetRequest(url, urlParams);
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
                if (this.mDebug)
                {
                    ErrorLog.LogWrite(ex);
                }
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
                data += "&" + HttpUtility.UrlEncode(item.Key) + "=" + HttpUtility.UrlEncode(item.Value);
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
                if (this.mDebug)
                {
                    ErrorLog.LogWrite(ex);
                }
                return "";
            }
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
