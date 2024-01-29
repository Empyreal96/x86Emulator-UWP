using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace x86Emulator
{
    public static class Helpers
    {
        public static bool DebugLog = false;
        public static bool DebugFile = false;
        private static bool fileInUse = false;
        private static StorageFile LogFile;

        public static async void Logger(Exception e, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (DebugFile)
            {
                try
                {
                    var message = $"Exception: {e.Message}\nMember: {memberName}\nFile: {sourceFilePath}\nLine: {sourceLineNumber}";
                    Logger(message);
                }
                catch (Exception ex)
                {

                }
            }
        }

        public static void LoggerDebug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (DebugLog && DebugFile)
            {
                Logger(message, memberName, sourceFilePath, sourceLineNumber);
            }
        }
        public static async void Logger(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (DebugFile)
            {
                try
                {
                    while (GettingFileInProgress)
                    {
                        await Task.Delay(200);
                    }
                    if (LogFile != null && message.Length > 0)
                    {
                        while (fileInUse)
                        {
                            await Task.Delay(200);
                        }
                        fileInUse = true;
                        try
                        {
                            await FileIO.AppendTextAsync(LogFile, $"\n\r-----------{DateTime.Now.ToString()}-----------\n\r{message}\nMember: {memberName}\nFile: {sourceFilePath}\nLine: {sourceLineNumber}\n\r------------------------------\n\r");
                            fileInUse = false;
                        }
                        catch (Exception e)
                        {
                            fileInUse = false;
                        }
                    }
                    else
                    {
                        fileInUse = false;
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        static bool GettingFileInProgress = false;
        public static async Task PrepareLogs(StorageFolder customFolder = null)
        {
            if (customFolder == null)
            {
                LogFile = await GetLogFile(ApplicationData.Current.LocalFolder);
            }
            else
            {
                LogFile = await GetLogFile(customFolder);
            }
        }
        private static async Task<StorageFile> GetLogFile(StorageFolder storageFolder)
        {
            GettingFileInProgress = true;
            StorageFile LogFile = null;
            try
            {
                if (storageFolder != null)
                {
                    var testFolder = await storageFolder.CreateFolderAsync("x86 Emulator (Logs)", CreationCollisionOption.OpenIfExists);

                    try
                    {
                        foreach (var fileItem in await testFolder.GetFilesAsync())
                        {
                            if ((await fileItem.GetBasicPropertiesAsync()).Size == 0)
                            {
                                await fileItem.DeleteAsync();
                            }
                        }
                    }
                    catch (Exception e)
                    {

                    }

                    var fileName = DateTime.Now.ToString().Replace("/", "_").Replace("\\", "_").Replace(":", "_").Replace(" ", "_");
                    var testFile = (StorageFile)await testFolder.TryGetItemAsync($"{fileName}.txt");
                    if (testFile != null)
                    {
                        await testFile.DeleteAsync();
                    }
                    LogFile = await testFolder.CreateFileAsync($"{fileName}.txt");
                }
            }
            catch (Exception e)
            {

            }
            GettingFileInProgress = false;
            return LogFile;
        }

        public static async Task<string> CRCFile(StorageFile file)
        {
            var crc = "";
            try
            {
                var checksum = new CRC32();
                using (var stream = await file.OpenReadAsync())
                {
                    crc = checksum.ComputeHash(stream.AsStream());
                }
            }
            catch (Exception e)
            {
                Logger(e);
            }

            return crc;
        }
    }

    public class CRC32
    {
        private readonly uint[] ChecksumTable;
        private readonly uint Polynomial = 0xEDB88320;

        public CRC32()
        {
            ChecksumTable = new uint[0x100];

            for (uint index = 0; index < 0x100; ++index)
            {
                uint item = index;
                for (int bit = 0; bit < 8; ++bit)
                    item = ((item & 1) != 0) ? (Polynomial ^ (item >> 1)) : (item >> 1);
                ChecksumTable[index] = item;
            }
        }

        public string ComputeHash(Stream stream)
        {
            uint result = 0xFFFFFFFF;

            int current;
            while ((current = stream.ReadByte()) != -1)
                result = ChecksumTable[(result & 0xFF) ^ (byte)current] ^ (result >> 8);

            byte[] hash = BitConverter.GetBytes(~result);
            Array.Reverse(hash);

            String hashString = String.Empty;

            foreach (byte b in hash) hashString += b.ToString("x2").ToLower();

            return hashString;
        }
    }
}
