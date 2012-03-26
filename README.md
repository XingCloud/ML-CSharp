ML CSharp SDK
=============

构造函数初始化：ML
--------------

ML obj = new ML("csharp-test001", "3edc1ecb56a44644023d747ec725ae37", "cn", "en");

通过该方法初始化ML。初始化后即可通过obj.Trans() 或obj.Translate()翻译词条(其中obj为初始化创建的多语言对象)。

#### 参数类型

* serviceName: String - 服务名称, 如 "my_ml_test"
* apiKey: String - 行云多语言管理系统分配的API密钥, 如 "21f...e35"
* srcLang: String - 原始语言, 如"cn"
* tarLang: String - 目标语言, 如"en", 如果与原始语言相同, 则不翻译直接原文返回
* debug: bool - 是否支持错误日志写入，默认不写日志

#### 返回值

多语言服务的一个对象 object

#### 代码示例

	// 在应用的主类初始化函数中加入下面这行代码，如果与原始语言相同，则不翻译直接原文返回
	ML obj = new ML("csharp-test001", "3edc1ecb56a44644023d747ec725ae37", "cn", "en");
		
人工翻译：Trans()
-----------------

public string Trans(string words)

人工翻译词条，会将词条加到翻译平台，可进行词条管理。

#### 参数类型

* source: String - 需要翻译的词句, 如 "游戏开始"

#### 返回值

String - 翻译好的词句, 如 "game start"

#### 代码示例

	// 示例
	string words001 = "中国";
	ML obj = new ML("csharp-test001", "3edc1ecb56a44644023d747ec725ae37", "cn", "en");
	string transWords001 = obj.Trans(words001);
	
机器翻译： Translate()

public string Trans(string words)

机器翻译词条，可以及时得到翻译结果，支持长词条翻译。

#### 参数类型

* source: String - 需要翻译的词句, 如 "游戏开始"

#### 返回值

String - 翻译好的词句, 如 "game start"

#### 代码示例

	// 示例
	string words001 = "中国";
	ML obj = new ML("csharp-test001", "3edc1ecb56a44644023d747ec725ae37", "cn", "en");
	string transWords001 = obj.Translate(words001);