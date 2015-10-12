namespace ElmahR.Persistence.MongoDB
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.ComponentModel.Composition;
    using System.Linq;
    using Core;
    using Core.Persistors;
    using MoreLinq;
    using Core.Dependencies;
    using Core.Config;

    using global::MongoDB.Driver;
    using global::MongoDB.Driver.Builders;
    using global::MongoDB.Driver.Linq;
    using global::MongoDB.Bson;

    #endregion

    class ErrorLogEntry
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string ApplicationId { get; set; }
        public string Body { get; set; }
        public string Type { get; set; }
        public DateTime? ReceivedAt { get; set; }
    }

    class UserSetting
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Key { get; set; }
        public string Specifier { get; set; }
        public string Value { get; set; }
    }

    [Export(typeof(IPersistorTypeFinder))]
    public class PersistorTypeFinder : IPersistorTypeFinder
    {
        public Type Find()
        {
            return typeof(MongoDBPersistor);
        }
    }

    public class MongoDBPersistor : IApplicationsPersistor
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
            GetErrors().Insert(new ErrorLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Key = errorId,
                Body = error.GetJsonError(),
                ApplicationId = error.GetApplication().SourceId,
                Type = error.Type,
                ReceivedAt = error.Time
            });
        }

        public Error GetError(string id, IApplications applications)
        {
            var entry = (
                from e in GetErrors().AsQueryable()
                where e.Key == id
                select e).FirstOrDefault();
            if (entry == null)
                throw new ArgumentException(string.Format("Id not found: {0}", id));

            return Error.Build(entry.Body, entry.Key, applications[entry.ApplicationId]);
        }

        public IEnumerable<Error> GetErrors(int count, string[] activeApps, IApplications applications)
        {
            var entries = (
                from e in GetErrors().AsQueryable()
                orderby e.ReceivedAt descending 
                select e).ToList();

            return entries.Where(e => activeApps.Contains(e.ApplicationId))
                          .Take(count)
                          .Select(entry => Error.Build(entry.Body, entry.Key, applications[entry.ApplicationId]));
        }

        public IEnumerable<Error> GetErrors(int count, string beforeId, string[] activeApps, IApplications applications)
        {
            var last = (
                from e in GetErrors().AsQueryable()
                where e.Key == beforeId
                select e).FirstOrDefault();
            if (last == null)
                yield break;

            var entries = (
                from e in GetErrors().AsQueryable()
                where e.ReceivedAt <= last.ReceivedAt && e.Key != last.Key
                orderby e.ReceivedAt descending
                select e).ToList();

            foreach(var error in entries.Where(e => activeApps.Contains(e.ApplicationId))
                                        .Take(count)
                                        .Select(entry => Error.Build(entry.Body, entry.Key, applications[entry.ApplicationId])))
                yield return error;
        }

        public ErrorsResume GetErrorsResume(string sourceId)
        {
            var errors = GetErrors().AsQueryable();

            var entries = from e in errors
                          where e.ApplicationId == sourceId
                          orderby e.ReceivedAt descending
                          select e;
            var last = entries.FirstOrDefault();
            if (last == null || !last.ReceivedAt.HasValue)
                return null;

            var lastDate = last.ReceivedAt.Value.Date;
            var fullBarrier = BuildErrorsBarrier(errors, sourceId, lastDate, 1);

            return ErrorsResume.Create(lastDate, fullBarrier.CountBefore,
                new[]
                {
                    BuildErrorsBarrier(errors, sourceId, lastDate, 0),
                    BuildErrorsBarrier(errors, sourceId, lastDate, -7),
                    BuildErrorsBarrier(errors, sourceId, lastDate, -30)
                });
        }

        private static ErrorsBarrier BuildErrorsBarrier(IQueryable<ErrorLogEntry> context, string sourceId, DateTime baseDate, int offset)
        {
            var lastDate = baseDate.AddDays(offset);

            var after = context.Count(e => e.ApplicationId == sourceId && e.ReceivedAt >= lastDate);
            var before = context.Count(e => e.ApplicationId == sourceId && e.ReceivedAt < lastDate);

            return ErrorsBarrier.Create(lastDate, offset, before, after);
        }

        public void ClearErrorsBefore(string sourceId, DateTime when)
        {
            var errors = GetErrors();

            var query = Query.And(
                Query<ErrorLogEntry>.LT(e => e.ReceivedAt, when),
                Query<ErrorLogEntry>.EQ(e => e.ApplicationId, sourceId));

            errors.Remove(query);
        }

        public void ClearErrorsAfter(string sourceId, DateTime when)
        {
            var errors = GetErrors();

            var query = Query.And(
                Query<ErrorLogEntry>.GTE(e => e.ReceivedAt, when),
                Query<ErrorLogEntry>.EQ(e => e.ApplicationId, sourceId));

            errors.Remove(query);
        }

        public bool ToggleApplicationStatus(string applicationId, string userId, string key, bool active)
        {
            var setting = (from s in GetSettings().AsQueryable()
                           where s.UserId == userId
                              && s.Key == key
                              && s.Specifier == applicationId
                           select s).FirstOrDefault();

            var value = active ? "true" : "false";
            if (setting == null)
            {
                setting = new UserSetting
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Key = key,
                    Specifier = applicationId,
                    Value = value
                };
                GetSettings().Insert(setting);
            }
            else
            {
                setting.Value = value;
                GetSettings().Save(setting);
            }

            return active;
        }

        public IEnumerable<T> GetApplications<T>(string userId, string orderKey, string statusKey, Func<string, string, int, T> resultor)
        {
            var settings = GetSettings().AsQueryable();

            var sortSetting = (
                from s in settings
                where s.UserId == userId
                   && s.Key == orderKey
                select s.Value)
                .FirstOrDefault();

            var sortedAppIds = (
                sortSetting == null
                ? new string[] { }
                : sortSetting.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Index();

            var appsWithSettings = (
                from s in settings
                where s.UserId == userId
                   && s.Key == statusKey
                select new { Index = 0, s.Specifier, s.Value })
                .ToArray();

            var apps = 
                from s in appsWithSettings
                join a in sortedAppIds
                on s.Specifier equals a.Value into ss
                from x in ss.DefaultIfEmpty()
                select new { Index = x.Key, s.Specifier, s.Value } into w
                orderby w.Index
                select w;

            return 
                from a in apps.ToArray()
                select resultor(a.Specifier, a.Value, a.Index);
        }

        public void SortApplications(string userId, string orderKey, string[] applicationIds)
        {
            var settingsCollection = GetSettings();

            var settings =
                from s in settingsCollection.AsQueryable()
                where s.UserId == userId
                      && s.Key == orderKey
                      && s.Specifier == "*"
                select s;

            var setting = settings.FirstOrDefault();

            var value = string.Join(",", applicationIds);
            if (setting == null)
            {
                setting = new UserSetting
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Key = orderKey,
                    Specifier = "*",
                    Value = value
                };
                settingsCollection.Insert(setting);
            }
            else
            {
                setting.Value = value;
                settingsCollection.Save(setting);
            }
        }

        private static MongoCollection<ErrorLogEntry> GetErrors()
        {
            var errors = Database.GetCollection<ErrorLogEntry>("errors");
            return errors;
        }

        private static MongoCollection<UserSetting> GetSettings()
        {
            var errors = Database.GetCollection<UserSetting>("settings");
            return errors;
        }

        private static MongoDatabase Database
        {
            get
            {
                var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
                var server = MongoServer.Create(connectionString);
                var db = server.GetDatabase("errors");
                return db;
            }
        }

        public IEnumerable<TK> GetErrorsStats<TK>(string[] applicationIds, Func<string, string, DateTime, int, TK> selector)
        {
            var errors = GetErrors();

            const string 
                map = @"
                function() {
                    var e = this;
                    emit(
                    {
                        ApplicationId: e.ApplicationId, 
                        Type: e.Type, 
                        Year: e.ReceivedAt.getFullYear(), 
                        Month: e.ReceivedAt.getMonth()+1, 
                        Day: e.ReceivedAt.getDate() 
                    }, 
                    { 
                        count: 1 
                    });
                }",
                reduce = @"
                function(key, values) {
                    var count = 0;

                    values.forEach(function(v) {
                        count += v['count'];
                    });

                    return {count: count};
                }";

            var options = new MapReduceOptionsBuilder();
            options.SetOutput(MapReduceOutput.Inline);

            var stats = from BsonDocument row in errors.MapReduce(map, reduce, options).GetResults()
                        let key   = row["_id"].AsBsonDocument
                        let value = row["value"].AsBsonDocument
                        select new
                        {
                            Key = new
                            {
                                ApplicationId = key["ApplicationId"].AsString,
                                Type          = key["Type"].AsString,
                                Year          = (int)key["Year"].AsDouble,
                                Month         = (int)key["Month"].AsDouble,
                                Day           = (int)key["Day"].AsDouble
                            },
                            Count = (int)value["count"].AsDouble
                        };

            return stats.ToArray().Select(s => selector(
                s.Key.ApplicationId, 
                s.Key.Type, 
                new DateTime(s.Key.Year, s.Key.Month, s.Key.Day), 
                s.Count));
        }
    }
}
