ML C# SDK
=============

添加app.config文件
--------------

在configuration节点下修改相应的数据库信息。

  \<appSettings\><br>
  \<add key="connString" value="Server=10.1.4.235;Database=xingcloud; User ID=sa;Password=guomeng2012XINGCLOUD" /\><br>
  \</appSettings\><br>	


声明ML对象
--------------

#### 参数类型

* serviceName: String, 服务名称, 如 "my_ml_test"
* apiKey: String, 行云多语言管理系统分配的API密钥, 如 "21f...e35"
* sourceLang: String, 原始语言, 如"cn"
* targetLang: String, 目标语言, 如"en", 如果与原始语言相同, 则不翻译直接原文返回
* autoUpdateFile: Boolean, 是否更新本地缓存文件
* bool onLine: 是否为线上，如果为false的话则会去读取服务上xc_words.json中的数据，并插入到本地数据库中。

#### 代码示例

	ML ml = new ML("sdktest", "d867078150e2ca422539ba1482eb01a6","cn","en",true);


翻译词句（人工翻译）：Trans()
-----------------

public string trans(string source) 

通过该方法直接翻译词句。

#### 参数类型

* source: String, 需要翻译的词句, 如 "苹果"

#### 返回值

String, 翻译好的词句, 如 "Apple"

#### 代码示例

 	 ml.trans("苹果")	

翻译词句（机器翻译）：Translate()
-----------------

public string Translate(string source) 

通过该方法直接翻译词句。

#### 参数类型

* source: String, 需要翻译的词句, 如 "江山"

#### 返回值

String, 翻译好的词句, 如 "Country"

#### 代码示例

 	 ml.translate("江山")	
