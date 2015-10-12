namespace ElmahR.Core.Services
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using Persistors;

    #endregion

    public class UserService : IUserService
    {
        const string ApplicationStatusKey = "ApplicationStatus";
        const string ApplicationsOrderKey = "ApplicationsOrder";

        private readonly IApplicationsPersistor _persistor;

        public UserService(IApplicationsPersistorFactory persistor)
        {
            _persistor = persistor.Build();
        }

        public bool GetRememberMeStatus(string userKey)
        {
            return userKey != null;
        }

        public bool ToggleApplicationStatus(string userKey, string applicationId, bool active)
        {
            if (userKey == null)
                return active;

            return _persistor.ToggleApplicationStatus(applicationId, userKey, ApplicationStatusKey, active);
        }

        public IEnumerable<T> GetApplications<T>(string userKey, Func<string, string, int, T> resultor)
        {
            if (userKey == null)
                yield break;

            foreach (var application in _persistor.GetApplications(userKey, ApplicationsOrderKey, ApplicationStatusKey, resultor))
                yield return application;
        }

        public void SortApplications(string userKey, string[] applicationIds)
        {
            if (userKey == null)
                return;

            _persistor.SortApplications(userKey, ApplicationsOrderKey, applicationIds);
        }
    }
}