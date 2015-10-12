ElmahR.Persistence.EntityFramework README
=========================================

This package installs an ElmahR persistor based on EntityFramework. In your
web.config you will have to provide the necessary connection string to an
empty destination database. A related connection string entry named
ErrorLogContext has been added. For the first usage, the user under which your 
web application is running should have right to create tables, this module
uses Entity Framework Migrations to transparently create the necessary
tables. After this step classical read/write rights will be enough.


Please refer to https://bitbucket.org/wasp/elmahr/wiki/Home for more details.