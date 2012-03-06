ML C# SDK
=============

初始化：init()
--------------

 public void Init(string serviceName, string apiKey, string sourceLang, string targetLang, bool autoUpdateFile,bool autoAddTrans)

通过该方法初始化ML。初始化后即可通过ML.trans()翻译词句。

#### 参数类型

* serviceName: String, 服务名称, 如 "my_ml_test"
* apiKey: String, 行云多语言管理系统分配的API密钥, 如 "21f...e35"
* sourceLang: String, 原始语言, 如"cn"
* targetLang: String, 目标语言, 如"en", 如果与原始语言相同, 则不翻译直接原文返回
* autoUpdateFile: Boolean, 是否更新本地缓存文件
* autoAddTrans: Boolean, 是否自动添加未翻译词句到多语言服务器, 默认为true

#### 返回值

N/A

#### 代码示例

	{code}
	 在应用的主类初始化函数中加入下面这行代码，如果与原始语言相同，则不翻译直接原文返回
  ml.Init("guomeng", "4f08eef37ce7ca30b5751f8531bea0dd", "cn", "en", true, true);
	{code}


翻译词句：trans()
-----------------

public string trans(string source) 

通过该方法直接翻译词句。

#### 参数类型

* source: String, 需要翻译的词句, 如 "苹果"

#### 返回值

String, 翻译好的词句, 如 "Apple"

#### 代码示例

	{code}
 	 ml.trans("苹果")	
	{code}
