namespace ElmahR.Core.Persistors
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MoreLinq;
    using Config;

    #endregion

    class UserSetting
    {
        public string UserId { get; set; }
        public string Key { get; set; }
        public string Specifier { get; set; }
        public string Value { get; set; }
    }

    public class InMemoryPersistor : IApplicationsPersistor
    {
        private readonly List<Error> _errors = new List<Error>();
        private readonly Dictionary<string, Error> _errorsById = new Dictionary<string, Error>();

        private readonly Dictionary<string, UserSetting> _settingsById = new Dictionary<string, UserSetting>();

        public void Banner(Action<string> func)
        {
            func(string.Format("{0} - this persistor is volatile, if you need a durable one please check ElmahR.Persistence.* modules or Nuget packages", ToString()));
        }

        public void Init(RootSection rootSection)
        {
        }

        public void AddError(Error error, string errorId)
        {
            lock (_errors)
            {
                _errors.Add(error);
                _errorsById.Add(errorId, error);
            }
        }

        public Error GetError(string id, IApplications applications)
        {
            return _errorsById.ContainsKey(id) ? _errorsById[id] : null;
        }

        public IEnumerable<Error> GetErrors(int count, string[] activeApps, IApplications applications)
        {
            return (from e in
                        from ue in _errors.Index()
                        where activeApps.Contains(ue.Value.GetApplication().SourceId)
                        orderby ue.Value.Time descending
                        select ue
                    select e.Value).Take(count);
        }

        public IEnumerable<Error> GetErrors(int count, string beforeId, string[] activeApps, IApplications applications)
        {
            if (!_errorsById.ContainsKey(beforeId))
                yield break;

            var last = _errorsById[beforeId];

            foreach (var error in (from ue in _errors
                                   where activeApps.Contains(ue.GetApplication().SourceId)
                                   orderby ue.Time descending
                                   select ue into x
                                   where x.Time <= last.Time && x.Id != beforeId
                                   select x).Take(count))
                yield return error;
        }

        public ErrorsResume GetErrorsResume(string sourceId)
        {
            var entries = from e in _errors
                          where e.GetApplication().SourceId == sourceId
                          orderby e.Time descending 
                          select e;
            var last = entries.FirstOrDefault();
            if (last == null)
                return null;

            var lastDate = last.Time.Date;
            var fullBarrier = BuildErrorsBarrier(_errors, sourceId, lastDate, 1);

            return ErrorsResume.Create(lastDate, fullBarrier.CountBefore,
                new[]
                    {
                        BuildErrorsBarrier(_errors, sourceId, lastDate, 0),
                        BuildErrorsBarrier(_errors, sourceId, lastDate, -7),
                        BuildErrorsBarrier(_errors, sourceId, lastDate, -30)
                    });
        }

        private static ErrorsBarrier BuildErrorsBarrier(IList<Error> context, string sourceId, DateTime baseDate, int offset)
        {
            var lastDate = baseDate.AddDays(offset);

            var after = context.Count(e => e.GetApplication().SourceId == sourceId && e.Time >= lastDate);
            var before = context.Count(e => e.GetApplication().SourceId == sourceId && e.Time < lastDate);

            return ErrorsBarrier.Create(lastDate, offset, before, after);
        }

        public void ClearErrorsBefore(string sourceId, DateTime when)
        {
            lock (_errors)
            {
                var temp = _errors.Where(e => e.GetApplication().SourceId == sourceId && e.Time < when).ToArray();
                foreach (var error in temp)
                {
                    _errors.Remove(error);
                    _errorsById.Remove(error.Id);
                }
            }
        }

        public void ClearErrorsAfter(string sourceId, DateTime when)
        {
            lock (_errors)
            {
                var temp = _errors.Where(e => e.GetApplication().SourceId == sourceId && e.Time >= when).ToArray();
                foreach (var error in temp)
                {
                    _errors.Remove(error);
                    _errorsById.Remove(error.Id);
                }
            }
        }

        public bool ToggleApplicationStatus(string applicationId, string userId, string key, bool active)
        {
            var setting = (from s in _settingsById.Values
                            where s.UserId == userId
                                && s.Key == key
                                && s.Specifier == applicationId
                            select s).FirstOrDefault();

            var value = active ? "true" : "false";
            if (setting == null)
            {
                lock (_settingsById)
                {
                    setting = new UserSetting
                    {
                        UserId = userId,
                        Key = key,
                        Specifier = applicationId,
                        Value = value
                    };
                    _settingsById.Add(string.Format("{0}/{1}/{2}", userId, key, applicationId), setting);
                }
            }
            else
                setting.Value = value;
            
            return active;
        }

        public IEnumerable<T> GetApplications<T>(string userId, string orderKey, string statusKey, Func<string, string, int, T> resultor)
        {
            var sortSetting = (
                    from s in _settingsById.Values
                    where s.UserId == userId
                       && s.Key == orderKey
                    select s.Value)
                    .FirstOrDefault();

            var sortedAppIds = (
                sortSetting == null
                ? new string[] { }
                : sortSetting.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Index();

            var apps =
                from s in _settingsById.Values
                where s.UserId == userId
                   && s.Key == statusKey
                select new { Index = 0, s.Specifier, s.Value };

            apps = apps.ToArray();

            apps = 
                from s in apps
                join a in sortedAppIds
                on s.Specifier equals a.Value into ss
                from x in ss.DefaultIfEmpty()
                orderby x.Key
                select new { Index = x.Key, s.Specifier, s.Value };

            apps = apps.ToArray();

            return from a in apps
                   select resultor(a.Specifier, a.Value, a.Index);
        }

        public void SortApplications(string userId, string orderKey, string[] applicationIds)
        {
            var setting = (from s in _settingsById.Values
                           where s.UserId == userId
                              && s.Key == orderKey
                              && s.Specifier == "*"
                           select s).FirstOrDefault();

            var value = string.Join(",", applicationIds);
            if (setting == null)
            {
                lock (_settingsById)
                {
                    setting = new UserSetting
                        {
                            UserId = userId,
                            Key = orderKey,
                            Specifier = "*",
                            Value = value
                        };
                    _settingsById.Add(string.Format("{0}/{1}/{2}", userId, orderKey, "*"), setting);
                }
            }
            else
                setting.Value = value;
        }

        public IEnumerable<TK> GetErrorsStats<TK>(string[] applicationIds, Func<string, string, DateTime, int, TK> selector)
        {
            var stats =
                from e in _errors
                where applicationIds.Contains(e.GetApplication().SourceId)
                group e by new { ApplicationId = e.GetApplication().SourceId, e.Type, e.Time.Year, e.Time.Month, e.Time.Day }
                into g
                select new
                {
                    g.Key,
                    Count = g.Count()
                };

            return stats.ToArray().Select(s => selector(s.Key.ApplicationId, s.Key.Type, new DateTime(s.Key.Year, s.Key.Month, s.Key.Day), s.Count));
        }
    }
}