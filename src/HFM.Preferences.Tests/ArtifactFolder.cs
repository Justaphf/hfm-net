﻿
using System;
using System.IO;

namespace HFM.Preferences
{
    public class ArtifactFolder : IDisposable
    {
        public string Path { get; private set; }

        public ArtifactFolder()
           : this(Environment.CurrentDirectory)
        {

        }

        public ArtifactFolder(string basePath)
        {
            Path = System.IO.Path.Combine(basePath, System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            TryDeleteDirectory();
            GC.SuppressFinalize(this);
        }

        private void TryDeleteDirectory()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch (Exception)
            {

            }
        }
    }
}
