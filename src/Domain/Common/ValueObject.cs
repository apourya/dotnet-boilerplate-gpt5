﻿using System.Collections.Generic;
using System.Linq;

namespace EnterpriseBoilerplate.Domain.Common
{
    public abstract class ValueObject
    {
        protected abstract IEnumerable<object?> GetEqualityComponents();

        public override bool Equals(object? obj)
        {
            if (obj is null || obj.GetType() != GetType()) return false;
            return GetEqualityComponents().SequenceEqual(((ValueObject)obj).GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Aggregate(0, (hash, obj) =>
                {
                    unchecked
                    {
                        return (hash * 397) ^ (obj?.GetHashCode() ?? 0);
                    }
                });
        }
    }
}
