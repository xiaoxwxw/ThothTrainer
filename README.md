# ThothTrainer
WPF Client of Thoth training interface

[![Language: C#](https://img.shields.io/badge/language-C%23-994fd7.svg?style=flat)](https://github.com/CognitiveBuild/ThothTrainer)
[![.NET Framework 4.5](https://img.shields.io/badge/.net framework-4.5-994fd7.svg?style=flat)](https://github.com/CognitiveBuild/ThothTrainer)
[![GitHub license](https://img.shields.io/badge/license-Apache%202-blue.svg)](https://raw.githubusercontent.com/CognitiveBuild/Chatbot/master/LICENSE)

###Image training
![UI](https://cloud.githubusercontent.com/assets/1511528/18657050/2afc9ea4-7f29-11e6-9c81-6f5c4d0b2356.png)

###Real-time detecting
![UI](https://cloud.githubusercontent.com/assets/1511528/18657203/8cdbcc02-7f2a-11e6-87d9-9e554c190e5e.png)

#Platform
* Windows 7+ only

#Prerequisite
* Download and install MySQL community
* Download and install Microsoft Visual Studio 2015 community

#Installation guide
* Create MySQL database and tables
```sql

--
-- Schema `thoth`
--
DROP SCHEMA IF EXISTS `thoth`;
CREATE SCHEMA `thoth`;

--
-- Table structure for table `faces`
--

DROP TABLE IF EXISTS `faces`;

CREATE TABLE `faces` (
  `identity` int(11) NOT NULL,
  `face` longblob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;

CREATE TABLE `users` (
  `identity` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(128) NOT NULL,
  `email` varchar(128) DEFAULT NULL,
  `introduction` varchar(144) DEFAULT NULL,
  `ispresenter` char(1) DEFAULT 'N',
  `idinterest` int(11) DEFAULT '0',
  `facebook` varchar(256) DEFAULT NULL,
  `twitter` varchar(256) DEFAULT NULL,
  `linkedin` varchar(256) DEFAULT NULL,
  `datecreated` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`identity`),
  UNIQUE KEY `id_UNIQUE` (`identity`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;

```
* Update `App.config` with database environment settings, they are `your_ip_address`, `your_uid` and `your_password`
```xml
  <appSettings>
    <add key="LOCAL_FACES_DATABASE" value="server=your_ip_address; uid=your_uid; pwd=your_password; database=thoth; charset=utf8"/>
  </appSettings>
```

#License
Copyright 2016 GCG GBS CTO Office under [the Apache 2.0 license](LICENSE).
