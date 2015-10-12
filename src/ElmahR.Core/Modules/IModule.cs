namespace ElmahR.Core.Modules
{
    #region Imports

    using System;
    using System.Collections.Generic;

    #endregion

    public interface IModule
    {
        IEnumerable<TR> DefineScripts<TF, TR>(string version, Func<string, string, string> cndRenamer, Func<string, string, TF> fileDescriptor,
                                              Func<string, IEnumerable<TF>, TR> resultor);

        IEnumerable<TR> DefineStyles<TF, TR>(string version, Func<string, string, string> cndRenamer, Func<string, string, TF> fileDescriptor,
                                             Func<string, IEnumerable<TF>, TR> resultor);

        string Name { get; }
    }
}   