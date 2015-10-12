namespace ElmahR.Persistence.RavenDB
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Persistors;
    using Core.Config;

    #endregion

    class RavenDBPersistor : IApplicationsPersistor
    {
        public void Banner(Action<string> func)
        {
            func(ToString());
        }

        public void Init(RootSection rootSection)
        {
        }

        public void AddError(Error error, string errorId)
        {
            throw new NotImplementedException();
        }

        public Error GetError(string id, IApplications applications)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Error> GetErrors(int count, string[] activeApps, IApplications applications)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Error> GetErrors(int count, string beforeId, string[] activeApps, IApplications applications)
        {
            throw new NotImplementedException();
        }

        public ErrorsResume GetErrorsResume(string sourceId)
        {
            throw new NotImplementedException();
        }

        public void ClearErrorsBefore(string sourceId, DateTime when)
        {
            throw new NotImplementedException();
        }

        public void ClearErrorsAfter(string sourceId, DateTime when)
        {
            throw new NotImplementedException();
        }

        public bool ToggleApplicationStatus(string applicationId, string userId, string key, bool active)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetApplications<T>(string userId, string orderKey, string statusKey, Func<string, string, int, T> resultor)
        {
            throw new NotImplementedException();
        }

        public void SortApplications(string userId, string orderKey, string[] applicationIds)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TK> GetErrorsStats<TK>(string[] applicationIds, Func<string, string, DateTime, int, TK> selector)
        {
            throw new NotImplementedException();
        }
    }
}
