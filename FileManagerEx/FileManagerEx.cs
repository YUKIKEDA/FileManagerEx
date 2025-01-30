namespace FileManagerEx
{
    /// <summary>
    /// ディレクトリコピーの進捗情報を表すクラス
    /// </summary>
    public class DirectoryCopyProgress
    {
        /// <summary>
        /// 処理中のファイルパス
        /// </summary>
        public string? CurrentFilePath { get; set; }

        /// <summary>
        /// 現在のファイルの進捗率（0-100）
        /// </summary>
        public int CurrentFileProgress { get; set; }

        /// <summary>
        /// 全体の進捗率（0-100）
        /// </summary>
        public int TotalProgress { get; set; }

        /// <summary>
        /// コピー済みファイル数
        /// </summary>
        public int ProcessedFiles { get; set; }

        /// <summary>
        /// 総ファイル数
        /// </summary>
        public int TotalFiles { get; set; }
    }

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

        /// <summary>
        /// 進捗表示付きでディレクトリを非同期でコピーします
        /// </summary>
        /// <param name="sourceDirPath">コピー元ディレクトリのパス</param>
        /// <param name="destinationDirPath">コピー先ディレクトリのパス</param>
        /// <param name="progress">進捗状況を報告するためのIProgress</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <param name="overwrite">既存のファイルを上書きするかどうか</param>
        /// <returns>コピー処理の完了を表すTask</returns>
        public static async Task CopyDirectoryWithProgressAsync(
            string sourceDirPath,
            string destinationDirPath,
            IProgress<DirectoryCopyProgress>? progress,
            bool overwrite = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(sourceDirPath))
            {
                throw new DirectoryNotFoundException($"コピー元ディレクトリが見つかりません: {sourceDirPath}");
            }

            // 総ファイル数を計算
            var allFiles = Directory.GetFiles(sourceDirPath, "*", SearchOption.AllDirectories);
            var totalFiles = allFiles.Length;
            var processedFiles = 0;
            var progressInfo = new DirectoryCopyProgress
            {
                TotalFiles = totalFiles,
                ProcessedFiles = 0
            };

            // コピー先ディレクトリが存在しない場合は作成
            if (!Directory.Exists(destinationDirPath))
            {
                Directory.CreateDirectory(destinationDirPath);
            }

            try
            {
                // ファイルを非同期でコピー
                foreach (var filePath in Directory.GetFiles(sourceDirPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string fileName = Path.GetFileName(filePath);
                    string destFilePath = Path.Combine(destinationDirPath, fileName);

                    progressInfo.CurrentFilePath = filePath;

                    using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (var destStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[81920]; // 80KB buffer
                        long totalBytes = sourceStream.Length;
                        long copiedBytes = 0;

                        int bytesRead;
                        while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                            copiedBytes += bytesRead;

                            // 現在のファイルの進捗を計算
                            progressInfo.CurrentFileProgress = (int)((copiedBytes * 100) / totalBytes);
                            
                            // 全体の進捗を計算
                            progressInfo.TotalProgress = (int)((processedFiles * 100 + progressInfo.CurrentFileProgress) / totalFiles);
                            
                            progress?.Report(progressInfo);
                        }
                    }

                    processedFiles++;
                    progressInfo.ProcessedFiles = processedFiles;
                    progress?.Report(progressInfo);
                }

                // サブディレクトリを非同期で再帰的にコピー
                foreach (string dirPath in Directory.GetDirectories(sourceDirPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string dirName = Path.GetFileName(dirPath);
                    string destDirPath = Path.Combine(destinationDirPath, dirName);
                    await CopyDirectoryWithProgressAsync(dirPath, destDirPath, progress, overwrite, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合、部分的にコピーされたファイルをクリーンアップ
                if (Directory.Exists(destinationDirPath))
                {
                    Directory.Delete(destinationDirPath, true);
                }
                throw; // キャンセル例外を再スロー
            }
        }

        /// <summary>
        /// トランザクション機能付きでディレクトリを非同期でコピーします
        /// </summary>
        /// <param name="sourceDirPath">コピー元ディレクトリのパス</param>
        /// <param name="destinationDirPath">コピー先ディレクトリのパス</param>
        /// <param name="progress">進捗状況を報告するためのIProgress</param>
        /// <param name="overwrite">既存のファイルを上書きするかどうか</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>コピー処理の完了を表すTask</returns>
        public static async Task CopyDirectoryWithTransactionAsync(
            string sourceDirPath,
            string destinationDirPath,
            IProgress<DirectoryCopyProgress>? progress = null,
            bool overwrite = false,
            CancellationToken cancellationToken = default)
        {
            var copiedFiles = new List<string>();
            var copiedDirs = new List<string>();

            try
            {
                if (!Directory.Exists(sourceDirPath))
                {
                    throw new DirectoryNotFoundException($"コピー元ディレクトリが見つかりません: {sourceDirPath}");
                }

                // 総ファイル数を計算
                var allFiles = Directory.GetFiles(sourceDirPath, "*", SearchOption.AllDirectories);
                var totalFiles = allFiles.Length;
                var processedFiles = 0;
                var progressInfo = new DirectoryCopyProgress
                {
                    TotalFiles = totalFiles,
                    ProcessedFiles = 0
                };

                // コピー先ディレクトリを作成
                if (!Directory.Exists(destinationDirPath))
                {
                    Directory.CreateDirectory(destinationDirPath);
                    copiedDirs.Add(destinationDirPath);
                }

                // ファイルを非同期でコピー
                foreach (var filePath in Directory.GetFiles(sourceDirPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string fileName = Path.GetFileName(filePath);
                    string destFilePath = Path.Combine(destinationDirPath, fileName);

                    progressInfo.CurrentFilePath = filePath;

                    using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (var destStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[81920];
                        long totalBytes = sourceStream.Length;
                        long copiedBytes = 0;

                        int bytesRead;
                        while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
                        {
                            await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                            copiedBytes += bytesRead;

                            progressInfo.CurrentFileProgress = (int)((copiedBytes * 100) / totalBytes);
                            progressInfo.TotalProgress = (int)((processedFiles * 100 + progressInfo.CurrentFileProgress) / totalFiles);
                            progress?.Report(progressInfo);
                        }
                    }

                    copiedFiles.Add(destFilePath);
                    processedFiles++;
                    progressInfo.ProcessedFiles = processedFiles;
                    progress?.Report(progressInfo);
                }

                // サブディレクトリを再帰的にコピー
                foreach (string dirPath in Directory.GetDirectories(sourceDirPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string dirName = Path.GetFileName(dirPath);
                    string destDirPath = Path.Combine(destinationDirPath, dirName);

                    await CopyDirectoryWithTransactionInternalAsync(
                        dirPath,
                        destDirPath,
                        copiedFiles,
                        copiedDirs,
                        progress,
                        overwrite,
                        cancellationToken);
                }
            }
            catch (Exception)
            {
                // エラーが発生した場合、コピーしたファイルとディレクトリをロールバック
                await RollbackAsync(copiedFiles, copiedDirs);
                throw;
            }
        }

        private static async Task CopyDirectoryWithTransactionInternalAsync(
            string sourceDirPath,
            string destinationDirPath,
            List<string> copiedFiles,
            List<string> copiedDirs,
            IProgress<DirectoryCopyProgress>? progress,
            bool overwrite,
            CancellationToken cancellationToken)
        {
            if (!Directory.Exists(destinationDirPath))
            {
                Directory.CreateDirectory(destinationDirPath);
                copiedDirs.Add(destinationDirPath);
            }

            foreach (string filePath in Directory.GetFiles(sourceDirPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destinationDirPath, fileName);

                using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var destStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write))
                {
                    await sourceStream.CopyToAsync(destStream, cancellationToken);
                }

                copiedFiles.Add(destFilePath);
            }

            foreach (string dirPath in Directory.GetDirectories(sourceDirPath))
            {
                string dirName = Path.GetFileName(dirPath);
                string destDirPath = Path.Combine(destinationDirPath, dirName);
                await CopyDirectoryWithTransactionInternalAsync(
                    dirPath,
                    destDirPath,
                    copiedFiles,
                    copiedDirs,
                    progress,
                    overwrite,
                    cancellationToken);
            }
        }

        private static async Task RollbackAsync(List<string> copiedFiles, List<string> copiedDirs)
        {
            // コピーしたファイルを削除
            foreach (var file in copiedFiles)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception)
                    {
                        // ロールバック中のエラーは無視
                    }
                }
            }

            // コピーしたディレクトリを削除（逆順で削除）
            for (int i = copiedDirs.Count - 1; i >= 0; i--)
            {
                var dir = copiedDirs[i];
                if (Directory.Exists(dir))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception)
                    {
                        // ロールバック中のエラーは無視
                    }
                }
            }

            await Task.CompletedTask; // 将来的な非同期操作のために用意
        }
    }
}
