namespace Cave
{
    /// <summary>
    /// Provides an interface for structs / objects supporting the ToXT() method.
    /// </summary>
    public interface IXT
    {
        /// <summary>Provides an eXtended Text string for this object.</summary>
        /// <returns>Returns a new XT instance with the description of this object.</returns>
        XT ToXT();
    }
}
