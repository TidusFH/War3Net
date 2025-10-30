// ------------------------------------------------------------------------------
// <copyright file="CustomMpqArchiveBuilder.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using War3Net.IO.Mpq;

namespace War3Net.Tools.TriggerMerger.Services
{
    /// <summary>
    /// Custom MpqArchiveBuilder that handles duplicate filenames correctly.
    /// When there are duplicate files (same hashed name), keeps the FIRST occurrence,
    /// which comes from _modifiedFiles (our new/modified files).
    /// </summary>
    internal class CustomMpqArchiveBuilder : MpqArchiveBuilder
    {
        public CustomMpqArchiveBuilder(MpqArchive archive)
            : base(archive)
        {
        }

        /// <summary>
        /// Override to deduplicate files by name hash, keeping first occurrence.
        /// Since _modifiedFiles.Concat(_originalFiles) puts modified files first,
        /// this ensures our new files take precedence over original files.
        /// </summary>
        protected override IEnumerable<MpqFile> GetMpqFiles()
        {
            // Get all files from base implementation
            var allFiles = base.GetMpqFiles();

            // Remove duplicates by file name hash, keeping first occurrence
            // Since _modifiedFiles comes before _originalFiles in the concatenation,
            // this keeps our newly added/modified files and discards the originals
            return allFiles
                .GroupBy(file => file.Name)
                .Select(group => group.First());
        }
    }
}
