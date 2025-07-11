/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
// 
// Copyright (c) 2009-2025 dotNetRDF Project (http://dotnetrdf.org/)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
*/

using System;
using System.Security.Cryptography;
using System.Linq;
using System.Reflection;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.FullText;
using VDS.RDF.Query.FullText.Schema;
using VDS.RDF.Query.FullText.Search;
using Lucene.Net.Index;

namespace VDS.RDF.Query;

static class FullTextExtensions
{
    private static readonly NodeFactory NodeFactory = new(new NodeFactoryOptions());
    private static SHA256Managed _sha256;

    internal static ISet ToSet(this IFullTextSearchResult result, String matchVar, String scoreVar)
    {
        var s = new Set();
        if (matchVar != null) s.Add(matchVar, result.Node);
        if (scoreVar != null) s.Add(scoreVar, result.Score.ToLiteral(NodeFactory));
        return s;
    }

    internal static IFullTextSearchResult ToResult(this Document doc, double score, IFullTextIndexSchema schema)
    {
        //First get the node type
        IIndexableField nodeTypeField = doc.GetField(schema.NodeTypeField);
        if (nodeTypeField == null) throw new RdfQueryException("Node Type field " + schema.NodeTypeField + " not present on a retrieved document.  Please check you have configured the Index Schema correctly");
        NodeType nodeType;
        try 
        {
            nodeType = (NodeType)Enum.Parse(typeof(NodeType), nodeTypeField.GetStringValue());
        } 
        catch 
        {
            throw new RdfQueryException("Node Type field " + schema.NodeTypeField + " contained an invalid value '" + nodeTypeField.GetStringValue() + "'.  Please check you have configured the Index Schema correctly");
        }

        //Get the Graph
        IIndexableField graphField = doc.GetField(schema.GraphField);
        IRefNode graphName = graphField == null ? null :
            graphField.GetStringValue().StartsWith("_:") ? 
            NodeFactory.CreateBlankNode(graphField.GetStringValue()) :
            NodeFactory.CreateUriNode(NodeFactory.UriFactory.Create(graphField.GetStringValue()));

        //Then get the node value
        IIndexableField nodeValueField = doc.GetField(schema.NodeValueField);
        if (nodeValueField == null) throw new RdfQueryException("Node Value field " + schema.NodeValueField + " not present on a retrieved document.  Please check you have configured the Index Schema correctly");
        var nodeValue = nodeValueField.GetStringValue();

        //Then depending on the Node Type determine whether we need to obtain the Meta Field as well
        switch (nodeType)
        {
            case NodeType.Blank:
                //Can just create a Blank Node
                return new FullTextSearchResult(graphName, NodeFactory.CreateBlankNode(nodeValue), score);

            case NodeType.Literal:
                //Need to get Meta field to determine whether we have a language or datatype present
                IIndexableField nodeMetaField = doc.GetField(schema.NodeMetaField);
                if (nodeMetaField == null)
                {
                    //Assume a Plain Literal
                    return new FullTextSearchResult(graphName, NodeFactory.CreateLiteralNode(nodeValue), score);
                }
                else
                {
                    var nodeMeta = nodeMetaField.GetStringValue();
                    if (nodeMeta.StartsWith("@"))
                    {
                        //Language Specified literal
                        return new FullTextSearchResult(graphName, NodeFactory.CreateLiteralNode(nodeValue, nodeMeta.Substring(1)), score);
                    }
                    else
                    {
                        //Assume a Datatyped literal
                        return new FullTextSearchResult(graphName, NodeFactory.CreateLiteralNode(nodeValue, NodeFactory.UriFactory.Create(nodeMeta)), score);
                    }
                }

            case NodeType.Uri:
                //Can just create a URI Node
                return new FullTextSearchResult(graphName, NodeFactory.CreateUriNode(NodeFactory.UriFactory.Create(nodeValue)), score);

            default:
                throw new RdfQueryException("Only Blank, Literal and URI Nodes may be retrieved from a Lucene Document");
        }
    }

    internal static String ToLuceneFieldValue(this NodeType type)
    {
        return ((int)type).ToString();
    }

    internal static String ToLuceneFieldValue(this INode n)
    {
        switch (n.NodeType)
        {
            case NodeType.Blank:
                return ((IBlankNode)n).InternalID;
            case NodeType.Literal:
                return ((ILiteralNode)n).Value;
            case NodeType.Uri:
                return n.ToString();
            default:
                throw new FullTextIndexException("Only Blank, Literal and URI Nodes may be indexed using Lucene");
        }
    }

    internal static String ToLuceneFieldMeta(this INode n)
    {
        switch (n.NodeType)
        {
            case NodeType.Blank:
            case NodeType.Uri:
                return null;

            case NodeType.Literal:
                var lit = (ILiteralNode)n;
                if (lit.DataType != null)
                {
                    return lit.DataType.ToString();
                }
                else if (!lit.Language.Equals(String.Empty))
                {
                    return "@" + lit.Language;
                }
                else
                {
                    return null;
                }

            default:
                throw new FullTextIndexException("Only Blank, Literal and URI Nodes may be indexed using Lucene");
        }
    }

    /// <summary>
    /// Gets a SHA256 Hash for a String.
    /// </summary>
    /// <param name="s">String to hash.</param>
    /// <returns></returns>
    internal static String GetSha256Hash(this String s)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));

        //Only instantiate the SHA256 class when we first use it
        _sha256 ??= new SHA256Managed();

        var input = Encoding.UTF8.GetBytes(s);
        var output = _sha256.ComputeHash(input);

        var hash = new StringBuilder();
        foreach (var b in output)
        {
            hash.Append(b.ToString("x2"));
        }

        return hash.ToString();
    }

    internal static void SerializeConfiguration(this Directory directory, ConfigurationSerializationContext context)
    {
        context.EnsureObjectFactory(typeof(FullTextObjectFactory));

        INode rdfType = context.Graph.CreateUriNode(context.UriFactory.Create(RdfSpecsHelper.RdfType));
        INode dnrType = context.Graph.CreateUriNode(context.UriFactory.Create(ConfigurationLoader.PropertyType));
        INode indexClass = context.Graph.CreateUriNode(context.UriFactory.Create(FullTextHelper.ClassIndex));
        INode dirObj = context.NextSubject;

        context.Graph.Assert(dirObj, rdfType, indexClass);
        context.Graph.Assert(dirObj, context.Graph.CreateUriNode(context.UriFactory.Create(FullTextHelper.PropertyEnsureIndex)), (true).ToLiteral(context.Graph));
        if (directory is RAMDirectory)
        {
            context.Graph.Assert(dirObj, dnrType, context.Graph.CreateLiteralNode(directory.GetType().FullName + ", Lucene.Net"));
        }
        else if (directory is FSDirectory fsDirectory)
        {
            context.Graph.Assert(dirObj, dnrType, context.Graph.CreateLiteralNode(typeof(FSDirectory).FullName + ", Lucene.Net"));
            context.Graph.Assert(dirObj, context.Graph.CreateUriNode(context.UriFactory.Create(ConfigurationLoader.PropertyFromFile)), context.Graph.CreateLiteralNode(fsDirectory.Directory.FullName));
        }
        else
        {
            throw new DotNetRdfConfigurationException("dotNetRDF.Query.FullText only supports automatically serializing configuration for Lucene indexes that use RAMDirectory or FSDirectory currently");
        }
    }

    internal static void SerializeConfiguration(this Analyzer analyzer, ConfigurationSerializationContext context)
    {
        context.EnsureObjectFactory(typeof(FullTextObjectFactory));

        INode rdfType = context.Graph.CreateUriNode(context.UriFactory.Create(RdfSpecsHelper.RdfType));
        INode dnrType = context.Graph.CreateUriNode(context.UriFactory.Create(ConfigurationLoader.PropertyType));
        INode analyzerClass = context.Graph.CreateUriNode(context.UriFactory.Create(FullTextHelper.ClassAnalyzer));
        INode analyzerObj = context.NextSubject;

        Type t = analyzer.GetType();
        if (t.GetConstructor(Type.EmptyTypes) != null || t.GetConstructor(new[] { typeof(Lucene.Net.Util.LuceneVersion) }) != null)
        {
            context.Graph.Assert(analyzerObj, rdfType, analyzerClass);
            context.Graph.Assert(analyzerObj, dnrType, context.Graph.CreateLiteralNode(t.AssemblyQualifiedName));
        }
        else
        {
            throw new DotNetRdfConfigurationException("dotNetRDF.Query.FullText only supports automatically serializing configuration for Lucene analyzers that have an unparameterised constructor or a constructor that takes a Version parameter");
        }
    }

    /// <summary>
    /// Gets either the String form of the Object of the Empty String.
    /// </summary>
    /// <param name="obj">Object.</param>
    /// <returns>Result of calling <strong>ToString()</strong> on non-null objects and the empty string for null objects.</returns>
    internal static String ToSafeString(this Object obj)
    {
        return (obj != null ? obj.ToString() : String.Empty);
    }

    /// <summary>
    /// Gets either the String form of the URI of the Empty String.
    /// </summary>
    /// <param name="u">URI.</param>
    /// <returns>Result of calling <strong>AbsoluteUri</strong> on non-null URIs and the empty string for null URIs.</returns>
    internal static String ToSafeString(this Uri u)
    {
        return (u != null ? u.AbsoluteUri : String.Empty);
    }

    /// <summary>
    /// Ensures that a specific Object Factory type is registered in a Configuration Graph.
    /// </summary>
    /// <param name="context">Configuration Serialization Context.</param>
    /// <param name="factoryType">Factory Type.</param>
    internal static void EnsureObjectFactory(this ConfigurationSerializationContext context, Type factoryType)
    {
        INode dnrType = context.Graph.CreateUriNode(context.UriFactory.Create(ConfigurationLoader.PropertyType));
        INode rdfType = context.Graph.CreateUriNode(context.UriFactory.Create(RdfSpecsHelper.RdfType));
        var assembly = Assembly.GetAssembly(factoryType).FullName;
        if (assembly.Contains(',')) assembly = assembly.Substring(0, assembly.IndexOf(','));

        //Firstly need to ensure our object factory has been referenced
        var factoryCheck = new SparqlParameterizedString();
        factoryCheck.Namespaces.AddNamespace("dnr", context.UriFactory.Create(ConfigurationLoader.ConfigurationNamespace));
        factoryCheck.CommandText = "ASK WHERE { ?factory a dnr:ObjectFactory ; dnr:type '" + factoryType.FullName + ", " + assembly + "' . }";
        if (context.Graph.ExecuteQuery(factoryCheck) is SparqlResultSet { Result: false })
        {
            INode factory = context.Graph.CreateBlankNode();
            context.Graph.Assert(new Triple(factory, rdfType, context.Graph.CreateUriNode(context.UriFactory.Create(ConfigurationLoader.ClassObjectFactory))));
            context.Graph.Assert(new Triple(factory, dnrType, context.Graph.CreateLiteralNode(factoryType.FullName + ", " + assembly)));
        }
    }
}
