namespace ElmahR.Core.Services
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using Microsoft.AspNet.SignalR;

    #endregion

    public interface IUserService
    {
        bool GetRememberMeStatus(string userKey);
        bool ToggleApplicationStatus(string userKey, string applicationId, bool active);
        IEnumerable<T> GetApplications<T>(string userKey, Func<string, string, int, T> resultor);
        void SortApplications(string userKey, string[] applicationIds);
    }
}