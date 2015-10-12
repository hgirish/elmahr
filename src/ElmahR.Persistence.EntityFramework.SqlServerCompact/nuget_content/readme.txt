ElmahR.Persistence.EntityFramework.SqlServerCompact README
==========================================================

This modules installs the necessary dependencies to have EntityFramework
work with a SqlServerCompact local database (.sdf). Errors will be 
stored inside a local ElmahR.sdf database located in the App_Data folder.

IMPORTANT: it's likely that you'll have 2 connection strings
defined in your web.config for ErrorLogContext, one added by the underlying
ElmahR.Persistence.EntityFramework package, and one added by this one:
please remove the generic one and keep the one referring to ElmahR.sdf.


Please refer to https://bitbucket.org/wasp/elmahr/wiki/Home for more details.