ElmahR.Appharbor README
=======================

This package allows you to integrate your target ElmahR dashboard
with Appharbor. You might have your source applications deployed
on Appharbor infrastucture, if that's the case you can have
service hooks called when a build happens. Through this package
you can have your ElmahR dashboard enabled to receive calls from
a build service hook, and thanks to this plugin your dashboard will
show real time messages about build events in monitored applications
properly configured. 

The only thing needed to complete the integration is to add a
configuration key called 'appharborId' to each application definition
related to sources hosted in Appharbor, and whose value must be the 
Appharbor application slug. This way you will have a mapping between 
the build events and your dasboard.