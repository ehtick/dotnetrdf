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

using System.IO;
using VDS.RDF.Parsing;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Writing.Contexts;

/// <summary>
/// Writer Context for Turtle Writers.
/// </summary>
public class TurtleWriterContext 
    : BaseWriterContext
{
    /// <summary>
    /// Creates a new Turtle Writer Context with default settings.
    /// </summary>
    /// <param name="g">Graph to write.</param>
    /// <param name="output">TextWriter to write to.</param>
    /// <param name="syntax">Turtle Syntax.</param>
    public TurtleWriterContext(IGraph g, TextWriter output, TurtleSyntax syntax)
        : this(g, output, WriterCompressionLevel.More, true, true, syntax) { }

    /// <summary>
    /// Creates a new Turtle Writer Context with default settings.
    /// </summary>
    /// <param name="g">Graph to write.</param>
    /// <param name="output">TextWriter to write to.</param>
    public TurtleWriterContext(IGraph g, TextWriter output)
        : this(g, output, TurtleSyntax.Original) { }

    /// <summary>
    /// Creates a new Turtle Writer Context with custom settings.
    /// </summary>
    /// <param name="g">Graph to write.</param>
    /// <param name="output">TextWriter to write to.</param>
    /// <param name="prettyPrint">Pretty Print Mode.</param>
    /// <param name="hiSpeed">High Speed Mode.</param>
    /// <param name="syntax">Turtle Syntax.</param>
    public TurtleWriterContext(IGraph g, TextWriter output, bool prettyPrint, bool hiSpeed, TurtleSyntax syntax)
        : this(g, output, WriterCompressionLevel.More, prettyPrint, hiSpeed, syntax) { }

    /// <summary>
    /// Creates a new Turtle Writer Context with custom settings.
    /// </summary>
    /// <param name="g">Graph to write.</param>
    /// <param name="output">TextWriter to write to.</param>
    /// <param name="prettyPrint">Pretty Print Mode.</param>
    /// <param name="hiSpeed">High Speed Mode.</param>
    public TurtleWriterContext(IGraph g, TextWriter output, bool prettyPrint, bool hiSpeed)
        : this(g, output, WriterCompressionLevel.More, prettyPrint, hiSpeed, TurtleSyntax.Original) { }

    /// <summary>
    /// Creates a new Turtle Writer Context with custom settings.
    /// </summary>
    /// <param name="g">Graph to write.</param>
    /// <param name="output">TextWriter to write to.</param>
    /// <param name="compressionLevel">Compression Level to use.</param>
    /// <param name="prettyPrint">Pretty Print Mode.</param>
    /// <param name="hiSpeed">High Speed Mode.</param>
    /// <param name="syntax">Turtle Syntax.</param>
    public TurtleWriterContext(IGraph g, TextWriter output, int compressionLevel, bool prettyPrint, bool hiSpeed, TurtleSyntax syntax)
        : base(g, output, compressionLevel, prettyPrint, hiSpeed) 
    {
        NodeFormatter = (syntax == TurtleSyntax.Original ? new TurtleFormatter(g) : new TurtleW3CFormatter(g));
        _uriFormatter = (IUriFormatter)NodeFormatter;
    }
}
