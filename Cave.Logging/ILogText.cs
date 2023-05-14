namespace Cave.Logging;

/// <summary>Provides an interface for structs / objects supporting the ToLogText() method.</summary>
public interface ILogText
{
    #region Members

    /// <summary>Provides an eXtended Text string for this object.</summary>
    /// <returns>Returns a new LogText instance with the description of this object.</returns>
    LogText ToLogText();

    #endregion Members
}
