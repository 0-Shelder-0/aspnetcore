// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.WebUtilities
{
    public class QueryHelperTests
    {
        [Fact]
        public void ParseQueryWithUniqueKeysWorks()
        {
            var collection = QueryHelpers.ParseQuery("?key1=value1&key2=value2");
            Assert.Equal(2, collection.Count);
            Assert.Equal("value1", collection["key1"].FirstOrDefault());
            Assert.Equal("value2", collection["key2"].FirstOrDefault());
        }

        [Fact]
        public void ParseQueryWithoutQuestionmarkWorks()
        {
            var collection = QueryHelpers.ParseQuery("key1=value1&key2=value2");
            Assert.Equal(2, collection.Count);
            Assert.Equal("value1", collection["key1"].FirstOrDefault());
            Assert.Equal("value2", collection["key2"].FirstOrDefault());
        }

        [Fact]
        public void ParseQueryWithDuplicateKeysGroups()
        {
            var collection = QueryHelpers.ParseQuery("?key1=valueA&key2=valueB&key1=valueC");
            Assert.Equal(2, collection.Count);
            Assert.Equal(new[] { "valueA", "valueC" }, collection["key1"]);
            Assert.Equal("valueB", collection["key2"].FirstOrDefault());
        }

        [Fact]
        public void ParseQueryWithEmptyValuesWorks()
        {
            var collection = QueryHelpers.ParseQuery("?key1=&key2=");
            Assert.Equal(2, collection.Count);
            Assert.Equal(string.Empty, collection["key1"].FirstOrDefault());
            Assert.Equal(string.Empty, collection["key2"].FirstOrDefault());
        }

        [Fact]
        public void ParseQueryWithEmptyKeyWorks()
        {
            var collection = QueryHelpers.ParseQuery("?=value1&=");
            Assert.Equal(1, collection.Count);
            Assert.Equal(new[] { "value1", "" }, collection[""]);
        }
    }
}