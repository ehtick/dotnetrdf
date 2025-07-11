/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using Xunit;

namespace VDS.RDF.Configuration;

public class DefaultTypeTests
{
    private void TestDefaultType(String typeUri, String expectedType)
    {
        var actualType = ConfigurationLoader.GetDefaultType(typeUri);
        Assert.Equal(expectedType, actualType);
    }

    [Fact]
    public void ConfigurationDefaultTypeGraph()
    {
        TestDefaultType(ConfigurationLoader.ClassGraph, ConfigurationLoader.DefaultTypeGraph);
    }

    [Fact]
    public void ConfigurationDefaultTypeGraphCollection()
    {
        TestDefaultType(ConfigurationLoader.ClassGraphCollection, ConfigurationLoader.DefaultTypeGraphCollection);
    }

    [Fact]
    public void ConfigurationDefaultTypeSparqlHttpProtocolProcessor()
    {
        TestDefaultType(ConfigurationLoader.ClassSparqlHttpProtocolProcessor, ConfigurationLoader.DefaultTypeSparqlHttpProtocolProcessor);
    }

    [Fact]
    public void ConfigurationDefaultTypeSparqlQueryProcessor()
    {
        TestDefaultType(ConfigurationLoader.ClassSparqlQueryProcessor, ConfigurationLoader.DefaultTypeSparqlQueryProcessor);
    }

    [Fact]
    public void ConfigurationDefaultTypeSparqlUpdateProcessor()
    {
        TestDefaultType(ConfigurationLoader.ClassSparqlUpdateProcessor, ConfigurationLoader.DefaultTypeSparqlUpdateProcessor);
    }

    [Fact]
    public void ConfigurationDefaultTypeTripleCollection()
    {
        TestDefaultType(ConfigurationLoader.ClassTripleCollection, ConfigurationLoader.DefaultTypeTripleCollection);
    }

    [Fact]
    public void ConfigurationDefaultTypeTripleStore()
    {
        TestDefaultType(ConfigurationLoader.ClassTripleStore, ConfigurationLoader.DefaultTypeTripleStore);
    }

    [Fact]
    public void ConfigurationDefaultTypeUser()
    {
        TestDefaultType(ConfigurationLoader.ClassUser, typeof(System.Net.NetworkCredential).AssemblyQualifiedName);
    }

    [Fact]
    public void ConfigurationDefaultTypeUserGroup()
    {
        TestDefaultType(ConfigurationLoader.ClassUserGroup, ConfigurationLoader.DefaultTypeUserGroup);
    }

    [Fact]
    public void ConfigurationDefaultTypeProxy()
    {
        TestDefaultType(ConfigurationLoader.ClassProxy, typeof(System.Net.WebProxy).AssemblyQualifiedName);
    }
}
