namespace ElmahR.Core.Extensions
{
    #region Imports

    using System;
    using System.Linq;

    #endregion

    public static class String
    {
        public static bool IsTruthy(this string target)
        {
            var truths = new[] { "true", "1", "yes" };
            return !string.IsNullOrWhiteSpace(target) && truths.Any(t => t.Equals(target, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
