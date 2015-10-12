namespace ElmahR.Persistence.EntityFramework
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using System.Data.Entity.ModelConfiguration;
    using MoreLinq;
    using Core;
    using Core.Persistors;
    using Core.Dependencies;
    using Core.Config;
    using Core.Extensions;

    #endregion

    class UserSettingConfiguration : EntityTypeConfiguration<UserSetting>
    {
        public UserSettingConfiguration()
        {
            HasKey(k => new { k.UserId, k.Key, k.Specifier });
        }
    }

    static class ErrorLogContextFactory
    {
        public static ErrorLogContext Create(bool disableEntityFrameworkMigrations)
        {
            var context = new ErrorLogContext
            {
                DisableEntityFrameworkMigrations = disableEntityFrameworkMigrations
            };

            return context;
        }
    }

    class ErrorLogContext : DbContext
    {
        public DbSet<ErrorLogEntry> Entries { get; set; }
        public DbSet<UserSetting> Settings { get; set; }

        internal bool DisableEntityFrameworkMigrations { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            if (!DisableEntityFrameworkMigrations)
                Database.SetInitializer(new MigrateDatabaseToLatestVersion<ErrorLogContext, Setup>());

            modelBuilder.Configurations.Add(new UserSettingConfiguration());
        }

        private class Setup : DbMigrationsConfiguration<ErrorLogContext>
        {
            public Setup()
            {
                AutomaticMigrationsEnabled = true;
                AutomaticMigrationDataLossAllowed = true;
            }
        }
    }

    [Export(typeof(IPersistorTypeFinder))]
    public class PersistorTypeFinder : IPersistorTypeFinder
    {
        public Type Find()
        {
            return typeof (EntityFrameworkPersistor);
        }
    }

    public class EntityFrameworkPersistor : IApplicationsPersistor
    {
        private bool _disableEntityFrameworkMigrations;

        public void Init(RootSection rootSection)
        {
            var disableEntityFrameworkMigrations = rootSection == null || rootSection.GetProperty("disableEntityFrameworkMigrations").IsTruthy();

            _disableEntityFrameworkMigrations = disableEntityFrameworkMigrations;
        }

        public void Banner(Action<string> func)
        {
            func(ToString());
        }

        public void AddError(Error error, string errorId)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                context.Entries.Add(new ErrorLogEntry
                {
                    Key = errorId,
                    Body = error.GetJsonError(),
                    ApplicationId = error.GetApplication().SourceId,
                    Type = error.Type,
                    ReceivedAt = error.Time
                });
                context.SaveChanges();
            }
        }

        public Error GetError(string id, IApplications applications)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                var entries = from e in context.Entries
                              where e.Key == id
                              select e;

                var entry = entries.FirstOrDefault();
                if (entry == null)
                    throw new ArgumentException(string.Format("Id not found: {0}", id));

                var built = Error.Build(entry.Body, entry.Key, applications[entry.ApplicationId]);

                return built;
            }
        }

        public IEnumerable<Error> GetErrors(int count, string[] activeApps, IApplications applications)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                var entries = from e in context.Entries
                              orderby e.Id descending
                              select e;

                foreach (var entry in entries.Where(e => activeApps.Contains(e.ApplicationId))
                                             .Take(count))
                {
                    var built = Error.Build(entry.Body, entry.Key, applications[entry.ApplicationId]);
                    yield return built;
                }
            }
        }

        public IEnumerable<Error> GetErrors(int count, string beforeId, string[] activeApps, IApplications applications)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                var last = (
                    from e in context.Entries
                    where e.Key == beforeId
                    select e).FirstOrDefault();
                if (last == null)
                    yield break;

                var entries =
                    from e in context.Entries
                    where e.Key != last.Key && e.ReceivedAt <= last.ReceivedAt
                    orderby e.Id descending
                    select e;

                foreach (var entry in entries.Where(e => activeApps.Contains(e.ApplicationId))
                                             .Take(count))
                {
                    var built = Error.Build(entry.Body, entry.Key, applications[entry.ApplicationId]);
                    yield return built;
                }
            }
        }

        public ErrorsResume GetErrorsResume(string sourceId)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                var entries = from e in context.Entries
                              where e.ApplicationId == sourceId && e.ReceivedAt.HasValue
                              orderby e.ReceivedAt descending
                              select e;
                var last = entries.FirstOrDefault();
                if (last == null || !last.ReceivedAt.HasValue)
                    return null;

                var lastDate = last.ReceivedAt.Value.Date;
                var fullBarrier = BuildErrorsBarrier(context, sourceId, lastDate, 1);

                return ErrorsResume.Create(lastDate, fullBarrier.CountBefore,
                    new[]
                    {
                        BuildErrorsBarrier(context, sourceId, lastDate, 0),
                        BuildErrorsBarrier(context, sourceId, lastDate, -7),
                        BuildErrorsBarrier(context, sourceId, lastDate, -30)
                    });
            }
        }

        private static ErrorsBarrier BuildErrorsBarrier(ErrorLogContext context, string sourceId, DateTime baseDate, int offset)
        {
            var lastDate = baseDate.AddDays(offset);

            var after = context.Entries.Count(e => e.ApplicationId == sourceId && e.ReceivedAt >= lastDate);
            var before = context.Entries.Count(e => e.ApplicationId == sourceId && e.ReceivedAt < lastDate);

            return ErrorsBarrier.Create(lastDate, offset, before, after);
        }


        public void ClearErrorsBefore(string sourceId, DateTime when)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                context.Database.ExecuteSqlCommand(@"
                    DELETE ErrorLogEntries 
                    WHERE ApplicationId =  {0} 
                    AND ReceivedAt      <  {1}", sourceId, when);
            }
        }

        public void ClearErrorsAfter(string sourceId, DateTime when)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                context.Database.ExecuteSqlCommand(@"
                    DELETE ErrorLogEntries 
                    WHERE ApplicationId =  {0} 
                    AND ReceivedAt      >= {1}", sourceId, when);
            }
        }

        public bool ToggleApplicationStatus(string applicationId, string userId, string key, bool active)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                var setting = (from s in context.Settings
                               where s.UserId == userId
                                  && s.Key == key
                                  && s.Specifier == applicationId
                               select s).FirstOrDefault();

                var value = active ? "true" : "false";
                if (setting == null)
                {
                    setting = new UserSetting
                    {
                        UserId = userId,
                        Key = key,
                        Specifier = applicationId,
                        Value = value
                    };
                    context.Settings.Add(setting);
                }
                else
                    setting.Value = value;

                context.SaveChanges();

                return active;
            }
        }

        public IEnumerable<T> GetApplications<T>(string userId, string orderKey, string statusKey, Func<string, string, int, T> resultor)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                var sortSetting = (
                    from s in context.Settings
                    where s.UserId == userId
                       && s.Key == orderKey
                    select s.Value)
                    .FirstOrDefault();

                var sortedAppIds = (
                    sortSetting == null
                    ? new string[] { }
                    : sortSetting.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .Index();

                var apps = (
                    from s in context.Settings
                    where s.UserId == userId
                       && s.Key == statusKey
                    select new { Index = 0, s.Specifier, s.Value })
                    .ToArray();

                apps = (
                    from s in apps
                    join a in sortedAppIds
                    on s.Specifier equals a.Value into ss
                    from x in ss.DefaultIfEmpty()
                    select new { Index = x.Key, s.Specifier, s.Value })
                    .OrderBy(x => x.Index)
                    .ToArray();

                foreach (var app in from a in apps
                                    select resultor(a.Specifier, a.Value, a.Index))
                    yield return app;
            }
        }

        public void SortApplications(string userId, string orderKey, string[] applicationIds)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                var setting = (from s in context.Settings
                               where s.UserId == userId
                                  && s.Key == orderKey
                                  && s.Specifier == "*"
                               select s).FirstOrDefault();

                var value = string.Join(",", applicationIds);
                if (setting == null)
                {
                    setting = new UserSetting
                    {
                        UserId = userId,
                        Key = orderKey,
                        Specifier = "*",
                        Value = value
                    };
                    context.Settings.Add(setting);
                }
                else
                    setting.Value = value;

                context.SaveChanges();
            }
        }

        public IEnumerable<TK> GetErrorsStats<TK>(string[] applicationIds, Func<string, string, DateTime, int, TK> selector)
        {
            using (var context = ErrorLogContextFactory.Create(_disableEntityFrameworkMigrations))
            {
                var stats =
                    from e in context.Entries
                    where applicationIds.Contains(e.ApplicationId) && e.ReceivedAt.HasValue
                    group e by new { e.ApplicationId, e.Type, e.ReceivedAt.Value.Year, e.ReceivedAt.Value.Month, e.ReceivedAt.Value.Day }
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
}