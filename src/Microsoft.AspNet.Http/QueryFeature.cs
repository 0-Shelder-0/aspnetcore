// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Collections;
using Microsoft.AspNet.Http.Infrastructure;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Http
{
    public class QueryFeature : IQueryFeature
    {
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpRequestFeature> _request = FeatureReference<IHttpRequestFeature>.Default;
        private string _queryString;
        private IReadableStringCollection _query;

        public QueryFeature([NotNull] IDictionary<string, string[]> query)
            : this (new ReadableStringCollection(query))
        {
        }

        public QueryFeature([NotNull] IReadableStringCollection query)
        {
            _query = query;
        }

        public QueryFeature([NotNull] IFeatureCollection features)
        {
            _features = features;
        }

        public IReadableStringCollection Query
        {
            get
            {
                if (_features == null)
                {
                    return _query;
                }

                var queryString = _request.Fetch(_features).QueryString;
                if (_query == null || _queryString != queryString)
                {
                    _queryString = queryString;
                    _query = new ReadableStringCollection(QueryHelpers.ParseQuery(queryString));
                }
                return _query;
            }
        }
    }
}