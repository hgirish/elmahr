namespace ElmahR.Core.Extensions
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion
    
    public static class EnumerableExtensions
    {
        public static IEnumerable<KeyValuePair<TK, TV>> AsKeyTo<T, TK, TV>(this IEnumerable<T> sequence, Func<T, TK> keySelector, Func<T, TV> elementSelector)
        {
            return sequence.Select(e => new KeyValuePair<TK, TV>(keySelector(e), elementSelector(e)));
        }
    }
}