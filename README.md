# README

**SharePoint.Integration** is a project designed to apply a repository pattern to storing data in SharePoint.

In an MVC application, this can be registered for dependancy injection in the following way:

    For(typeof (ISharePointRepository<>)).Use(typeof (SharePointRepository<>))
        .CtorDependency<string>("sharepointUrl").Is("<<SHAREPOINT_URL>>")
        .CtorDependency<string>("username").Is("<<USERNAME>>")
        .CtorDependency<string>("password").Is("<<PASSWORD>>");

License: MIT http://www.opensource.org/licenses/mit-license.php

Instructions: http://richerprogramming.richardpriddy.co.uk/search/label/SharePointRepository