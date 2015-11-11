namespace ccnet.EnhancedBuildPublisher.plugin
{
    using System.Globalization;
    using System.IO;
    using Exortech.NetReflector;
    using ThoughtWorks.CruiseControl.Core.Tasks;
	using ThoughtWorks.CruiseControl.Core.Util;
	using ThoughtWorks.CruiseControl.Core;

    /// <summary>
    /// <para>
    /// The Build Publisher lets you copy any arbitrary files on a <b>successful</b> build.
    /// </para>
    /// <para>
    /// You can set alwaysPublish to true, if you want the copy always to happen.
    /// </para>
    /// </summary>
    /// <title>Enhanced Build Publisher</title>
    /// <version>1.0</version>
    /// <example>
    /// <code title="Minimalist example">
    /// &lt;buildpublisher /&gt;
    /// </code>
    /// <para>
    /// This will copy the contents of the project's working directory to a new label subdirectory under the
    /// project's artifact directory (i.e. &lt;artifact_dir&gt;\&lt;label_dir&gt;) 
    /// </para>
    /// <code title="Full example">
    /// &lt;buildpublisher&gt;
    /// &lt;sourceDir&gt;C:\myprojects\project1&lt;/sourceDir&gt;
    /// &lt;publishDir&gt;\\myfileserver\project1&lt;/publishDir&gt;
    /// &lt;useLabelSubDirectory&gt;false&lt;/useLabelSubDirectory&gt;
    /// &lt;alwaysPublish&gt;false&lt;/alwaysPublish&gt;
	/// &lt;recurse&gt;true&lt;/recurse&gt;
	/// &lt;/buildpublisher&gt;
    /// </code>
    /// <para>
    /// This will copy the contents of <b>C:\myprojects\project1</b> to the network share 
    /// <b>\\myfileserver\project1</b>. 
    /// </para>
	/// <para>
	/// The EnhancedBuildPublisher adds the "recurse" option.  By default it is true, allowing the
	/// EnhancedBuildPublisher to work exactly like the standard BuildPublisher.  By setting "recurse" to
	/// "false", only the files in the top level folder will be published, ignoring any sub-folders.
	/// </para>
    /// </example>
    [ReflectorType("enhancedbuildpublisher")]
    public class EnhancedBuildPublisher
        : TaskBase
    {
        /// <summary>
        /// 	
        /// </summary>
        public enum CleanupPolicy
        {
            /// <summary>
            /// No cleaning done 
            /// </summary>
            NoCleaning,
            /// <summary>
            /// Keep the last X published builds
            /// </summary>
            KeepLastXBuilds,
            /// <summary>
            /// Delete builds older than X days
            /// </summary>
            DeleteBuildsOlderThanXDays
        }

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildPublisher"/> class.
        /// </summary>
		public EnhancedBuildPublisher()
        {
            this.UseLabelSubDirectory = true;
            this.AlwaysPublish = false;
            this.CleanPublishDirPriorToCopy = false;
            this.CleanUpMethod = CleanupPolicy.NoCleaning;
            this.CleanUpValue = 5;
			this.Recurse = true;
        }
        #endregion

        /// <summary>
        /// The directory to copy the files to. This path can be absolute or can be relative to the project's
        /// artifact directory. If <b>useLabelSubDirectory</b> is true (default) a subdirectory with the
        /// current build's label will be created, and the contents of sourceDir will be copied to it. If
        /// unspecified, the project's artifact directory will be used as the publish directory.
        /// </summary>
        /// <version>1.0</version>
        /// <default>n/a</default>
        [ReflectorProperty("publishDir", Required = false)]
        public string PublishDir { get; set; }

        /// <summary>
        /// The source directory to copy files from. This path can be absolute or can be relative to the
        /// project's working directory. If unspecified, the project's working directory will be used as the
        /// source directory.
        /// </summary>
        /// <version>1.0</version>
        /// <default>n/a</default>
        [ReflectorProperty("sourceDir", Required = false)]
        public string SourceDir { get; set; }

        /// <summary>
        /// If set to true (the default value), files will be copied to subdirectory under the publishDir which
        /// will be named with the label for the current integration.
        /// </summary>
        /// <version>1.0</version>
        /// <default>true</default>
        [ReflectorProperty("useLabelSubDirectory", Required = false)]
        public bool UseLabelSubDirectory { get; set; }

        /// <summary>
        /// Always copies the files, regardless of the state of the build.
        /// </summary>
        /// <version>1.0</version>
        /// <default>false</default>
        [ReflectorProperty("alwaysPublish", Required = false)]
        public bool AlwaysPublish { get; set; }

        /// <summary>
        /// Cleans the publishDir if it exists, so that you will always have an exact copy of the sourceDir.
        /// </summary>
        /// <version>1.5</version>
        /// <default>false</default>
        [ReflectorProperty("cleanPublishDirPriorToCopy", Required = false)]
        public bool CleanPublishDirPriorToCopy { get; set; }

        /// <summary>
        /// Defines a way to clean up published builds.
        /// </summary>
        /// <version>1.4.4</version>
        /// <default>NoClean</default>
        [ReflectorProperty("cleanUpMethod", Required = false)]
        public CleanupPolicy CleanUpMethod { get; set; }

        /// <summary>
        /// The value used for the cleaning method.
        /// </summary>
        /// <version>1.4.4</version>
        /// <default>5</default>
        [ReflectorProperty("cleanUpValue", Required = false)]
        public int CleanUpValue { get; set; }

		[ReflectorProperty( "recurse", Required = false )]
		public bool Recurse { get; set; }

        /// <summary>
        /// Execute the actual task functionality.
        /// </summary>
        /// <param name="result"></param>
        /// <returns>
        /// True if the task was successful, false otherwise.
        /// </returns>
        protected override bool Execute(IIntegrationResult result)
        {

            result.BuildProgressInformation.SignalStartRunTask(!string.IsNullOrEmpty(Description) ? Description : "Publishing build results");

            if (result.Succeeded || AlwaysPublish)
            {
                var srcDir = new DirectoryInfo(result.BaseFromWorkingDirectory(SourceDir));
                var pubDir = new DirectoryInfo(result.BaseFromArtifactsDirectory(PublishDir));
                Log.Debug("Publish directory is '{0}'", pubDir.FullName);
                Log.Debug("Source directory is '{0}'", srcDir.FullName);
                if (!srcDir.Exists)
                {
                    Log.Warning("Source directory '{0}' does not exist - cancelling task", srcDir.FullName);
                    var errorResult = new GeneralTaskResult(
                        false,
                        "Unable to find source directory '" + srcDir.FullName + "'");
                    result.AddTaskResult(errorResult);
                    return false;
                }

                if (!pubDir.Exists)
                {
                    Log.Info("Publish directory '{0}' does not exist - creating", pubDir.FullName);
                    pubDir.Create();
                }
                else
                {
                    if (CleanPublishDirPriorToCopy)
                    {
                        DeleteFolder(pubDir.FullName);
                        pubDir.Create();
                    }
                }

                if (UseLabelSubDirectory)
                    pubDir = pubDir.CreateSubdirectory(result.Label);

                RecurseSubDirectories(srcDir, pubDir, this.Recurse);

                switch (CleanUpMethod)
                {
                    case CleanupPolicy.NoCleaning:
                        break;

                    case CleanupPolicy.DeleteBuildsOlderThanXDays:
                        DeleteSubDirsOlderThanXDays(new DirectoryInfo(result.BaseFromArtifactsDirectory(PublishDir)).FullName,
                                                    CleanUpValue, result.BuildLogDirectory);
                        break;

                    case CleanupPolicy.KeepLastXBuilds:
                        KeepLastXSubDirs(new DirectoryInfo(result.BaseFromArtifactsDirectory(PublishDir)).FullName,
                                                    CleanUpValue, result.BuildLogDirectory);
                        break;

                    default:
                        throw new System.Exception(string.Format(System.Globalization.CultureInfo.CurrentCulture, "unmapped cleaning method choosen {0}", CleanUpMethod));
                }
            }

            return true;
        }


        /// <summary>
        /// Copies all files and folders from srcDir to pubDir
        /// </summary>
        /// <param name="srcDir">The SRC dir.</param>
        /// <param name="pubDir">The pub dir.</param>
        private static void RecurseSubDirectories(DirectoryInfo srcDir, DirectoryInfo pubDir, bool recurse)
        {
            var files = srcDir.GetFiles();
            foreach (var file in files)
            {
                var destFile = new FileInfo(Path.Combine(pubDir.FullName, file.Name));
                if (destFile.Exists) destFile.Attributes = FileAttributes.Normal;
                file.CopyTo(destFile.ToString(), true);
            }

			if ( recurse )
			{
				var subDirectories = srcDir.GetDirectories();
				foreach ( var subDir in subDirectories )
				{
					var subDestination = pubDir.CreateSubdirectory( subDir.Name );
					RecurseSubDirectories( subDir, subDestination, recurse );
				}
			}
        }

        /// <summary>
        /// Keeps the last X sub dirs.
        /// </summary>
        /// <param name="targetFolder">The target folder.</param>
        /// <param name="amountToKeep">The amount to keep.</param>
        /// <param name="buildLogDirectory">The build log directory.</param>
        private void KeepLastXSubDirs(string targetFolder, int amountToKeep, string buildLogDirectory)
        {
            Log.Trace("Deleting Subdirs of {0}", targetFolder);

            var sortNames = new System.Collections.Generic.List<string>();
            const string dateFormat = "yyyyMMddHHmmssffffff";

            foreach (var folder in Directory.GetDirectories(targetFolder))
            {
                if (folder != buildLogDirectory)
                    sortNames.Add(Directory.GetCreationTime(folder).ToString(dateFormat, CultureInfo.CurrentCulture) + folder);
            }

            sortNames.Sort();
            var amountToDelete = sortNames.Count - amountToKeep;
            for (var i = 0; i < amountToDelete; i++)
            {
                DeleteFolder(sortNames[0].Substring(dateFormat.Length));
                sortNames.RemoveAt(0);
            }
        }

        private void DeleteSubDirsOlderThanXDays(string targetFolder, int daysToKeep, string buildLogDirectory)
        {
            Log.Trace("Deleting Subdirs of {0}", targetFolder);
            var cutoffDate = System.DateTime.Now.Date.AddDays(-daysToKeep);
            foreach (var folder in Directory.GetDirectories(targetFolder))
            {
                if ((Directory.GetCreationTime(folder).Date < cutoffDate) &&
                    (folder != buildLogDirectory))
                    DeleteFolder(folder);
            }
        }

        private void DeleteFolder(string folderName)
        {
            Log.Trace("Deleting {0}", folderName);
            SetFilesToNormalAttributeAndDelete(folderName);
            Directory.Delete(folderName);
        }

        private void SetFilesToNormalAttributeAndDelete(string folderName)
        {
            foreach (var file in Directory.GetFiles(folderName))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var subFolder in Directory.GetDirectories(folderName))
            {
                DeleteFolder(subFolder);
            }
        }



    }
}