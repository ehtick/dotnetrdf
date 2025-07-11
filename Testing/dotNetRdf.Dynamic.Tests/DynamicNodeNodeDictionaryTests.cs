﻿/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2013 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VDS.RDF;
using Xunit;

namespace VDS.RDF.Dynamic;

public class DynamicNodeNodeDictionaryTests
{
    [Fact]
    public void Get_index_requires_predicate()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.Throws<ArgumentNullException>(() =>
            d[null as INode]);
    }

    [Fact]
    public void Get_index_returns_dynamic_objects()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = g.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, g);

        var actual = d[p];

        var objects = Assert.IsType<DynamicObjectCollection>(actual);
        var @object = Assert.Single(objects);
        Assert.Equal(o, @object);
    }

    [Fact]
    public void Set_index_requires_predicate()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.Throws<ArgumentNullException>(() =>
            d[null as INode] = null);
    }

    [Fact]
    public void Set_index_removes_predicate()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s1> <urn:p2> ""o3"" .
<urn:s2> <urn:s1> ""o6"" .
<urn:s2> <urn:p3> <urn:s1> .
");

        var actual = new Graph();
        actual.LoadFromString(@"
<urn:s1> <urn:p1> ""o1"" .
<urn:s1> <urn:p1> ""o2"" .
<urn:s1> <urn:p2> ""o3"" .
<urn:s2> <urn:s1> ""o6"" .
<urn:s2> <urn:p3> <urn:s1> .
");

        var s = actual.CreateUriNode(UriFactory.Root.Create("urn:s1"));
        var p = actual.CreateUriNode(UriFactory.Root.Create("urn:p1"));
        var d = new DynamicNode(s, actual);

        d[p] = null;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Set_index_adds_predicate_objects()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s1> <urn:p1> ""o"" .
<urn:s1> <urn:p2> ""o3"" .
<urn:s2> <urn:s1> ""o6"" .
<urn:s2> <urn:p3> <urn:s1> .
");

        var actual = new Graph();
        actual.LoadFromString(@"
<urn:s1> <urn:p1> ""o1"" .
<urn:s1> <urn:p1> ""o2"" .
<urn:s1> <urn:p2> ""o3"" .
<urn:s2> <urn:s1> ""o6"" .
<urn:s2> <urn:p3> <urn:s1> .
");

        var s = actual.CreateUriNode(UriFactory.Root.Create("urn:s1"));
        var p = actual.CreateUriNode(UriFactory.Root.Create("urn:p1"));
        var d = new DynamicNode(s, actual);

        d[p] = "o";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Keys_are_predicates()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s1> <urn:p1> <urn:o1> .
<urn:s1> <urn:p1> <urn:o2> .
<urn:s1> <urn:p2> <urn:o3> .
<urn:s2> <urn:s1> <urn:o5> .
<urn:s3> <urn:p3> <urn:s1> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s1"));
        var d = new DynamicNode(s, g);

        var actual = ((IDictionary<INode, object>)d).Keys;
        var expected = g.GetTriplesWithSubject(s).Select(triple => triple.Predicate).Distinct();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Add_requires_predicate()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.Throws<ArgumentNullException>(() =>
            d.Add(null as INode, null));
    }

    [Fact]
    public void Add_requires_objects()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var p = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.Throws<ArgumentNullException>(() =>
            d.Add(p, null));
    }

    [Fact]
    public void Add_handles_enumerables()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s> <urn:p> <urn:s> .
<urn:s> <urn:p> <urn:p> .
<urn:s> <urn:p> <urn:o> .
");

        var g = new Graph();
        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = g.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, g);

        d.Add(p, new[] { s, p, o });

        Assert.Equal(expected, g);
    }

    [Fact]
    public void Add_handles_strings()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s> <urn:p> ""abc"" .
");

        var g = new Graph();
        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var d = new DynamicNode(s, g);

        d.Add(p, "abc");

        Assert.Equal(expected, g);
    }

    [Fact]
    public void Add_handles_pairs()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s> <urn:p> ""o"" .
");

        var g = new Graph();
        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var d = new DynamicNode(s, g) as IDictionary<INode, object>;

        d.Add(new KeyValuePair<INode, object>(p, "o"));

        Assert.Equal(expected, g);
    }

    [Fact]
    public void Contains_rejects_null_predicate()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.False(d.Contains(null as INode, null));
    }

    [Fact]
    public void Contains_rejects_null_objects()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var p = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.False(d.Contains(p, null));
    }

    [Fact]
    public void Contains_rejects_missing_predicate()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var p = g.CreateBlankNode();
        var o = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.False(d.Contains(p, o));
    }

    [Fact]
    public void Contains_rejects_missing_objects()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.False(d.Contains(p, o));
    }

    [Fact]
    public void Contains_searches_objects_by_predicate()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = g.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, g);

        Assert.True(d.Contains(p, o));
    }

    [Fact]
    public void Contains_handles_enumerables()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:p> <urn:s> .
<urn:s> <urn:p> <urn:p> .
<urn:s> <urn:p> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var d = new DynamicNode(s, g);

        Assert.True(d.Contains(p, new[] { s, p }));
    }

    [Fact]
    public void Contains_handles_strings()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:p> ""o"" .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var d = new DynamicNode(s, g);

        Assert.True(d.Contains(p, "o"));
    }

    [Fact]
    [SuppressMessage("Assertions", "xUnit2017:Do not use Contains() to check if a value exists in a collection", Justification = "We are testing a specific `contains` method")]
    public void Contains_handles_pairs()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = g.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, g);

        Assert.True(d.Contains(new KeyValuePair<INode, object>(p, o)));
    }

    [Fact]
    public void ContainsKey_rejects_missing_key()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.False(d.ContainsKey(null as INode));
    }

    [Fact]
    public void ContainsKey_searches_predicates_by_subject()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = g.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, g);

        Assert.False(d.ContainsKey(s));
        Assert.True(d.ContainsKey(p));
        Assert.False(d.ContainsKey(o));
    }

    [Fact]
    public void Copies_pairs_with_predicate_key_and_dynamic_objects_value()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:s> <urn:s> . # 1.1
<urn:s> <urn:s> <urn:p> . # 1.2
<urn:s> <urn:s> <urn:o> . # 1.3
<urn:s> <urn:p> <urn:s> . # 2.1
<urn:s> <urn:p> <urn:p> . # 2.2
<urn:s> <urn:p> <urn:o> . # 2.3
<urn:s> <urn:o> <urn:s> . # 3.1
<urn:s> <urn:o> <urn:p> . # 3.2
<urn:s> <urn:o> <urn:o> . # 3.3
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = g.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, g);
        var array = new KeyValuePair<INode, object>[5];
        var spo = new[] { s, p, o };
        void isEmpty(KeyValuePair<INode, object> actual)
        {
            Assert.Equal(default(KeyValuePair<INode, object>), actual);
        }

        Action<KeyValuePair<INode, object>> isSPOWith(INode expected)
        {
            return actual =>
            {
                Assert.Equal(expected, actual.Key);
                Assert.IsType<DynamicObjectCollection>(actual.Value);
                Assert.Equal(spo, actual.Value);
            };
        }

        ((IDictionary<INode, object>)d).CopyTo(array, 1);

        Assert.Collection(
            array,
            isEmpty,
            isSPOWith(s),
            isSPOWith(p),
            isSPOWith(o),
            isEmpty);
    }

    [Fact]
    public void Enumerates_pairs_with_predicate_key_and_dynamic_objects_value()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:s> <urn:s> . # 1.1
<urn:s> <urn:s> <urn:p> . # 1.2
<urn:s> <urn:s> <urn:o> . # 1.3
<urn:s> <urn:p> <urn:s> . # 2.1
<urn:s> <urn:p> <urn:p> . # 2.2
<urn:s> <urn:p> <urn:o> . # 2.3
<urn:s> <urn:o> <urn:s> . # 3.1
<urn:s> <urn:o> <urn:p> . # 3.2
<urn:s> <urn:o> <urn:o> . # 3.3
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = g.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, g);
        var spo = new[] { s, p, o };

        using (var actual = d.Cast<KeyValuePair<INode, object>>().GetEnumerator())
        {
            using (var expected = spo.Cast<INode>().GetEnumerator())
            {
                while (expected.MoveNext() | actual.MoveNext())
                {
                    Assert.Equal(expected.Current, actual.Current.Key);
                    Assert.IsType<DynamicObjectCollection>(actual.Current.Value);
                    Assert.Equal(spo, actual.Current.Value);
                }
            }
        }
    }

    [Fact]
    public void Remove_p_rejects_null_predicate()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.False(d.Remove(null as INode));
    }

    [Fact]
    public void Remove_p_retracts_by_subject_and_predicate()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s> <urn:s> <urn:s> .
<urn:s> <urn:s> <urn:p> .
<urn:s> <urn:s> <urn:o> .
# <urn:s> <urn:p> <urn:s> .
# <urn:s> <urn:p> <urn:p> .
# <urn:s> <urn:p> <urn:o> .
<urn:s> <urn:o> <urn:s> .
<urn:s> <urn:o> <urn:p> .
<urn:s> <urn:o> <urn:o> .
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var actual = new Graph();
        actual.LoadFromString(@"
<urn:s> <urn:s> <urn:s> .
<urn:s> <urn:s> <urn:p> .
<urn:s> <urn:s> <urn:o> .
<urn:s> <urn:p> <urn:s> . # should retract
<urn:s> <urn:p> <urn:p> . # should retract
<urn:s> <urn:p> <urn:o> . # should retract
<urn:s> <urn:o> <urn:s> .
<urn:s> <urn:o> <urn:p> .
<urn:s> <urn:o> <urn:o> .
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var s = actual.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = actual.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var d = new DynamicNode(s, actual);

        d.Remove(p);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Remove_p_reports_retraction_success()
    {
        var actual = new Graph();
        actual.LoadFromString(@"
<urn:s> <urn:s> <urn:s> .
<urn:s> <urn:s> <urn:p> .
<urn:s> <urn:s> <urn:o> .
<urn:s> <urn:p> <urn:s> . # should retract
<urn:s> <urn:p> <urn:p> . # should retract
<urn:s> <urn:p> <urn:o> . # should retract
<urn:s> <urn:o> <urn:s> .
<urn:s> <urn:o> <urn:p> .
<urn:s> <urn:o> <urn:o> .
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var s = actual.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = actual.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var d = new DynamicNode(s, actual);

        Assert.True(d.Remove(p));
        Assert.False(d.Remove(p));
    }

    [Fact]
    public void Remove_po_rejects_null_predicate()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.False(d.Remove(null as INode, null));
    }

    [Fact]
    public void Remove_po_rejects_null_object()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var p = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.False(d.Remove(p, null));
    }

    [Fact]
    public void Remove_po_retracts_by_subject_predicate_and_objects()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s> <urn:s> <urn:s> .
<urn:s> <urn:s> <urn:p> .
<urn:s> <urn:s> <urn:o> .
<urn:s> <urn:p> <urn:s> .
<urn:s> <urn:p> <urn:p> .
# <urn:s> <urn:p> <urn:o> .
<urn:s> <urn:o> <urn:s> .
<urn:s> <urn:o> <urn:p> .
<urn:s> <urn:o> <urn:o> .
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var actual = new Graph();
        actual.LoadFromString(@"
<urn:s> <urn:s> <urn:s> .
<urn:s> <urn:s> <urn:p> .
<urn:s> <urn:s> <urn:o> .
<urn:s> <urn:p> <urn:s> .
<urn:s> <urn:p> <urn:p> .
<urn:s> <urn:p> <urn:o> . # should retract
<urn:s> <urn:o> <urn:s> .
<urn:s> <urn:o> <urn:p> .
<urn:s> <urn:o> <urn:o> .
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var s = actual.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = actual.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = actual.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, actual);

        d.Remove(p, o);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Remove_po_handles_enumerables()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s> <urn:s> <urn:s> .
<urn:s> <urn:s> <urn:p> .
<urn:s> <urn:s> <urn:o> .
# <urn:s> <urn:p> <urn:s> .
# <urn:s> <urn:p> <urn:p> .
# <urn:s> <urn:p> <urn:o> .
<urn:s> <urn:o> <urn:s> .
<urn:s> <urn:o> <urn:p> .
<urn:s> <urn:o> <urn:o> .
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var actual = new Graph();
        actual.LoadFromString(@"
<urn:s> <urn:s> <urn:s> .
<urn:s> <urn:s> <urn:p> .
<urn:s> <urn:s> <urn:o> .
<urn:s> <urn:p> <urn:s> . # should retract
<urn:s> <urn:p> <urn:p> . # should retract
<urn:s> <urn:p> <urn:o> . # should retract
<urn:s> <urn:o> <urn:s> .
<urn:s> <urn:o> <urn:p> .
<urn:s> <urn:o> <urn:o> .
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var s = actual.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = actual.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = actual.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, actual);

        d.Remove(p, new[] { s, p, o });

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Remove_po_handles_strings()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s> <urn:p> ""s"" .
<urn:s> <urn:p> ""p"" .
# <urn:s> <urn:p> ""o"" .
");

        var actual = new Graph();
        actual.LoadFromString(@"
<urn:s> <urn:p> ""s"" .
<urn:s> <urn:p> ""p"" .
<urn:s> <urn:p> ""o"" . # should retract
");

        var s = actual.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = actual.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var d = new DynamicNode(s, actual);

        d.Remove(p, "o");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Remove_po_handles_pairs()
    {
        var expected = new Graph();
        expected.LoadFromString(@"
<urn:s> <urn:s> <urn:s> .
<urn:s> <urn:s> <urn:p> .
<urn:s> <urn:s> <urn:o> .
<urn:s> <urn:p> <urn:s> .
<urn:s> <urn:p> <urn:p> .
# <urn:s> <urn:p> <urn:o> .
<urn:s> <urn:o> <urn:s> .
<urn:s> <urn:o> <urn:p> .
<urn:s> <urn:o> <urn:o> .
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var actual = new Graph();
        actual.LoadFromString(@"
<urn:s> <urn:s> <urn:s> .
<urn:s> <urn:s> <urn:p> .
<urn:s> <urn:s> <urn:o> .
<urn:s> <urn:p> <urn:s> .
<urn:s> <urn:p> <urn:p> .
<urn:s> <urn:p> <urn:o> . # should retract
<urn:s> <urn:o> <urn:s> .
<urn:s> <urn:o> <urn:p> .
<urn:s> <urn:o> <urn:o> .
<urn:p> <urn:s> <urn:s> .
<urn:p> <urn:s> <urn:p> .
<urn:p> <urn:s> <urn:o> .
<urn:p> <urn:p> <urn:s> .
<urn:p> <urn:p> <urn:p> .
<urn:p> <urn:p> <urn:o> .
<urn:p> <urn:o> <urn:s> .
<urn:p> <urn:o> <urn:p> .
<urn:p> <urn:o> <urn:o> .
<urn:o> <urn:s> <urn:s> .
<urn:o> <urn:s> <urn:p> .
<urn:o> <urn:s> <urn:o> .
<urn:o> <urn:p> <urn:s> .
<urn:o> <urn:p> <urn:p> .
<urn:o> <urn:p> <urn:o> .
<urn:o> <urn:o> <urn:s> .
<urn:o> <urn:o> <urn:p> .
<urn:o> <urn:o> <urn:o> .
");

        var s = actual.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = actual.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var o = actual.CreateUriNode(UriFactory.Root.Create("urn:o"));
        var d = new DynamicNode(s, actual);

        ((IDictionary<INode, object>)d).Remove(new KeyValuePair<INode, object>(p, o));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryGetValue_rejects_null_predicate()
    {
        var g = new Graph();
        var s = g.CreateBlankNode();
        var d = new DynamicNode(s, g);

        Assert.False(d.TryGetValue(null as INode, out var objects));
    }

    [Fact]
    public void TryGetValue_rejects_missing_predicate()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var x = g.CreateUriNode(UriFactory.Root.Create("urn:x"));
        var d = new DynamicNode(s, g);

        Assert.False(d.TryGetValue(x, out var objects));
    }

    [Fact]
    public void TryGetValue_outputs_objects_by_predicate()
    {
        var g = new Graph();
        g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

        var s = g.CreateUriNode(UriFactory.Root.Create("urn:s"));
        var p = g.CreateUriNode(UriFactory.Root.Create("urn:p"));
        var d = new DynamicNode(s, g);

        Assert.True(d.TryGetValue(p, out var objects));
        Assert.IsType<DynamicObjectCollection>(objects);
    }
}
