namespace ElmahR.Core.Persistors
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using Config;

    #endregion

    public interface IApplicationsPersistor
    {
        void Init(RootSection rootSection);
        void AddError(Error error, string errorId);
        Error GetError(string id, IApplications applications);
        IEnumerable<Error> GetErrors(int count, string[] activeApps, IApplications applications);
        IEnumerable<Error> GetErrors(int count, string beforeId, string[] activeApps, IApplications applications);
        ErrorsResume GetErrorsResume(string sourceId);
        void ClearErrorsBefore(string sourceId, DateTime when);
        void ClearErrorsAfter(string sourceId, DateTime when);
        bool ToggleApplicationStatus(string applicationId, string userId, string key, bool active);
        IEnumerable<T> GetApplications<T>(string userId, string orderKey, string statusKey, Func<string, string, int, T> resultor);
        void SortApplications(string userId, string orderKey, string[] applicationIds);
        IEnumerable<TK> GetErrorsStats<TK>(string[] applicationIds, Func<string, string, DateTime, int, TK> selector);
        void Banner(Action<string> func);
    }

    public interface IApplicationsPersistorFactory
    {
        IApplicationsPersistor Build();
    }
}