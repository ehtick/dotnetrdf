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
using VDS.RDF.Nodes;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.FullText;
using VDS.RDF.Query.FullText.Search;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Query.PropertyFunctions;

/// <summary>
/// Property Function which does full text matching.
/// </summary>
public class FullTextMatchPropertyFunction
    : ILeviathanPropertyFunction
{
    private readonly PatternItem _matchVar;
    private readonly PatternItem _scoreVar;
    private readonly PatternItem _searchVar;
    private readonly List<String> _vars = new();
    private int? _limit;
    private double? _threshold;

    /// <summary>
    /// Constructs a Full Text Match property function.
    /// </summary>
    /// <param name="info">Property Function information.</param>
    public FullTextMatchPropertyFunction(PropertyFunctionInfo info)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        if (!EqualityHelper.AreUrisEqual(info.FunctionUri, FunctionUri)) throw new ArgumentException("Property Function information is not valid for this function");

        //Get basic arguments
        _matchVar = info.SubjectArgs[0];
        _vars.AddRange(_matchVar.Variables);
        if (info.SubjectArgs.Count == 2)
        {
            _scoreVar = info.SubjectArgs[1];
            foreach (var v in _scoreVar.Variables)
            {
                if (!_vars.Contains(v)) _vars.Add(v);
            }
        }

        //Check extended arguments
        _searchVar = info.ObjectArgs[0];
        foreach(var v in _searchVar.Variables) { if (!_vars.Contains(v)) _vars.Add(v); }
        switch (info.ObjectArgs.Count)
        {
            case 1:
                break;
            case 2:
                PatternItem arg = info.ObjectArgs[1];
                if (!arg.IsFixed) throw new RdfQueryException("Cannot use a variable as the limit/score threshold for full text queries, must use a numeric constant");
                IValuedNode n = ((NodeMatchPattern)arg).Node.AsValuedNode();
                switch (n.NumericType)
                {
                    case SparqlNumericType.Integer:
                        _limit = (int)n.AsInteger();
                        break;
                    case SparqlNumericType.Decimal:
                    case SparqlNumericType.Double:
                    case SparqlNumericType.Float:
                        _threshold = n.AsDouble();
                        break;
                    default:
                        throw new RdfQueryException("Cannot use a non-numeric constant as the limit/score threshold for full text queries, must use a numeric constant");
                }
                break;
            default:
                PatternItem arg1 = info.ObjectArgs[1];
                PatternItem arg2 = info.ObjectArgs[2];
                if (!arg1.IsFixed || !arg2.IsFixed) throw new RdfQueryException("Cannot use a variable as the limit/score threshold for full text queries, must use a numeric constant");
                IValuedNode n1 = ((NodeMatchPattern)arg1).Node.AsValuedNode();
                _threshold = n1.NumericType switch
                {
                    SparqlNumericType.NaN => throw new RdfQueryException(
                        "Cannot use a non-numeric constant as the score threshold for full text queries, must use a numeric constant"),
                    _ => n1.AsDouble()
                };
                IValuedNode n2 = ((NodeMatchPattern)arg2).Node.AsValuedNode();
                _limit = n2.NumericType switch
                {
                    SparqlNumericType.NaN => throw new RdfQueryException(
                        "Cannot use a non-numeric constant as the limit for full text queries, must use a numeric constant"),
                    _ => (int)n2.AsInteger()
                };
                break;
        }
    }

    /// <summary>
    /// Gets the Function URI for the property function.
    /// </summary>
    public Uri FunctionUri
    {
        get 
        {
            return UriFactory.Root.Create(FullTextHelper.FullTextMatchPredicateUri); 
        }
    }

    /// <summary>
    /// Gets the Variables used in the property function.
    /// </summary>
    public IEnumerable<String> Variables
    {
        get
        {
            return _vars;
        }
    }

    /// <summary>
    /// Evaluates the property function.
    /// </summary>
    /// <param name="context">Evaluation Context.</param>
    /// <returns></returns>
    public BaseMultiset Evaluate(SparqlEvaluationContext context)
    {
        //The very first thing we must do is check the incoming input
        if (context.InputMultiset is NullMultiset) return context.InputMultiset; //Can abort evaluation if input is null
        if (context.InputMultiset.IsEmpty) return context.InputMultiset; //Can abort evaluation if input is null

        //Then we need to retrieve the full text search provider
        if (context[FullTextHelper.ContextKey] is not IFullTextSearchProvider provider)
        {
            throw new FullTextQueryException("No Full Text Search Provider is available, please ensure you attach a FullTextQueryOptimiser to your query");
        }

        //First determine whether we can apply the limit when talking to the provider
        //Essentially as long as the Match Variable (the one we'll bind results to) is not already
        //bound AND we are actually using a limit
        var applyLimitDirect = _limit.HasValue && _limit.Value > -1 && !_matchVar.IsFixed && !context.InputMultiset.ContainsVariables(_matchVar.Variables);

        //Is there a constant for the Match Item?  If so extract it now
        //Otherwise are we needing to check against existing bindings
        INode matchConstant = null;
        var checkExisting = false;
        HashSet<INode> existing = null;
        if (_matchVar.IsFixed)
        {
            matchConstant = _matchVar.Bind(new Set());
        }
        else if (context.InputMultiset.ContainsVariables(_matchVar.Variables))
        {
            checkExisting = true;
            existing = new HashSet<INode>();
            foreach (INode n in context.InputMultiset.Sets.Select(s => _matchVar.Bind(s)).Where(bound => bound != null))
            {
                existing.Add(n);
            }
        }

        //Then check that the score variable is not already bound, if so error
        //If a Score Variable is provided and it is OK then we'll bind scores at a later stage
        if (_scoreVar != null)
        {
            if (_scoreVar.IsFixed) throw new FullTextQueryException("Queries using full text search that wish to return result scores must provide a variable");
            if (context.InputMultiset.ContainsVariables(_scoreVar.Variables)) throw new FullTextQueryException("Queries using full text search that wish to return result scores must use an unbound variable to do so");
        }

        //Next ensure that the search text is a node and not a variable
        if (!_searchVar.IsFixed) throw new FullTextQueryException("Queries using full text search must provide a constant value for the search term");
        INode searchNode = ((NodeMatchPattern)_searchVar).Node;
        if (searchNode.NodeType != NodeType.Literal) throw new FullTextQueryException("Queries using full text search must use a literal value for the search term");
        var search = ((ILiteralNode)searchNode).Value;

        //Determine which graphs we are operating over
        IEnumerable<IRefNode> graphUris = context.Data.ActiveGraphNames;

        //Now we can use the full text search provider to start getting results
        context.OutputMultiset = new Multiset();
        IEnumerable<IFullTextSearchResult> results = applyLimitDirect ? GetResults(graphUris, provider, search, _limit.Value) : GetResults(graphUris, provider, search);
        var r = 0;
        var matchVar = _matchVar.Variables.First();
        var scoreVar = _scoreVar?.Variables.FirstOrDefault();
        foreach (IFullTextSearchResult result in results)
        {
            if (matchConstant != null)
            {
                //Check against constant if present
                if (result.Node.Equals(matchConstant))
                {
                    r++;
                    context.OutputMultiset.Add(result.ToSet(matchVar, scoreVar));
                }
            }
            else if (checkExisting)
            {
                //Check against existing bindings if present
                if (existing.Contains(result.Node))
                {
                    r++;
                    context.OutputMultiset.Add(result.ToSet(matchVar, scoreVar));
                }
            }
            else
            {
                //Otherwise all results are acceptable
                r++;
                context.OutputMultiset.Add(result.ToSet(matchVar, scoreVar));
            }

            //Apply the limit locally if necessary
            if (!applyLimitDirect && _limit > -1 && r >= _limit) break;
        }

        return context.OutputMultiset;
    }

    /// <summary>
    /// Gets the Full Text Results for a specific search query.
    /// </summary>
    /// <param name="graphUris">Graph URIs.</param>
    /// <param name="provider">Search Provider.</param>
    /// <param name="search">Search Query.</param>
    /// <returns></returns>
    protected IEnumerable<IFullTextSearchResult> GetResults(IEnumerable<IRefNode> graphUris, IFullTextSearchProvider provider, string search)
    {
        return _threshold.HasValue ? provider.Match(graphUris, search, _threshold.Value) : provider.Match(graphUris, search);
    }

    /// <summary>
    /// Gets the Full Text Results for a specific search query.
    /// </summary>
    /// <param name="graphUris">Graph URIs.</param>
    /// <param name="provider">Search Provider.</param>
    /// <param name="search">Search Query.</param>
    /// <param name="limit">Result Limit.</param>
    /// <returns></returns>
    protected virtual IEnumerable<IFullTextSearchResult> GetResults(IEnumerable<IRefNode> graphUris, IFullTextSearchProvider provider, string search, int limit)
    {
        return _threshold.HasValue ? provider.Match(graphUris, search, _threshold.Value, limit) : provider.Match(graphUris, search, limit);
    }
}
