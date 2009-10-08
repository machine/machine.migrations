using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Machine.Core.Services;

namespace Machine.Migrations.Services.Impl
{
  public class MigrationFinder : IMigrationFinder
  {
    #region Member Data
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MigrationFinder));
    readonly Regex _regex = new Regex(@"^(\d+)_([\w_]+)\.(cs|boo|sql)$");
    readonly IConfiguration _configuration;
    readonly IFileSystem _fileSystem;
    readonly INamer _namer;
    #endregion

    #region MigrationFinder()
    public MigrationFinder(IFileSystem fileSystem, INamer namer, IConfiguration configuration)
    {
      _fileSystem = fileSystem;
      _namer = namer;
      _configuration = configuration;
    }
    #endregion

    #region IMigrationFinder Members
    public ICollection<MigrationReference> FindMigrations()
    {
      var migrations = new Dictionary<long, MigrationReference>();
      foreach (string file in _fileSystem.GetFiles(_configuration.MigrationsDirectory))
      {
        Match m = _regex.Match(Path.GetFileName(file));
        if (m.Success)
        {
          var migration = new MigrationReference(Int64.Parse(m.Groups[1].Value), _namer.ToCamelCase(m.Groups[2].Value), file);

          if (migrations.ContainsKey(migration.Version))
            throw new DuplicateMigrationVersionException("Duplicate Version " + migration.Version);

          migrations.Add(migration.Version, migration);
        }
      }
      var sortedMigrations = new List<MigrationReference>(migrations.Values);
      sortedMigrations.Sort((mr1, mr2) => mr1.Version.CompareTo(mr2.Version));

      if (sortedMigrations.Count == 0)
      {
        _log.InfoFormat("Found {0} migrations in '{1}'!", migrations.Count, _configuration.MigrationsDirectory);
      }
      return sortedMigrations;
    }
    #endregion
  }
}