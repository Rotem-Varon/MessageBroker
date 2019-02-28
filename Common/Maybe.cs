using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MessageBroker.Common
{
    //http://blog.ploeh.dk/2015/11/13/null-has-no-type-but-maybe-has/ 
    public class Maybe<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _values;

        public Maybe()
        {
            _values = new T[0];
        }

        public Maybe(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            _values = new[] { value };
        }

        [ExcludeFromCodeCoverage]
        public IEnumerator<T> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}