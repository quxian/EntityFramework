// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;

namespace Microsoft.Data.Entity.Storage
{
    public class RelationalCommandBuilder : IRelationalCommandBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRelationalTypeMapper _typeMapper;

        public RelationalCommandBuilder(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IRelationalTypeMapper typeMapper)
        {
            Check.NotNull(loggerFactory, nameof(loggerFactory));
            Check.NotNull(typeMapper, nameof(typeMapper));

            _loggerFactory = loggerFactory;
            _typeMapper = typeMapper;
        }

        private readonly IndentedStringBuilder _stringBuilder = new IndentedStringBuilder();

        public virtual IRelationalCommandBuilder AppendLine()
        {
            _stringBuilder.AppendLine();

            return this;
        }

        public virtual IRelationalCommandBuilder Append([NotNull] object o)
        {
            Check.NotNull(o, nameof(o));

            _stringBuilder.Append(o);

            return this;
        }

        public virtual IRelationalCommandBuilder AppendLine([NotNull] object o)
        {
            Check.NotNull(o, nameof(o));

            _stringBuilder.AppendLine(o);

            return this;
        }

        public virtual IRelationalCommandBuilder AppendLines([NotNull] object o)
        {
            Check.NotNull(o, nameof(o));

            _stringBuilder.AppendLines(o);

            return this;
        }

        public virtual IRelationalCommand BuildRelationalCommand()
            => new RelationalCommand(
                _loggerFactory,
                _typeMapper,
                _stringBuilder.ToString(),
                RelationalParameterList.RelationalParameters);

        public virtual RelationalParameterList RelationalParameterList { get; } = new RelationalParameterList();

        public virtual IDisposable Indent()
            => _stringBuilder.Indent();

        public virtual int Length => _stringBuilder.Length;

        public virtual IRelationalCommandBuilder Clear()
        {
            _stringBuilder.Clear();

            return this;
        }

        public virtual IRelationalCommandBuilder IncrementIndent()
        {
            _stringBuilder.IncrementIndent();

            return this;
        }

        public virtual IRelationalCommandBuilder DecrementIndent()
        {
            _stringBuilder.DecrementIndent();

            return this;
        }

        public override string ToString() => _stringBuilder.ToString();
    }
}
