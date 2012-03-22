ML CSharp SDK
=============

构造函数初始化：ML
--------------

ML obj = new ML("csharp-test001", "3edc1ecb56a44644023d747ec725ae37", "cn", "en");

通过该方法初始化ML。初始化后即可通过obj.trans()翻译词句(其中obj为初始化创建的多语言对象)。

#### 参数类型

* serviceName: String - 服务名称, 如 "my_ml_test"
* apiKey: String - 行云多语言管理系统分配的API密钥, 如 "21f...e35"
* srcLang: String - 原始语言, 如"cn"
* tarLang: String - 目标语言, 如"en", 如果与原始语言相同, 则不翻译直接原文返回
* queueNum: Integer - 本地缓存队列长度，默认是10
* cacheDir: String - 本地缓存目录地址，默认是当前文件目录下

#### 返回值

多语言服务的一个对象 object

#### 代码示例

	// 在应用的主类初始化函数中加入下面这行代码，如果与原始语言相同，则不翻译直接原文返回
	ML obj = new ML("csharp-test001", "3edc1ecb56a44644023d747ec725ae37", "cn", "en");
		
翻译词句：Trans()
-----------------

public string Trans(string words, string fileName, bool timelyTrans)

通过该方法直接翻译词句。

#### 参数类型

* source: String - 需要翻译的词句, 如 "游戏开始"
* fileName: String - 翻译词条被组织到那个文件中， 默认是"xc_words.json"
* timelyTrans: bool - 即时翻译标识，默认 false，如果为true，则立刻返回结果，但不保证精确，并且项目连表中不会存储

#### 返回值

String - 翻译好的词句, 如 "game start"

#### 代码示例

	// 示例
	string words001 = "中国";
	ML obj = new ML("csharp-test001", "3edc1ecb56a44644023d747ec725ae37", "cn", "en");
	string transWords001 = obj.Trans(words001);
	