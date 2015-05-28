// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.Html
{
    public class HtmlUrlAttributeTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void SimpleUrlInAttributeInMarkupBlock()
        {
            ParseBlockTest("<a href='~/Foo/Bar/Baz' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href='", 2, 0, 2), new LocationTagged<string>("'", 22, 0, 22)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            Factory.Markup("~/Foo/Bar/Baz")
                                   .WithEditorHints(EditorHints.VirtualPath)
                                   .With(new LiteralAttributeChunkGenerator(
                                       new LocationTagged<string>(string.Empty, 9, 0, 9),
                                       new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 9, 0, 9))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void SimpleUrlInAttributeInMarkupDocument()
        {
            ParseDocumentTest("<a href='~/Foo/Bar/Baz' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href='", 2, 0, 2), new LocationTagged<string>("'", 22, 0, 22)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            Factory.Markup("~/Foo/Bar/Baz")
                                   .WithEditorHints(EditorHints.VirtualPath)
                                   .With(new LiteralAttributeChunkGenerator(
                                       new LocationTagged<string>(string.Empty, 9, 0, 9),
                                       new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 9, 0, 9))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />"))));
        }

        [Fact]
        public void SimpleUrlInAttributeInMarkupSection()
        {
            ParseDocumentTest("@section Foo { <a href='~/Foo/Bar/Baz' /> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true)
                               .Accepts(AcceptedCharacters.Any),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<a"),
                                new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href='", 17, 0, 17), new LocationTagged<string>("'", 37, 0, 37)),
                                    Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                                    Factory.Markup("~/Foo/Bar/Baz")
                                           .WithEditorHints(EditorHints.VirtualPath)
                                           .With(new LiteralAttributeChunkGenerator(
                                               new LocationTagged<string>(string.Empty, 24, 0, 24),
                                               new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 24, 0, 24))),
                                    Factory.Markup("'").With(SpanChunkGenerator.Null)),
                                Factory.Markup(" />")),
                            Factory.Markup(" ")
                        ),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void UrlWithExpressionsInAttributeInMarkupBlock()
        {
            ParseBlockTest("<a href='~/Foo/@id/Baz' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                            new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href='", 2, 0, 2), new LocationTagged<string>("'", 22, 0, 22)),
                                Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                                Factory.Markup("~/Foo/")
                                        .WithEditorHints(EditorHints.VirtualPath)
                                        .With(new LiteralAttributeChunkGenerator(
                                            new LocationTagged<string>(string.Empty, 9, 0, 9),
                                            new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 9, 0, 9))),
                                new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), 15, 0, 15),
                                    new ExpressionBlock(
                                        Factory.CodeTransition().Accepts(AcceptedCharacters.None),
                                        Factory.Code("id")
                                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                               .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                Factory.Markup("/Baz")
                                       .With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 18, 0, 18), new LocationTagged<string>("/Baz", 18, 0, 18))),
                                Factory.Markup("'").With(SpanChunkGenerator.Null)),
                            Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void UrlWithExpressionsInAttributeInMarkupDocument()
        {
            ParseDocumentTest("<a href='~/Foo/@id/Baz' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href='", 2, 0, 2), new LocationTagged<string>("'", 22, 0, 22)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            Factory.Markup("~/Foo/")
                                   .WithEditorHints(EditorHints.VirtualPath)
                                   .With(new LiteralAttributeChunkGenerator(
                                       new LocationTagged<string>(string.Empty, 9, 0, 9),
                                       new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 9, 0, 9))),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), 15, 0, 15),
                                new ExpressionBlock(
                                    Factory.CodeTransition().Accepts(AcceptedCharacters.None),
                                    Factory.Code("id")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace))),
                            Factory.Markup("/Baz")
                                   .With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 18, 0, 18), new LocationTagged<string>("/Baz", 18, 0, 18))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />"))));
        }

        [Fact]
        public void UrlWithExpressionsInAttributeInMarkupSection()
        {
            ParseDocumentTest("@section Foo { <a href='~/Foo/@id/Baz' /> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<a"),
                                new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href='", 17, 0, 17), new LocationTagged<string>("'", 37, 0, 37)),
                                    Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                                    Factory.Markup("~/Foo/")
                                           .WithEditorHints(EditorHints.VirtualPath)
                                           .With(new LiteralAttributeChunkGenerator(
                                               new LocationTagged<string>(string.Empty, 24, 0, 24),
                                               new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 24, 0, 24))),
                                    new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 30, 0, 30), 30, 0, 30),
                                        new ExpressionBlock(
                                            Factory.CodeTransition().Accepts(AcceptedCharacters.None),
                                            Factory.Code("id")
                                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                   .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                    Factory.Markup("/Baz")
                                           .With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 33, 0, 33), new LocationTagged<string>("/Baz", 33, 0, 33))),
                                    Factory.Markup("'").With(SpanChunkGenerator.Null)),
                                Factory.Markup(" />")),
                            Factory.Markup(" ")
                        ),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void UrlWithComplexCharactersInAttributeInMarkupBlock()
        {
            ParseBlockTest("<a href='~/Foo+Bar:Baz(Biz),Boz' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href='", 2, 0, 2), new LocationTagged<string>("'", 31, 0, 31)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            Factory.Markup("~/Foo+Bar:Baz(Biz),Boz")
                                   .WithEditorHints(EditorHints.VirtualPath)
                                   .With(new LiteralAttributeChunkGenerator(
                                       new LocationTagged<string>(string.Empty, 9, 0, 9),
                                       new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 9, 0, 9))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void UrlWithComplexCharactersInAttributeInMarkupDocument()
        {
            ParseDocumentTest("<a href='~/Foo+Bar:Baz(Biz),Boz' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href='", 2, 0, 2), new LocationTagged<string>("'", 31, 0, 31)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            Factory.Markup("~/Foo+Bar:Baz(Biz),Boz")
                                   .WithEditorHints(EditorHints.VirtualPath)
                                   .With(new LiteralAttributeChunkGenerator(
                                       new LocationTagged<string>(string.Empty, 9, 0, 9),
                                       new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 9, 0, 9))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />"))));
        }

        [Fact]
        public void UrlInUnquotedAttributeValueInMarkupBlock()
        {
            ParseBlockTest("<a href=~/Foo+Bar:Baz(Biz),Boz/@id/Boz />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href=", 2, 0, 2), new LocationTagged<string>(string.Empty, 38, 0, 38)),
                            Factory.Markup(" href=").With(SpanChunkGenerator.Null),
                            Factory.Markup("~/Foo+Bar:Baz(Biz),Boz/")
                                   .WithEditorHints(EditorHints.VirtualPath)
                                   .With(new LiteralAttributeChunkGenerator(
                                       new LocationTagged<string>(string.Empty, 8, 0, 8),
                                       new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 8, 0, 8))),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 31, 0, 31), 31, 0, 31),
                                new ExpressionBlock(
                                    Factory.CodeTransition()
                                           .Accepts(AcceptedCharacters.None),
                                    Factory.Code("id")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharacters.NonWhiteSpace))),
                            Factory.Markup("/Boz").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 34, 0, 34), new LocationTagged<string>("/Boz", 34, 0, 34)))),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void UrlInUnquotedAttributeValueInMarkupDocument()
        {
            ParseDocumentTest("<a href=~/Foo+Bar:Baz(Biz),Boz/@id/Boz />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href=", 2, 0, 2), new LocationTagged<string>(string.Empty, 38, 0, 38)),
                            Factory.Markup(" href=").With(SpanChunkGenerator.Null),
                            Factory.Markup("~/Foo+Bar:Baz(Biz),Boz/")
                                   .WithEditorHints(EditorHints.VirtualPath)
                                   .With(new LiteralAttributeChunkGenerator(
                                       new LocationTagged<string>(string.Empty, 8, 0, 8),
                                       new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 8, 0, 8))),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 31, 0, 31), 31, 0, 31),
                                new ExpressionBlock(
                                    Factory.CodeTransition()
                                           .Accepts(AcceptedCharacters.None),
                                    Factory.Code("id")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharacters.NonWhiteSpace))),
                            Factory.Markup("/Boz").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 34, 0, 34), new LocationTagged<string>("/Boz", 34, 0, 34)))),
                        Factory.Markup(" />"))));
        }

        [Fact]
        public void UrlInUnquotedAttributeValueInMarkupSection()
        {
            ParseDocumentTest("@section Foo { <a href=~/Foo+Bar:Baz(Biz),Boz/@id/Boz /> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<a"),
                                new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href=", 17, 0, 17), new LocationTagged<string>(string.Empty, 53, 0, 53)),
                                    Factory.Markup(" href=").With(SpanChunkGenerator.Null),
                                    Factory.Markup("~/Foo+Bar:Baz(Biz),Boz/")
                                            .WithEditorHints(EditorHints.VirtualPath)
                                            .With(new LiteralAttributeChunkGenerator(
                                                new LocationTagged<string>(string.Empty, 23, 0, 23),
                                                new LocationTagged<SpanChunkGenerator>(new ResolveUrlChunkGenerator(), 23, 0, 23))),
                                    new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 46, 0, 46), 46, 0, 46),
                                        new ExpressionBlock(
                                            Factory.CodeTransition()
                                                   .Accepts(AcceptedCharacters.None),
                                            Factory.Code("id")
                                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                        .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                    Factory.Markup("/Boz").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 49, 0, 49), new LocationTagged<string>("/Boz", 49, 0, 49)))),
                                Factory.Markup(" />")),
                                Factory.Markup(" ")
                            ),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }
    }
}
