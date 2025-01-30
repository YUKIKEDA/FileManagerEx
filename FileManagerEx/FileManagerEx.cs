namespace FileManagerEx
{
    public static class FileManagerEx
    {
        /// <summary>
        /// ディレクトリを再帰的にコピーします
        /// </summary>
        /// <param name="sourceDirPath">コピー元ディレクトリのパス</param>
        /// <param name="destinationDirPath">コピー先ディレクトリのパス</param>
        /// <param name="overwrite">既存のファイルを上書きするかどうか</param>
        public static void CopyDirectory(string sourceDirPath, string destinationDirPath, bool overwrite = false)
        {
            // コピー元ディレクトリが存在しない場合は例外をスロー
            if (!Directory.Exists(sourceDirPath))
            {
                throw new DirectoryNotFoundException($"コピー元ディレクトリが見つかりません: {sourceDirPath}");
            }

            // コピー先ディレクトリが存在しない場合は作成
            if (!Directory.Exists(destinationDirPath))
            {
                Directory.CreateDirectory(destinationDirPath);
            }

            // ファイルをコピー
            foreach (string filePath in Directory.GetFiles(sourceDirPath))
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destinationDirPath, fileName);
                File.Copy(filePath, destFilePath, overwrite);
            }

            // サブディレクトリを再帰的にコピー
            foreach (string dirPath in Directory.GetDirectories(sourceDirPath))
            {
                string dirName = Path.GetFileName(dirPath);
                string destDirPath = Path.Combine(destinationDirPath, dirName);
                CopyDirectory(dirPath, destDirPath, overwrite);
            }
        }

        /// <summary>
        /// ディレクトリを非同期で再帰的にコピーします
        /// </summary>
        /// <param name="sourceDirPath">コピー元ディレクトリのパス</param>
        /// <param name="destinationDirPath">コピー先ディレクトリのパス</param>
        /// <param name="overwrite">既存のファイルを上書きするかどうか</param>
        /// <returns>コピー処理の完了を表すTask</returns>
        public static async Task CopyDirectoryAsync(string sourceDirPath, string destinationDirPath, bool overwrite = false)
        {
            // コピー元ディレクトリが存在しない場合は例外をスロー
            if (!Directory.Exists(sourceDirPath))
            {
                throw new DirectoryNotFoundException($"コピー元ディレクトリが見つかりません: {sourceDirPath}");
            }

            // コピー先ディレクトリが存在しない場合は作成
            if (!Directory.Exists(destinationDirPath))
            {
                Directory.CreateDirectory(destinationDirPath);
            }

            // ファイルを非同期でコピー
            var fileCopyTasks = Directory.GetFiles(sourceDirPath)
                .Select(async filePath =>
                {
                    string fileName = Path.GetFileName(filePath);
                    string destFilePath = Path.Combine(destinationDirPath, fileName);
                    using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (var destStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await sourceStream.CopyToAsync(destStream);
                    }
                });

            // すべてのファイルコピーの完了を待機
            await Task.WhenAll(fileCopyTasks);

            // サブディレクトリを非同期で再帰的にコピー
            var dirCopyTasks = Directory.GetDirectories(sourceDirPath)
                .Select(dirPath =>
                {
                    string dirName = Path.GetFileName(dirPath);
                    string destDirPath = Path.Combine(destinationDirPath, dirName);
                    return CopyDirectoryAsync(dirPath, destDirPath, overwrite);
                });

            // すべてのディレクトリコピーの完了を待機
            await Task.WhenAll(dirCopyTasks);
        }
    }
}
