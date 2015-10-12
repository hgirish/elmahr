using System.Threading.Tasks;

namespace ElmahR.Core
{
    #region Imports

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Config;

    #endregion

    public class ErrorsBarrier
    {
        private ErrorsBarrier(DateTime when, int offset, int countBefore, int countAfter)
        {
            When = when;
            Offset = offset;
            CountBefore = countBefore;
            CountAfter = countAfter;
        }

        public static ErrorsBarrier Create(DateTime when, int offset, int countBefore, int countAfter)
        {
            return new ErrorsBarrier(when, offset, countBefore, countAfter);
        }

        public DateTime When   { get; private set; }
        public int Offset      { get; private set; }
        public int CountBefore { get; private set; }
        public int CountAfter  { get; private set; }
    }

    public class ErrorsResume
    {
        private ErrorsResume(DateTime when, int count, IEnumerable<ErrorsBarrier> barriers)
        {
            When = when;
            Count = count;
            Barriers = barriers.ToList();
        }

        public static ErrorsResume Create(DateTime when, int count, IEnumerable<ErrorsBarrier> barriers)
        {
            return new ErrorsResume(when, count, barriers);    
        }

        public DateTime When                       { get; private set; }
        public int Count                           { get; private set; }
        public IEnumerable<ErrorsBarrier> Barriers { get; private set; }
    }

    public interface IApplications : IEnumerable<Application>
    {
        Applications AddSource(string applicationName, string sourceId, string testExceptionUrl, SourceSection config);
        Application this[string sourceId] { get; }
        IEnumerable<Error> GetErrors(int count, string[] activeApps);
        IEnumerable<Error> GetErrors(int count, string beforeId, string[] activeApps);
        bool HasSource(string sourceId);
        string SelfSourceId { get; }
        Error GetError(string id);
        ErrorsResume GetErrorsResume(string sourceId);
        void ClearErrorsBefore(string sourceId, DateTime when);
        void ClearErrorsAfter(string sourceId, DateTime when);
        IEnumerable<TK> GetErrorsStats<TK>(string[] applicationIds, Func<string, string, DateTime, int, TK> selector);
        void Banner(Action<string> func);
    }
}