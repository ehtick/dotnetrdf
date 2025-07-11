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
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace VDS.RDF;

/// <summary>
/// A Thread Safe version of the <see cref="Graph">Graph</see> class.
/// </summary>
/// <threadsafety instance="true">Should be safe for almost any concurrent read and write access scenario, internally managed using a <see cref="ReaderWriterLockSlim">ReaderWriterLockSlim</see>.  If you encounter any sort of Threading/Concurrency issue please report to the. <a href="mailto:dotnetrdf-bugs@lists.sourceforge.net">dotNetRDF Bugs Mailing List</a></threadsafety>
/// <remarks>Performance will be marginally worse than a normal <see cref="Graph">Graph</see> but in multi-threaded scenarios this will likely be offset by the benefits of multi-threading.</remarks>
public class ThreadSafeGraph
    : Graph, IEquatable<ThreadSafeGraph>
{
    /// <summary>
    /// Locking Manager for the Graph.
    /// </summary>
    protected ReaderWriterLockSlim _lockManager = new(LockRecursionPolicy.SupportsRecursion);

    /// <summary>
    /// Creates a new Thread Safe Graph.
    /// </summary>
    public ThreadSafeGraph()
        : this(new ThreadSafeTripleCollection(new TreeIndexedTripleCollection(true))) { }

    /// <summary>
    /// Creates a new Thread Safe graph using the given Triple Collection.
    /// </summary>
    /// <param name="tripleCollection">Triple Collection.</param>
    public ThreadSafeGraph(BaseTripleCollection tripleCollection)
        : base(new ThreadSafeTripleCollection(tripleCollection)) { }

    /// <summary>
    /// Creates a new Thread Safe graph using the given name, factories and triple collection.
    /// </summary>
    /// <remarks>
    /// If <paramref name="tripleCollection"/> is not an instance of <see cref="ThreadSafeTripleCollection"/>, it will be wrapped in a <see cref="ThreadSafeTripleCollection"/>.
    /// This constructor is used by the <see cref="GraphFactory"/> class in the Configuration namespace.
    /// </remarks>
    /// <param name="name"></param>
    /// <param name="nodeFactory"></param>
    /// <param name="uriFactory"></param>
    /// <param name="tripleCollection"></param>
    /// <param name="emptyNamespaceMap"></param>
    public ThreadSafeGraph(
        IRefNode name, 
        INodeFactory nodeFactory = null,
        IUriFactory uriFactory = null, 
        BaseTripleCollection tripleCollection = null, 
        bool emptyNamespaceMap = false)
        :base(name, nodeFactory, uriFactory, 
            tripleCollection is ThreadSafeTripleCollection ? 
                tripleCollection : 
                tripleCollection != null ? 
                    new ThreadSafeTripleCollection(tripleCollection) : 
                    new ThreadSafeTripleCollection(new TreeIndexedTripleCollection(true)),
            emptyNamespaceMap)
    {
    }
    
    /// <summary>
    /// Creates a new Thread Safe graph using a Thread Safe triple collection.
    /// </summary>
    /// <param name="tripleCollection">Thread Safe triple collection.</param>
    public ThreadSafeGraph(ThreadSafeTripleCollection tripleCollection)
        : base(tripleCollection) { }

    /// <summary>
    /// Creates a new named thread-safe graph.
    /// </summary>
    /// <param name="name">The graph name.</param>
    public ThreadSafeGraph(IRefNode name):
        this(name, new ThreadSafeTripleCollection(new TreeIndexedTripleCollection(true))) { }

    /// <summary>
    /// Creates a new named thread-safe graph using the given triple collection.
    /// </summary>
    /// <param name="name">The graph name.</param>
    /// <param name="tripleCollection">The triple collection that the graph contains.</param>
    /// <remarks><paramref name="tripleCollection"/> will be wrapped as a <see cref="ThreadSafeTripleCollection"/> by this constructor.</remarks>
    public ThreadSafeGraph(IRefNode name, BaseTripleCollection tripleCollection)
        : this(name, new ThreadSafeTripleCollection(tripleCollection)){ }

    /// <summary>
    /// Creates a new named thread-safe graph using a thread-safe triple collection.
    /// </summary>
    /// <param name="name">The graph name.</param>
    /// <param name="tripleCollection">The thread-safe triple collection that the graph contains.</param>
    public ThreadSafeGraph(IRefNode name, ThreadSafeTripleCollection tripleCollection)
        :base(name, tripleCollection){ }

    /// <summary>
    /// Implements equality testing between <see cref="ThreadSafeGraph"/> instances.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(ThreadSafeGraph other)
    {
        return Equals((IGraph)other);
    }

    #region Triple Assertion and Retraction

    /// <summary>
    /// Asserts a Triple in the Graph.
    /// </summary>
    /// <param name="t">The Triple to add to the Graph.</param>
    public override bool Assert(Triple t)
    {
        try
        {
            _lockManager.EnterWriteLock();
            return base.Assert(t);
        }
        finally
        {
            _lockManager.ExitWriteLock();
        }
    }

    /// <summary>
    /// Asserts a List of Triples in the graph.
    /// </summary>
    /// <param name="ts">List of Triples in the form of an IEnumerable.</param>
    public override bool Assert(IEnumerable<Triple> ts)
    {
        try
        {
            _lockManager.EnterWriteLock();
            return base.Assert(ts);
        }
        finally
        {
            _lockManager.ExitWriteLock();
        }
    }

    /// <summary>
    /// Retracts a Triple from the Graph.
    /// </summary>
    /// <param name="t">Triple to Retract.</param>
    /// <remarks>Current implementation may have some defunct Nodes left in the Graph as only the Triple is retracted.</remarks>
    public override bool Retract(Triple t)
    {
        try
        {
            _lockManager.EnterWriteLock();
            return base.Retract(t);
        }
        finally
        {
            _lockManager.ExitWriteLock();
        }
    }

    /// <summary>
    /// Retracts a enumeration of Triples from the graph.
    /// </summary>
    /// <param name="ts">Enumeration of Triples to retract.</param>
    public override bool Retract(IEnumerable<Triple> ts)
    {
        try
        {
            _lockManager.EnterWriteLock();
            return base.Retract(ts);
        }
        finally
        {
            _lockManager.ExitWriteLock();
        }
    }

    #endregion

    /// <summary>
    /// Creates a new Blank Node ID and returns it.
    /// </summary>
    /// <returns></returns>
    public override string GetNextBlankNodeID()
    {
        var id = string.Empty;
        try 
        {
            _lockManager.EnterWriteLock();
            id = base.GetNextBlankNodeID();
        }
        finally 
        {
            _lockManager.ExitWriteLock();
            if (id.Equals(string.Empty))
            {
                throw new RdfException("Unable to generate a new Blank Node ID due to a Threading issue");
            }
        }
        return id;
    }

    #region IDisposable Members
    private bool _isDisposed;
    /// <summary>
    /// Disposes of a Graph.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            if (disposing)
            {
                _lockManager.Dispose();
            }
        }
        base.Dispose();
    }
    #endregion

    #region Node Selection

    /// <summary>
    /// Returns the Blank Node with the given Identifier.
    /// </summary>
    /// <param name="nodeId">The Identifier of the Blank Node to select.</param>
    /// <returns>Either the Blank Node or null if no Node with the given Identifier exists.</returns>
    public override IBlankNode GetBlankNode(string nodeId)
    {
        IBlankNode b;
        try
        {
            _lockManager.EnterReadLock();
            b = base.GetBlankNode(nodeId);
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return b;
    }

    /// <summary>
    /// Returns the LiteralNode with the given Value if it exists.
    /// </summary>
    /// <param name="literal">The literal value of the Node to select.</param>
    /// <returns>Either the LiteralNode Or null if no Node with the given Value exists.</returns>
    /// <remarks>The LiteralNode in the Graph must have no Language or DataType set.</remarks>
    public override ILiteralNode GetLiteralNode(string literal)
    {
        ILiteralNode l;
        try
        {
            _lockManager.EnterReadLock();
            l = base.GetLiteralNode(literal);
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return l;
    }

    /// <summary>
    /// Returns the LiteralNode with the given Value in the given Language if it exists.
    /// </summary>
    /// <param name="literal">The literal value of the Node to select.</param>
    /// <param name="langspec">The Language Specifier for the Node to select.</param>
    /// <returns>Either the LiteralNode Or null if no Node with the given Value and Language Specifier exists.</returns>
    public override ILiteralNode GetLiteralNode(string literal, string langspec)
    {
        ILiteralNode l;
        try
        {
            _lockManager.EnterReadLock();
            l = base.GetLiteralNode(literal, langspec);
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return l;
    }

    /// <summary>
    /// Returns the LiteralNode with the given Value and given Data Type if it exists.
    /// </summary>
    /// <param name="literal">The literal value of the Node to select.</param>
    /// <param name="datatype">The Uri for the Data Type of the Literal to select.</param>
    /// <returns>Either the LiteralNode Or null if no Node with the given Value and Data Type exists.</returns>
    public override ILiteralNode GetLiteralNode(string literal, Uri datatype)
    {
        ILiteralNode l;
        try
        {
            _lockManager.EnterReadLock();
            l = base.GetLiteralNode(literal, datatype);
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return l;
    }

    /// <summary>
    /// Returns the UriNode with the given QName if it exists.
    /// </summary>
    /// <param name="qname">The QName of the Node to select.</param>
    /// <returns></returns>
    public override IUriNode GetUriNode(string qname)
    {
        IUriNode u;
        try
        {
            _lockManager.EnterReadLock();
            u = base.GetUriNode(qname);
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return u;
    }

    /// <summary>
    /// Returns the UriNode with the given Uri if it exists.
    /// </summary>
    /// <param name="uri">The Uri of the Node to select.</param>
    /// <returns>Either the UriNode Or null if no Node with the given Uri exists.</returns>
    public override IUriNode GetUriNode(Uri uri)
    {
        IUriNode u;
        try
        {
            _lockManager.EnterReadLock();
            u = base.GetUriNode(uri);
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return u;
    }

    #endregion

    #region Triple Selection

    /// <summary>
    /// Gets all the Triples involving the given Node.
    /// </summary>
    /// <param name="n">The Node to find Triples involving.</param>
    /// <returns>Zero/More Triples.</returns>
    public override IEnumerable<Triple> GetTriples(INode n)
    {
        List<Triple> triples;
        try
        {
            _lockManager.EnterReadLock();
            triples = base.GetTriples(n).ToList();
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return triples;
    }

    /// <summary>
    /// Gets all the Triples involving the given Uri.
    /// </summary>
    /// <param name="uri">The Uri to find Triples involving.</param>
    /// <returns>Zero/More Triples.</returns>
    public override IEnumerable<Triple> GetTriples(Uri uri)
    {
        List<Triple> triples;
        try
        {
            _lockManager.EnterReadLock();
            triples = base.GetTriples(uri).ToList();
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return triples;
    }

    /// <summary>
    /// Gets all the Triples with the given Node as the Object.
    /// </summary>
    /// <param name="n">The Node to find Triples with it as the Object.</param>
    /// <returns></returns>
    public override IEnumerable<Triple> GetTriplesWithObject(INode n)
    {
        List<Triple> triples;
        try
        {
            _lockManager.EnterReadLock();
            triples = base.GetTriplesWithObject(n).ToList();
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return triples;
    }

    /// <summary>
    /// Gets all the Triples with the given Uri as the Object.
    /// </summary>
    /// <param name="u">The Uri to find Triples with it as the Object.</param>
    /// <returns>Zero/More Triples.</returns>
    public override IEnumerable<Triple> GetTriplesWithObject(Uri u)
    {
        List<Triple> triples;
        try
        {
            _lockManager.EnterReadLock();
            triples = base.GetTriplesWithObject(u).ToList();
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return triples;
    }

    /// <summary>
    /// Gets all the Triples with the given Node as the Predicate.
    /// </summary>
    /// <param name="n">The Node to find Triples with it as the Predicate.</param>
    /// <returns></returns>
    public override IEnumerable<Triple> GetTriplesWithPredicate(INode n)
    {
        List<Triple> triples;
        try
        {
            _lockManager.EnterReadLock();
            triples = base.GetTriplesWithPredicate(n).ToList();
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return triples;
    }

    /// <summary>
    /// Gets all the Triples with the given Uri as the Predicate.
    /// </summary>
    /// <param name="u">The Uri to find Triples with it as the Predicate.</param>
    /// <returns>Zero/More Triples.</returns>
    public override IEnumerable<Triple> GetTriplesWithPredicate(Uri u)
    {
        List<Triple> triples;
        try
        {
            _lockManager.EnterReadLock();
            triples = base.GetTriplesWithPredicate(u).ToList();
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return triples;
    }

    /// <summary>
    /// Gets all the Triples with the given Node as the Subject.
    /// </summary>
    /// <param name="n">The Node to find Triples with it as the Subject.</param>
    /// <returns>Zero/More Triples.</returns>
    public override IEnumerable<Triple> GetTriplesWithSubject(INode n)
    {
        List<Triple> triples;
        try
        {
            _lockManager.EnterReadLock();
            triples = base.GetTriplesWithSubject(n).ToList();
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return triples;
    }

    /// <summary>
    /// Gets all the Triples with the given Uri as the Subject.
    /// </summary>
    /// <param name="u">The Uri to find Triples with it as the Subject.</param>
    /// <returns>Zero/More Triples.</returns>
    public override IEnumerable<Triple> GetTriplesWithSubject(Uri u)
    {
        List<Triple> triples;
        try
        {
            _lockManager.EnterReadLock();
            triples = base.GetTriplesWithSubject(u).ToList();
        }
        finally
        {
            _lockManager.ExitReadLock();
        }
        return triples;
    }

    #endregion
}
