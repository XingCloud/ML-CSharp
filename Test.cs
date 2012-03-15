using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Com.XingCloud.ML;
using System.Net;
using System.Threading;

namespace DBtest
{
    class Test
    {
        static void Main(string[] args)
        {
         
            ML ml = new ML("sdktest", "d867078150e2ca422539ba1482eb01a6","cn","en",true);//声明ML对象，并初始化服务名字，服务的apikey
            Console.WriteLine(ml.Trans("苹果"));//翻译词条（人工翻译）    
            Console.WriteLine( ml.Translate("电脑"));//翻译词条（机器翻译）
   
        }
    }
}
