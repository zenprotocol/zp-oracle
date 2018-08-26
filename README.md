Zen Protocol Oracle Example 
============

An oracle service example.  
This example uses [Intrinio](http://www.intrinio.com/) as data source.  
See scripts/zen-oracle.service for systemd and environment-variable configuration.  

# Prerequisites

[Mono](http://www.mono-project.com/)  
[MongoDB](https://www.mongodb.com/) 

# Build

```
./paket restore
msbuild src
```

