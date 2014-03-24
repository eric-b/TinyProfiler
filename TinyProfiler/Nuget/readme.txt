TinyProfiler :

JUST AFTER PACKAGE INSTALLATION : 
I recommend you to edit packages.config to add the attribute developmentDependency="true" :

<packages>
  <package id="TinyProfiler" version="1.0.1.0" targetFramework="net35" developmentDependency="true" />
</packages>

Thus, if you make a Nuget package with your project, it does not depend on that package (TinyProfiler is just a source code, no dll dependency).

