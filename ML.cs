using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Web;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Data;
using System.Xml;
using System.Configuration;
using System.Web.Security;

namespace Com.XingCloud.ML 
{
    public class ML
    {
        Hashtable WordTable = new Hashtable();
        DataSet dataset = new DataSet();
        private string LocalXmlPath;
        private Dictionary<string, object> json;
        private object target;
        private bool AutoAddTrans;
        private bool LangSame;
        private string ServiceName;
        private string ApiKey;

        private class FileInfos
        {
            public DataEntity data = new DataEntity();
        }

        public class DataEntity
        {
            public string file_path { get; set; }
            public string status { get; set; }
            public string source { get; set; }
            public string target { get; set; }
            public string source_words_count { get; set; }
            public int human_translated { get; set; }
            public int machine_translated { get; set; }
            public string request_address { get; set; }
            public int length { get; set; }
            public string md5 { get; set; }
        }

        /*
         * 以POST的方式发送请求
         * url：请求地址
         * parameters：用来存放请求的参数
         */
        private String SendRequest(string url, Hashtable parameters)
        {
            Encoding encoding = Encoding.GetEncoding("utf-8");
            string param = "";
            foreach (DictionaryEntry dictionaryEntry in parameters)
            {
                param += HttpUtility.UrlEncode(dictionaryEntry.Key.ToString(), encoding) +
                        "=" + HttpUtility.UrlEncode(dictionaryEntry.Value.ToString(), encoding) + "&";
            }
            byte[] postBytes = Encoding.ASCII.GetBytes(param);
            HttpWebRequest httpwebrequest = (HttpWebRequest)HttpWebRequest.Create(url);
            httpwebrequest.Method = "POST";
            httpwebrequest.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
            httpwebrequest.ContentLength = postBytes.Length;
            using (Stream reqStream = httpwebrequest.GetRequestStream())
            {
                reqStream.Write(postBytes, 0, postBytes.Length);
            }
            using (WebResponse webresponse = httpwebrequest.GetResponse())
            {
                Stream responseStream = webresponse.GetResponseStream();
                using (StreamReader streamreader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    return (streamreader.ReadToEnd());
                }
            }
        }

        /*
         * 以GET方式发送请求
         */ 
        private String SendRequestGet(string timestamp, string hash,
                                      string url, string serviceName,
                                      string locale, string filepath)
        {
            System.Net.WebClient webClient = new System.Net.WebClient();
            byte[] by = webClient.DownloadData(url + "?timestamp=" + timestamp + "&hash=" + hash 
                                        + "&service_name=" + serviceName + "&locale=" + locale
                                        + "&file_path=" + filepath);
            string FileInfo = Encoding.UTF8.GetString(by);
            return FileInfo;
        }

        /*
         *从线上获得json信息
         */
        private String GetJson(string url)
        {
            string json = string.Empty;
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
                    json = r.ReadToEnd();
                }
            }
            return json;
        }

        //访问file/info，从返回值中获取xc_words.json的地址
        private string GetRequestAddress(string serviceName, string tarLang,string apiKey)
        {
            DateTime StartTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            DateTime NowTime = DateTime.Now;
            long UnixTime = (long)Math.Round((NowTime - StartTime).TotalMilliseconds, MidpointRounding.AwayFromZero);
            string TimeStamp = UnixTime.ToString();
            string PWD = TimeStamp + apiKey;
            string Hash = FormsAuthentication.HashPasswordForStoringInConfigFile(PWD, "MD5").ToLower();
            string FileinfoUrl = "http://i.xingcloud.com/api/v1/file/info";
            string Info = SendRequestGet(TimeStamp,Hash ,FileinfoUrl, serviceName, tarLang, "xc_words.json");
            JavaScriptSerializer js = new JavaScriptSerializer();
            FileInfos Fileinfo = js.Deserialize<FileInfos>(Info);
            string RequestAddress = Fileinfo.data.request_address;
            return RequestAddress;
        }

        /*
         * 在本地创建文件存放服务上获得的json信息
         */ 
        public void CreateFile(string filePath,string str)
        {
            FileStream fileStream = File.Create(filePath);
            fileStream.Close();
            FileStream fs = new FileStream(filePath, FileMode.Create);
            //获得字节数组
            byte[] data = new UTF8Encoding().GetBytes(str);
            //开始写入
            fs.Write(data, 0, data.Length);
            //清空缓冲区、关闭流
            fs.Flush();
            fs.Close();
        }

        /*
         * 读取文件
         */ 
        public string ReadFile(string filePath)
        { 
            string sLine = "";
            if (File.Exists(filePath))
            {
            StreamReader objReader = new StreamReader(filePath);
            ArrayList LineList = new ArrayList();
            while (sLine != null)
            {
                sLine = objReader.ReadLine();
                LineList.Add(sLine);
            }
            objReader.Close();
            sLine = string.Join("", (string[])LineList.ToArray(typeof(string)));
            }
            return sLine;
        }

        /*
         *ML初始化。需要先登陆行云多语言管理系统创建翻译服务 http://i.xingcloud.com/service
         *serviceName:服务名字
         *apiKey：服务apikey
         *sourceLang：原始语言
         *targetLang ：目标语言
         *autoUpdateFile ：是否更新本地文件
         *autoAddString ：时候自动添加新词
         */
        public void Init(string serviceName, string apiKey, 
                         string sourceLang, string targetLang, 
                         bool autoUpdateFile,bool autoAddTrans)
        {
            if (sourceLang.Equals(targetLang))
            {
                LangSame = true;
                return;
            }

            ApiKey = apiKey;
            AutoAddTrans = autoAddTrans;
            LangSame = false;
            LocalXmlPath = "." + "\\" + serviceName + "_" + targetLang + ".json";
            ServiceName = serviceName;
            if (autoUpdateFile)
            {
                string RequestAddress = GetRequestAddress(serviceName, targetLang, apiKey);
                CreateFile(LocalXmlPath, GetJson(RequestAddress));
            }
            string words = ReadFile(LocalXmlPath);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            json = (Dictionary<string, object>)serializer.DeserializeObject(words);         
        }

        /*
         *访问服务添加新的字符串
         */
        private void AddString(string addString)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            DateTime nowTime = DateTime.Now;
            long unixTime = (long)Math.Round((nowTime - startTime).TotalMilliseconds, MidpointRounding.AwayFromZero);
            string timestamp = unixTime.ToString();
            string PWD = timestamp + ApiKey;
            string md = FormsAuthentication.HashPasswordForStoringInConfigFile(PWD, "MD5").ToLower();
            Hashtable stringAddParameters = new Hashtable();
            stringAddParameters.Add("service_name", ServiceName);
            stringAddParameters.Add("data", addString);
            stringAddParameters.Add("timestamp", timestamp);
            stringAddParameters.Add("hash", md);
            string stringAddUrl = "http://i.xingcloud.com/api/v1/string/add";
            SendRequest(stringAddUrl, stringAddParameters);
        }

        /*
         * 翻译文本
         */ 
        public string trans(string source) 
        {
            if ("".Equals(source)) 
            {
                return source;
            }

            if (LangSame)
            {
                return source;
            }

            else
            {
                try
                {
                    if (json.TryGetValue(source, out target))
                    {
                        return (string)target;
                    }
                    else
                    {
                        //根据AutoAddTrans的值来判断是否添加词条
                        if (AutoAddTrans)
                        {
                            AddString(source);
                        }
                        return source;
                    }
                }
                catch {
                    return source;
                }
            }
        }
    }
}
