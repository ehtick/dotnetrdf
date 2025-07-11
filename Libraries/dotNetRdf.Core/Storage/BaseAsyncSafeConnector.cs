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
using System.Threading;
using System.Threading.Tasks;
using VDS.RDF.Storage.Management;

namespace VDS.RDF.Storage;

/// <summary>
/// Abstract Base Class for <see cref="IStorageProvider">IStorageProvider</see> implementations for which it is safe to do the <see cref="IAsyncStorageProvider">IAsyncStorageProvider</see> implementation simply by farming out calls to the synchronous methods onto background threads (i.e. non-HTTP based connectors).
/// </summary>
public abstract class BaseAsyncSafeConnector
    : IStorageProvider, IAsyncStorageProvider
{
    /// <summary>
    /// Gets the parent server (if any).
    /// </summary>
    public virtual IStorageServer ParentServer
    {
        get
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the parent server (if any).
    /// </summary>
    public virtual IAsyncStorageServer AsyncParentServer
    {
        get
        {
            return null;
        }
    }

    /// <summary>
    /// Get or set the URI factory for the connector to use.
    /// </summary>
    public virtual IUriFactory UriFactory { get; set; } = new CachingUriFactory();

    /// <summary>
    /// Loads a Graph from the Store.
    /// </summary>
    /// <param name="g">Graph to load into.</param>
    /// <param name="graphUri">URI of the Graph to load.</param>
    public abstract void LoadGraph(IGraph g, Uri graphUri);

    /// <summary>
    /// Loads a Graph from the Store.
    /// </summary>
    /// <param name="g">Graph to load into.</param>
    /// <param name="graphUri">URI of the Graph to load.</param>
    public abstract void LoadGraph(IGraph g, string graphUri);

    /// <summary>
    /// Loads a Graph from the Store.
    /// </summary>
    /// <param name="handler">Handler to load with.</param>
    /// <param name="graphUri">URI of the Graph to load.</param>
    public abstract void LoadGraph(IRdfHandler handler, Uri graphUri);

    /// <summary>
    /// Loads a Graph from the Store.
    /// </summary>
    /// <param name="handler">Handler to load with.</param>
    /// <param name="graphUri">URI of the Graph to load.</param>
    public abstract void LoadGraph(IRdfHandler handler, string graphUri);

    /// <summary>
    /// Saves a Graph to the Store.
    /// </summary>
    /// <param name="g">Graph to save.</param>
    public abstract void SaveGraph(IGraph g);

    /// <summary>
    /// Updates a Graph in the Store.
    /// </summary>
    /// <param name="graphName">Name of the Graph to update.</param>
    /// <param name="additions">Triples to be added.</param>
    /// <param name="removals">Triples to be removed.</param>
    public abstract void UpdateGraph(IRefNode graphName, IEnumerable<Triple> additions, IEnumerable<Triple> removals);

    /// <summary>
    /// Updates a Graph in the Store.
    /// </summary>
    /// <param name="graphUri">URI of the Graph to update.</param>
    /// <param name="additions">Triples to be added.</param>
    /// <param name="removals">Triples to be removed.</param>
    public abstract void UpdateGraph(Uri graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals);

    /// <summary>
    /// Updates a Graph in the Store.
    /// </summary>
    /// <param name="graphUri">URI of the Graph to update.</param>
    /// <param name="additions">Triples to be added.</param>
    /// <param name="removals">Triples to be removed.</param>
    public abstract void UpdateGraph(string graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals);

    /// <summary>
    /// Deletes a Graph from the Store.
    /// </summary>
    /// <param name="graphUri">URI of the Graph to delete.</param>
    public abstract void DeleteGraph(Uri graphUri);

    /// <summary>
    /// Deletes a Graph from the Store.
    /// </summary>
    /// <param name="graphUri">URI of the Graph to delete.</param>
    public abstract void DeleteGraph(string graphUri);

    /// <summary>
    /// Lists the Graphs in the Store.
    /// </summary>
    /// <returns></returns>
    [Obsolete("Replaced by ListGraphNames")]
    public abstract IEnumerable<Uri> ListGraphs();

    /// <summary>
    /// Gets an enumeration of the names of the graphs in the store.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// <para>
    /// Implementations should implement this method only if they need to provide a custom way of listing Graphs.  If the Store for which you are providing a manager can efficiently return the Graphs using a SELECT DISTINCT ?g WHERE { GRAPH ?g { ?s ?p ?o } } query then there should be no need to implement this function.
    /// </para>
    /// </remarks>
    public abstract IEnumerable<string> ListGraphNames();

    /// <summary>
    /// Indicates whether the Store is ready to accept requests.
    /// </summary>
    public abstract bool IsReady
    {
        get;
    }

    /// <summary>
    /// Gets whether the Store is read only.
    /// </summary>
    public abstract bool IsReadOnly
    {
        get;
    }

    /// <summary>
    /// Gets the IO Behaviour of the Store.
    /// </summary>
    public abstract IOBehaviour IOBehaviour
    {
        get;
    }

    /// <summary>
    /// Gets whether the Store supports Triple level updates via the <see cref="BaseAsyncSafeConnector.UpdateGraph(Uri, IEnumerable{Triple}, IEnumerable{Triple})">UpdateGraph()</see> method.
    /// </summary>
    public abstract bool UpdateSupported
    {
        get;
    }

    /// <summary>
    /// Gets whether the Store supports Graph deletion via the <see cref="BaseAsyncSafeConnector.DeleteGraph(Uri)">DeleteGraph()</see> method.
    /// </summary>
    public abstract bool DeleteSupported
    {
        get;
    }

    /// <summary>
    /// Gets whether the Store supports listing graphs via the <see cref="BaseAsyncSafeConnector.ListGraphs()">ListGraphs()</see> method.
    /// </summary>
    public abstract bool ListGraphsSupported
    {
        get;
    }

    /// <summary>
    /// Disposes of the Store.
    /// </summary>
    public abstract void Dispose();

    /// <summary>
    /// Loads a Graph from the Store asynchronously.
    /// </summary>
    /// <param name="g">Graph to load into.</param>
    /// <param name="graphUri">URI of the Graph to load.</param>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    public void LoadGraph(IGraph g, Uri graphUri, AsyncStorageCallback callback, object state)
    {
        this.AsyncLoadGraph(g, graphUri, callback, state);
    }

    /// <summary>
    /// Loads a Graph from the Store asynchronously.
    /// </summary>
    /// <param name="g">Graph to load into.</param>
    /// <param name="graphUri">URI of the Graph to load.</param>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    public void LoadGraph(IGraph g, string graphUri, AsyncStorageCallback callback, object state)
    {
        Uri u = string.IsNullOrEmpty(graphUri) ? null : g.UriFactory.Create(graphUri);
        LoadGraph(g, u, callback, state);
    }

    /// <summary>
    /// Loads a Graph from the Store asynchronously.
    /// </summary>
    /// <param name="handler">Handler to load with.</param>
    /// <param name="graphUri">URI of the Graph to load.</param>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    public void LoadGraph(IRdfHandler handler, Uri graphUri, AsyncStorageCallback callback, object state)
    {
        this.AsyncLoadGraph(handler, graphUri, callback, state);
    }

    /// <summary>
    /// Loads a Graph from the Store asynchronously.
    /// </summary>
    /// <param name="handler">Handler to load with.</param>
    /// <param name="graphUri">URI of the Graph to load.</param>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    public void LoadGraph(IRdfHandler handler, string graphUri, AsyncStorageCallback callback, object state)
    {
        Uri u = string.IsNullOrEmpty(graphUri) ? null : UriFactory.Create(graphUri);
        LoadGraph(handler, u, callback, state);
    }

    /// <inheritdoc />
    public Task LoadGraphAsync(IGraph g, string graphUri, CancellationToken cancellationToken)
    {
        return Task.Run(() => LoadGraph(g, graphUri), cancellationToken);
    }

    /// <inheritdoc />
    public Task LoadGraphAsync(IRdfHandler handler, string graphUri, CancellationToken cancellationToken)
    {
        return Task.Run(() => LoadGraph(handler, graphUri), cancellationToken);
    }

    /// <summary>
    /// Saves a Graph to the Store asynchronously.
    /// </summary>
    /// <param name="g">Graph to save.</param>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    public void SaveGraph(IGraph g, AsyncStorageCallback callback, object state)
    {
        this.AsyncSaveGraph(g, callback, state);
    }

    /// <inheritdoc />
    public Task SaveGraphAsync(IGraph g, CancellationToken cancellationToken)
    {
        return Task.Run(() => SaveGraph(g), cancellationToken);
    }

    /// <summary>
    /// Updates a Graph in the Store asynchronously.
    /// </summary>
    /// <param name="graphUri">URI of the Graph to update.</param>
    /// <param name="additions">Triples to be added.</param>
    /// <param name="removals">Triples to be removed.</param>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    public void UpdateGraph(Uri graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals, AsyncStorageCallback callback, object state)
    {
        this.AsyncUpdateGraph(graphUri, additions, removals, callback, state);
    }

    /// <summary>
    /// Updates a Graph in the Store asynchronously.
    /// </summary>
    /// <param name="graphUri">URI of the Graph to update.</param>
    /// <param name="additions">Triples to be added.</param>
    /// <param name="removals">Triples to be removed.</param>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    public void UpdateGraph(string graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals, AsyncStorageCallback callback, object state)
    {
        Uri u = string.IsNullOrEmpty(graphUri) ? null : UriFactory.Create(graphUri);
        UpdateGraph(u, additions, removals, callback, state);
    }

    /// <inheritdoc />
    public Task UpdateGraphAsync(string graphName, IEnumerable<Triple> additions, IEnumerable<Triple> removals,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => UpdateGraph(graphName, additions, removals), cancellationToken);
    }

    /// <summary>
    /// Deletes a Graph from the Store.
    /// </summary>
    /// <param name="graphUri">URI of the Graph to delete.</param>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    public void DeleteGraph(Uri graphUri, AsyncStorageCallback callback, object state)
    {
        this.AsyncDeleteGraph(graphUri, callback, state);
    }

    /// <summary>
    /// Deletes a Graph from the Store.
    /// </summary>
    /// <param name="graphUri">URI of the Graph to delete.</param>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    public void DeleteGraph(string graphUri, AsyncStorageCallback callback, object state)
    {
        Uri u = string.IsNullOrEmpty(graphUri) ? null : UriFactory.Create(graphUri);
        DeleteGraph(u, callback, state);
    }

    /// <inheritdoc />
    public Task DeleteGraphAsync(string graphName, CancellationToken cancellationToken)
    {
        return Task.Run(() => DeleteGraph(graphName), cancellationToken);
    }

    /// <summary>
    /// Lists the Graphs in the Store asynchronously.
    /// </summary>
    /// <param name="callback">Callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    [Obsolete("Replaced with ListGraphsAsync(CancellationToken)")]
    public void ListGraphs(AsyncStorageCallback callback, object state)
    {
        this.AsyncListGraphs(callback, state);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListGraphsAsync(CancellationToken cancellationToken)
    {
        IEnumerable<string> graphUris = await Task.Run(ListGraphNames, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return graphUris;
    }
}
