namespace FileManagerEx.UnitTest
{
    public class FileManagerExTests : IDisposable
    {
        private readonly string _testRootPath;
        private readonly string _sourcePath;
        private readonly string _destinationPath;

        public FileManagerExTests()
        {
            // テスト用のディレクトリパスを設定
            _testRootPath = Path.Combine(Path.GetTempPath(), "FileManagerExTests");
            _sourcePath = Path.Combine(_testRootPath, "source");
            _destinationPath = Path.Combine(_testRootPath, "destination");

            // テスト用ディレクトリを作成
            Directory.CreateDirectory(_sourcePath);
        }

        [Fact]
        public void CopyDirectory_基本的なファイルコピー_成功()
        {
            // 準備
            var testFilePath = Path.Combine(_sourcePath, "test.txt");
            File.WriteAllText(testFilePath, "テストコンテンツ");

            // 実行
            FileManagerEx.CopyDirectory(_sourcePath, _destinationPath);

            // 検証
            var copiedFilePath = Path.Combine(_destinationPath, "test.txt");
            Assert.True(File.Exists(copiedFilePath));
            Assert.Equal("テストコンテンツ", File.ReadAllText(copiedFilePath));
        }

        [Fact]
        public void CopyDirectory_サブディレクトリを含むコピー_成功()
        {
            // 準備
            var subDir = Path.Combine(_sourcePath, "subdir");
            Directory.CreateDirectory(subDir);
            var testFilePath = Path.Combine(subDir, "test.txt");
            File.WriteAllText(testFilePath, "サブディレクトリのテスト");

            // 実行
            FileManagerEx.CopyDirectory(_sourcePath, _destinationPath);

            // 検証
            var copiedFilePath = Path.Combine(_destinationPath, "subdir", "test.txt");
            Assert.True(Directory.Exists(Path.Combine(_destinationPath, "subdir")));
            Assert.True(File.Exists(copiedFilePath));
            Assert.Equal("サブディレクトリのテスト", File.ReadAllText(copiedFilePath));
        }

        [Fact]
        public void CopyDirectory_存在しないソースディレクトリ_例外をスロー()
        {
            // 準備
            var nonExistentPath = Path.Combine(_testRootPath, "non_existent");

            // 実行と検証
            var exception = Assert.Throws<DirectoryNotFoundException>(() =>
                FileManagerEx.CopyDirectory(nonExistentPath, _destinationPath));

            Assert.Contains("コピー元ディレクトリが見つかりません", exception.Message);
        }

        [Fact]
        public void CopyDirectory_上書きオプション_成功()
        {
            // 準備
            var testFilePath = Path.Combine(_sourcePath, "test.txt");
            File.WriteAllText(testFilePath, "元のコンテンツ");
            
            // 最初のコピー
            FileManagerEx.CopyDirectory(_sourcePath, _destinationPath);
            
            // ソースファイルの内容を変更
            File.WriteAllText(testFilePath, "新しいコンテンツ");

            // 実行（上書きオプションあり）
            FileManagerEx.CopyDirectory(_sourcePath, _destinationPath, true);

            // 検証
            var copiedFilePath = Path.Combine(_destinationPath, "test.txt");
            Assert.Equal("新しいコンテンツ", File.ReadAllText(copiedFilePath));
        }

        [Fact]
        public async Task CopyDirectoryAsync_基本的なファイルコピー_成功()
        {
            // 準備
            var testFilePath = Path.Combine(_sourcePath, "test.txt");
            await File.WriteAllTextAsync(testFilePath, "テストコンテンツ");

            // 実行
            await FileManagerEx.CopyDirectoryAsync(_sourcePath, _destinationPath);

            // 検証
            var copiedFilePath = Path.Combine(_destinationPath, "test.txt");
            Assert.True(File.Exists(copiedFilePath));
            Assert.Equal("テストコンテンツ", await File.ReadAllTextAsync(copiedFilePath));
        }

        [Fact]
        public async Task CopyDirectoryAsync_サブディレクトリを含むコピー_成功()
        {
            // 準備
            var subDir = Path.Combine(_sourcePath, "subdir");
            Directory.CreateDirectory(subDir);
            var testFilePath = Path.Combine(subDir, "test.txt");
            await File.WriteAllTextAsync(testFilePath, "サブディレクトリのテスト");

            // 実行
            await FileManagerEx.CopyDirectoryAsync(_sourcePath, _destinationPath);

            // 検証
            var copiedFilePath = Path.Combine(_destinationPath, "subdir", "test.txt");
            Assert.True(Directory.Exists(Path.Combine(_destinationPath, "subdir")));
            Assert.True(File.Exists(copiedFilePath));
            Assert.Equal("サブディレクトリのテスト", await File.ReadAllTextAsync(copiedFilePath));
        }

        [Fact]
        public async Task CopyDirectoryAsync_存在しないソースディレクトリ_例外をスロー()
        {
            // 準備
            var nonExistentPath = Path.Combine(_testRootPath, "non_existent");

            // 実行と検証
            var exception = await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
                await FileManagerEx.CopyDirectoryAsync(nonExistentPath, _destinationPath));

            Assert.Contains("コピー元ディレクトリが見つかりません", exception.Message);
        }

        [Fact]
        public async Task CopyDirectoryAsync_大きなファイルのコピー_成功()
        {
            // 準備
            var testFilePath = Path.Combine(_sourcePath, "large_file.dat");
            var buffer = new byte[1024 * 1024]; // 1MB
            using (var stream = File.Create(testFilePath))
            {
                for (int i = 0; i < 5; i++) // 5MBのファイル作成
                {
                    await stream.WriteAsync(buffer);
                }
            }

            // 実行
            await FileManagerEx.CopyDirectoryAsync(_sourcePath, _destinationPath);

            // 検証
            var copiedFilePath = Path.Combine(_destinationPath, "large_file.dat");
            Assert.True(File.Exists(copiedFilePath));
            Assert.Equal(new FileInfo(testFilePath).Length, new FileInfo(copiedFilePath).Length);
        }

        [Fact]
        public async Task CopyDirectoryAsync_複数ファイルの並列コピー_成功()
        {
            // 準備
            for (int i = 0; i < 10; i++)
            {
                var testFilePath = Path.Combine(_sourcePath, $"file_{i}.txt");
                await File.WriteAllTextAsync(testFilePath, $"コンテンツ {i}");
            }

            // 実行
            await FileManagerEx.CopyDirectoryAsync(_sourcePath, _destinationPath);

            // 検証
            for (int i = 0; i < 10; i++)
            {
                var copiedFilePath = Path.Combine(_destinationPath, $"file_{i}.txt");
                Assert.True(File.Exists(copiedFilePath));
                Assert.Equal($"コンテンツ {i}", await File.ReadAllTextAsync(copiedFilePath));
            }
        }

        [Fact]
        public async Task CopyDirectoryWithProgressAsync_キャンセル_処理が中断される()
        {
            // 準備
            var cts = new CancellationTokenSource();
            var progress = new Progress<DirectoryCopyProgress>();
            var progressList = new List<DirectoryCopyProgress>();

            progress.ProgressChanged += (s, e) =>
            {
                progressList.Add(new DirectoryCopyProgress
                {
                    CurrentFilePath = e.CurrentFilePath,
                    CurrentFileProgress = e.CurrentFileProgress,
                    TotalProgress = e.TotalProgress,
                    ProcessedFiles = e.ProcessedFiles,
                    TotalFiles = e.TotalFiles
                });

                // 進捗が30%を超えたらキャンセル
                if (e.TotalProgress > 30)
                {
                    cts.Cancel();
                }
            };

            // 大きなテストファイルを作成
            var testFilePath = Path.Combine(_sourcePath, "large_file.dat");
            var buffer = new byte[1024 * 1024]; // 1MB
            using (var stream = File.Create(testFilePath))
            {
                for (int i = 0; i < 10; i++) // 10MBのファイル作成
                {
                    await stream.WriteAsync(buffer);
                }
            }

            // 実行と検証
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await FileManagerEx.CopyDirectoryWithProgressAsync(
                    _sourcePath,
                    _destinationPath,
                    progress,
                    false,  // overwrite parameter
                    cts.Token));

            // コピー先ディレクトリが削除されていることを確認
            Assert.False(Directory.Exists(_destinationPath));
            
            // 進捗が報告されていることを確認
            Assert.NotEmpty(progressList);
            Assert.Contains(progressList, p => p.TotalProgress > 0);
        }

        [Fact]
        public async Task CopyDirectoryWithProgressAsync_即時キャンセル_例外をスロー()
        {
            // 準備
            var cts = new CancellationTokenSource();
            cts.Cancel(); // 即時キャンセル

            // 実行と検証
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await FileManagerEx.CopyDirectoryWithProgressAsync(
                    _sourcePath,
                    _destinationPath,
                    null,
                    false,  // overwrite parameter
                    cts.Token));

            // コピー先ディレクトリが作成されていないことを確認
            Assert.False(Directory.Exists(_destinationPath));
        }

        [Fact]
        public async Task CopyDirectoryWithTransactionAsync_基本的なファイルコピー_成功()
        {
            // 準備
            var testFilePath = Path.Combine(_sourcePath, "test.txt");
            await File.WriteAllTextAsync(testFilePath, "テストコンテンツ");

            // 実行
            await FileManagerEx.CopyDirectoryWithTransactionAsync(_sourcePath, _destinationPath);

            // 検証
            var copiedFilePath = Path.Combine(_destinationPath, "test.txt");
            Assert.True(File.Exists(copiedFilePath));
            Assert.Equal("テストコンテンツ", await File.ReadAllTextAsync(copiedFilePath));
        }

        [Fact]
        public async Task CopyDirectoryWithTransactionAsync_エラー発生時にロールバック()
        {
            // 準備
            var testFilePath1 = Path.Combine(_sourcePath, "file1.txt");
            var testFilePath2 = Path.Combine(_sourcePath, "file2.txt");
            await File.WriteAllTextAsync(testFilePath1, "ファイル1");
            await File.WriteAllTextAsync(testFilePath2, "ファイル2");

            // コピー先のディレクトリを作成し、ロックされたファイルを作成
            Directory.CreateDirectory(_destinationPath);
            var destFile1 = Path.Combine(_destinationPath, "file1.txt");
            File.Copy(testFilePath1, destFile1);

            using (var stream = File.Open(destFile1, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // 実行（エラーが発生することを期待）
                var exception = await Assert.ThrowsAsync<IOException>(async () =>
                    await FileManagerEx.CopyDirectoryWithTransactionAsync(_sourcePath, _destinationPath));

                // 検証：部分的にコピーされたファイルが存在しないことを確認
                var destFile2 = Path.Combine(_destinationPath, "file2.txt");
                Assert.True(File.Exists(destFile1)); // ロックされているファイルは残っている
                Assert.False(File.Exists(destFile2)); // 他のファイルはロールバックされている
            }
        }

        [Fact]
        public async Task CopyDirectoryWithTransactionAsync_サブディレクトリを含むコピーのロールバック()
        {
            // 準備
            var subDir = Path.Combine(_sourcePath, "subdir");
            Directory.CreateDirectory(subDir);
            await File.WriteAllTextAsync(Path.Combine(_sourcePath, "root.txt"), "ルートファイル");
            await File.WriteAllTextAsync(Path.Combine(subDir, "sub.txt"), "サブファイル");

            // コピー先のディレクトリを作成し、ロックされたファイルを作成
            Directory.CreateDirectory(_destinationPath);
            var destRootFile = Path.Combine(_destinationPath, "root.txt");
            File.WriteAllText(destRootFile, "既存ファイル");

            using (var stream = File.Open(destRootFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // 実行（エラーが発生することを期待）
                var exception = await Assert.ThrowsAsync<IOException>(async () =>
                    await FileManagerEx.CopyDirectoryWithTransactionAsync(_sourcePath, _destinationPath));

                // 検証：部分的にコピーされたファイルとディレクトリが存在しないことを確認
                Assert.True(File.Exists(destRootFile)); // ロックされているファイルは残っている
                Assert.False(Directory.Exists(Path.Combine(_destinationPath, "subdir"))); // サブディレクトリはロールバックされている
            }
        }

        [Fact]
        public async Task CopyDirectoryWithTransactionAsync_進捗報告_成功()
        {
            // 準備
            var progressList = new List<DirectoryCopyProgress>();
            var progress = new Progress<DirectoryCopyProgress>(p =>
            {
                progressList.Add(new DirectoryCopyProgress
                {
                    CurrentFilePath = p.CurrentFilePath,
                    CurrentFileProgress = p.CurrentFileProgress,
                    TotalProgress = p.TotalProgress,
                    ProcessedFiles = p.ProcessedFiles,
                    TotalFiles = p.TotalFiles
                });
            });

            // テストファイルを作成
            var testFilePath = Path.Combine(_sourcePath, "test.txt");
            var buffer = new byte[1024 * 1024]; // 1MB
            using (var stream = File.Create(testFilePath))
            {
                await stream.WriteAsync(buffer);
            }

            // 実行
            await FileManagerEx.CopyDirectoryWithTransactionAsync(_sourcePath, _destinationPath, progress);

            // 検証
            Assert.NotEmpty(progressList);
            Assert.Contains(progressList, p => p.TotalProgress == 100);
            Assert.Contains(progressList, p => p.ProcessedFiles == 1);
            Assert.Contains(progressList, p => p.TotalFiles == 1);
        }

        public void Dispose()
        {
            // テスト用ディレクトリのクリーンアップ
            if (Directory.Exists(_testRootPath))
            {
                Directory.Delete(_testRootPath, true);
            }
            GC.SuppressFinalize(this);
        }
    }
}