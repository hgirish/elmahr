using System;

namespace ElmahR.SampleSource.Dep
{
    public static class BuggyClass
    {
        public static void RaiseException()
        {
            try
            {
                throw new ArgumentException("Buggy Class raised an exception");
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }
        }
    }
}
