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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VDS.RDF.Dynamic;

public partial class DynamicNode
{
    /// <summary>
    /// Gets a collection of <see cref="DynamicObjectCollection">dynamic object collections</see>, one per distinct outgoing predicate from this node.
    /// </summary>
    public ICollection<object> Values
    {
        get
        {
            return NodePairs.Values;
        }
    }

    /// <summary>
    /// Gets the number of distinct outgoing predicates from this node.
    /// </summary>
    public int Count
    {
        get
        {
            return PredicateNodes.Count();
        }
    }

    /// <summary>
    /// Gets a value indicating whether this node is read only (always false).
    /// </summary>
    public bool IsReadOnly
    {
        get
        {
            return false;
        }
    }

    /// <summary>
    /// Retracts statements with this subject.
    /// </summary>
    public void Clear()
    {
        new DynamicGraph(Graph).Remove(this);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.Cast<KeyValuePair<INode, object>>().GetEnumerator();
    }
}
