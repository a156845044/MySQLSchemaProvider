Instructions
------------

* Make sure you have MySQL Connector/Net 1.0.7 or later installed 
  (http://dev.mysql.com/downloads/connector/net/1.0.html)

* Make sure you have the latest version of CodeSmith. You may be
  able to use this Schema provider on earlier versions of
  CodeSmith 3.xx, but you will need to recompile the provider.

* Make sure CodeSmith is not running

* Extract and copy the SchemaExplorer.MySQLSchemaProvider.dll 
  assembly to /Program Files/CodeSmith/v3.0/SchemaProviders 
  (or wherever you have CodeSmith installed)
  
* Use the following format for your connection string

  Server=localhost;Port=3306;Database=databaseName;Uid=userName;Pwd=userPassword
  

Release Notes
-------------

Release 0.9.2 : 2006-02-17

* Updated GetTableKeys() to provide not only the foreign keys for the table 
  but also the foreign keys of other tables that point to the given table.
  Updated code graciously provided by Douglas R. Steen.  Thanks!
* Added .NET 2.0 version for CodeSmith 3.2.5 or higher (unless recompiled)
* .NET 1.1 version requires CodeSmith 3.1.6 (unless recompiled)
  
Release 0.9.1 : 2005-12-29

* Added support for Extended Properties CS_IsIdentity
* Bug fix for GetTableData()
* Requires CodeSmith v3.1.4

Release 0.9.0 : 2005-11-27

* Initial Release
  
  