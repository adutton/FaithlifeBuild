namespace Faithlife.Build
{
	/// <summary>
	/// The settings to use when running MSBuild.
	/// </summary>
	public sealed class MSBuildSettings
	{
		/// <summary>
		/// Specifies the <c>MSBuild</c> version to use.
		/// </summary>
		public MSBuildVersion? Version { get; set; }

		/// <summary>
		/// Specifies the <c>MSBuild</c> platform to use.
		/// </summary>
		public MSBuildPlatform? Platform { get; set; }
	}
}
