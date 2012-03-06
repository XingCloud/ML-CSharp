using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.XingCloud.ML;
using System.Configuration;

namespace sdktest
{
    class SdkTest
    {
        
        static void Main(string[] args)
        {
           ML ml= new ML();
           ml.Init("guomeng", "4f08eef37ce7ca30b5751f8531bea0dd", "cn", "en", true, true);/*加载服务，并将对应的json文件下载到本地，并将其中的数据加载到内存中*/
           Console.WriteLine(ml.trans("小日本"));
        }
        
    }
}
