# BaGetter

fork自[[BaGetter](https://github.com/bagetter/BaGetter)]
BaGetter is a lightweight [NuGet] and [symbol] server, written in C#.
![image](https://github.com/user-attachments/assets/8eabbcde-48c5-4777-8e6b-1c9062d8166d)


## 📦 功能

* 增加download package : 手动下载指定版本的包
* 修改v3/package/{id}/{version}/{idVersion}.nupkg 接口 : 支持自动下载本地存储中不存在的包
* 后续计划 : 修改import命令，[Bagetter](https://nugetprod0.blob.core.windows.net/ng-search-data/downloads.v1.json)源码中下载包地址已不可用（PublicAccessNotPermitted），改为解析
[Nuget API](https://learn.microsoft.com/en-us/nuget/api/search-query-service-resource)





