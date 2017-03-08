﻿// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.LondonTravel.Site.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a document database client.
    /// </summary>
    public interface IDocumentClient : IDisposable
    {
        /// <summary>
        /// Creates a new document in the store as an asynchronous operation.
        /// </summary>
        /// <param name="document">The document to create.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation to add
        /// the specified document to the store which returns the Id of the new document.
        /// </returns>
        Task<string> CreateAsync(object document);

        /// <summary>
        /// Deletes the document with the specified Id as an asynchronous operation.
        /// </summary>
        /// <param name="id">The Id of the document to delete.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation to delete the
        /// document with the specified Id which returns <see langword="true"/> if the document
        /// was deleted or <see langword="false"/> if not found.
        /// </returns>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Gets the document with the specified Id as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">The type of the document to return.</typeparam>
        /// <param name="id">The Id of the document to retrieve.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation to get the
        /// document with the specified Id of the specified type or <see langword="null"/> if not found.
        /// </returns>
        Task<T> GetAsync<T>(string id)
            where T : class;

        /// <summary>
        /// Gets any documents that match the specified predicate as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">The type of the documents to return.</typeparam>
        /// <param name="predicate">A predicate to use to match documents of interest.</param>
        /// <param name="cancellationToken">The cancellation token to use.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation to query the
        /// document store for matching documents of the specified type.
        /// </returns>
        Task<IEnumerable<T>> GetAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
            where T : class;

        /// <summary>
        /// Replaces the document with the specified Id with the
        /// specified new document as an asynchronous operation.
        /// </summary>
        /// <param name="id">The Id of the document to replace.</param>
        /// <param name="document">The replacement document.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the operation to update the document.
        /// </returns>
        Task ReplaceAsync(string id, object document);
    }
}
