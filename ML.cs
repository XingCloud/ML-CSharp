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
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace Com.XingCloud.ML
{
    public class ML
    {
        SqlConnection conn;
        private int partition = 100;
        string DBtitle = "language";
        private string ServiceName;
        private string ApiKey;
        private string SourceLang;
        private string TargetLang;
        private bool OnLine;
        public ML(string serviceName, string apiKey, string sourceLang, string targetLang,bool onLine)
        {
            this.ServiceName = serviceName;
            this.ApiKey = apiKey;
            this.SourceLang = sourceLang;
            this.TargetLang = targetLang;
            this.OnLine = onLine;
            this.conn = new SqlConnection();
            if (this.conn.State == ConnectionState.Open)
            {
                this.conn.Close();
            }
            string connectionString = ConfigurationManager.AppSettings["connString"];
            this.conn.ConnectionString = connectionString;
            this.conn.Open(); 
           for(int n=0;n<partition;n++)
           {
                string sql = " if not exists (select * from sysobjects where name='language" + n + "' and xtype='U') CREATE TABLE language" + n + "  ( md5 char(32) NOT NULL PRIMARY KEY, source varchar(2000) NOT NULL, target varchar(2000) NOT NULL )";
                SqlCommand comd = new SqlCommand(sql, this.conn);
                comd.ExecuteNonQuery();
            }
           if (!onLine)
           {
               updateXCWords();
           }
    }
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        Dictionary<string, object> wordsHash;
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
        private string SendRequest(string url, Hashtable parameters)
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
        private string GetFileInfo(string timestamp, string hash,
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
         * 访问file/info，从返回值中获取xc_words.json的地址
         */ 
        private string GetRequestAddress(string serviceName, string tarLang, string apiKey)
        {
            DateTime StartTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            DateTime NowTime = DateTime.Now;
            long UnixTime = (long)Math.Round((NowTime - StartTime).TotalMilliseconds, MidpointRounding.AwayFromZero);
            string TimeStamp = UnixTime.ToString();
            string pwd = TimeStamp + apiKey;
            string Hash = FormsAuthentication.HashPasswordForStoringInConfigFile(pwd, "MD5").ToLower();
            string FileinfoUrl = "http://i.xingcloud.com/api/v1/file/info";
            string Info = GetFileInfo(TimeStamp, Hash, FileinfoUrl, serviceName, tarLang, "xc_words.json");
            JavaScriptSerializer js = new JavaScriptSerializer();
            FileInfos Fileinfo = js.Deserialize<FileInfos>(Info);
            string RequestAddress = Fileinfo.data.request_address;
            return RequestAddress;
        }
        /*
         *访问文件地址，并获取文件内容
         */
        private string GetJson(string url)
        {
            StringBuilder stringBuilder=new StringBuilder() ;
            string rLine = string.Empty;
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
                    while(rLine!=null){
                        rLine = r.ReadLine();
                        stringBuilder.Append(rLine);
                    }
                }
            }
            return stringBuilder.ToString() ;
        }

        /*
         * 查询数据
         */
        private string queryString(string source) 
        {
                int flag = 0;
                string target = string.Empty;
                string md = FormsAuthentication.HashPasswordForStoringInConfigFile(source, "MD5").ToLower();
                int tableNum = Math.Abs(md.GetHashCode()) % partition;
                string tableName = DBtitle + tableNum;
                string sql = "select target from " + tableName + " where md5='" + md + "'";
                DataSet objDataSet = new DataSet();
                SqlCommand comd = new SqlCommand(sql, this.conn);
                SqlDataReader sqlReader = comd.ExecuteReader();
                while (sqlReader.Read())
                {
                    flag += 1;
                    target= sqlReader.GetValue(0).ToString();
                    sqlReader.Close();
                    return target;
                }
                if (flag == 0)
                {
                    sqlReader.Close();
                    return source;
                }
                return source;
        }

        /*
         * 向数据库表中插入数据
         */ 
        private void insert(string tableName,
                            string md5,
                            string source,
                            string target)
        {
            try
            {

                string sql = "if not exists (select * from " + tableName + " where md5='" + md5 + "') insert into "
                    + tableName + " values ('" + md5 + "','" + source + "','" + target + "')";
                SqlCommand comd = new SqlCommand(sql, this.conn);
                comd.ExecuteNonQuery();
            }catch(Exception e){
                Console.WriteLine(e.ToString());
            }

        }
         /*
          *访问Rest接口，添加字符串
          */ 
        private void AddString(string addString)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            DateTime nowTime = DateTime.Now;
            long unixTime = (long)Math.Round((nowTime - startTime).TotalMilliseconds, MidpointRounding.AwayFromZero);
            string timestamp = unixTime.ToString();
            string PWD = timestamp + this.ApiKey;
            string md = FormsAuthentication.HashPasswordForStoringInConfigFile(PWD, "MD5").ToLower();
            Hashtable stringAddParameters = new Hashtable();
            stringAddParameters.Add("service_name", this.ServiceName);
            stringAddParameters.Add("data", addString);
            stringAddParameters.Add("timestamp", timestamp);
            stringAddParameters.Add("hash", md);
            string stringAddUrl = "http://i.xingcloud.com/api/v1/string/add";
            SendRequest(stringAddUrl, stringAddParameters);
        }
           /*
            *将服务中的xc_words文件存入数据库
            */
        private void updateXCWords()
        {
            string requestAddress = GetRequestAddress(this.ServiceName, this.TargetLang, this.ApiKey);
            string json = GetJson(requestAddress);
            wordsHash = (Dictionary<string, object>)serializer.DeserializeObject(json);
                Dictionary<string, object>.KeyCollection keyColl = wordsHash.Keys;
                object value;
                foreach (string key in keyColl)
                {
                    wordsHash.TryGetValue(key, out value);
                    string md = FormsAuthentication.HashPasswordForStoringInConfigFile(key, "MD5").ToLower();
                    int tableNum = Math.Abs(md.GetHashCode()) % partition;
                    insert(DBtitle+tableNum, md,key, value.ToString());
                }
        }
        /*
         * 调用translateAPI
         */ 
        private string TranslateApi(string sourceLang,
                                    string targetLang,
                                    string queryString)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            DateTime nowTime = DateTime.Now;
            long unixTime = (long)Math.Round((nowTime - startTime).TotalMilliseconds, MidpointRounding.AwayFromZero);
            string timestamp = unixTime.ToString();
            string pwd = timestamp + this.ApiKey;
            string md5 = FormsAuthentication.HashPasswordForStoringInConfigFile(pwd, "MD5").ToLower();
            string TranslateUrl = "http://i.xingcloud.com/api/v1/string/translate";
            System.Net.WebClient webClient = new System.Net.WebClient();
            byte[] byteArray = webClient.DownloadData(TranslateUrl + "?timestamp=" + timestamp + "&hash=" + md5
                                        + "&service_name=" + this.ServiceName + "&source=" + sourceLang + "&target=" + targetLang
                                        + "&query=" + queryString);
            string QueryResult = Encoding.UTF8.GetString(byteArray);
            return QueryResult;
        }
        /*
         * 人工翻译，会将翻译的词条添加到人工管理界面
         * source:需要翻译的词条
         */
        public string Trans(string source)
        {
            string target = queryString(source);
            if (!this.OnLine)
            {
                if (target.Equals(source))
                {
                    AddString(source);
                }
            }
            return target;
        }
        /*
         *机器翻译，不会进行人工管理
         *source：需要翻译的词条
         */

        public string Translate( string source  )
        {
            if(!this.OnLine)
            {
                string result=string.Empty;
                try
                {
                 result = TranslateApi(this.SourceLang, this.TargetLang, source);
                }catch(Exception e){
                  Console.WriteLine(e.ToString());
                }
                string md = FormsAuthentication.HashPasswordForStoringInConfigFile(source, "MD5").ToLower();
                int tableNum = Math.Abs(md.GetHashCode()) % partition;
                insert(DBtitle + tableNum, md, source, result);
                return result;
            }else
            {
                 return queryString(source);
            }
        }

    }
}
