using System;
using System.Collections.Generic;
using System.IO;

namespace ClassLibrary1.Transport
{
    /// <summary>
    /// хранилище конфигураций агента
    /// </summary>
    public interface IConfigurationStoreService
    {
        IList<ConfigurationRequestDataItem> GetRequestItems();
        void Save(ConfigurationResponse configurationResponse);
        void Subscribe(IConfigurationUpdateSubscriber subscriber);
        void UnSubscribe(IConfigurationUpdateSubscriber subscriber);
    }

    public class ConfigurationStore : IConfigurationStoreService
    {
        private readonly string tempConfigsDir;
        private readonly string configsDir;
        private const string configExt = ".cnf";
        private const string tempConfigExt = configExt + ".tmp";
        private const string tokenFileExt = ".token";
        private IList<IConfigurationUpdateSubscriber> subscribers = new List<IConfigurationUpdateSubscriber>();

        public ConfigurationStore(string configsDir, string tempConfigsDir)
        {
            this.configsDir = configsDir;
            this.tempConfigsDir = tempConfigsDir;

            // по сути Directory.CreateDirectory должно успешно отрабатывать и без предвариательной проверки, но вроде в некоторых случаях бывают ошибки
            if (!Directory.Exists(this.configsDir))
            {
                Directory.CreateDirectory(this.configsDir);
            }
            if (!Directory.Exists(this.tempConfigsDir))
            {
                Directory.CreateDirectory(this.tempConfigsDir);
            }
        }

        private string GetConfigTempFilePath(ConfigurationResponse configurationResponse)
        {
            return Path.Combine(this.tempConfigsDir, configurationResponse.ProviderKey + tempConfigExt);
        }

        private string GetConfigFilePath(ConfigurationResponse configurationResponse)
        {
            return Path.Combine(this.configsDir, configurationResponse.ProviderKey + configExt);
        }

        public void Save(ConfigurationResponse configurationResponse)
        {
            var tempFilePath = this.GetConfigTempFilePath(configurationResponse);
            var tempTokenPath = tempFilePath + tokenFileExt;
            using (var fileStream = File.OpenWrite(tempFilePath))
            {
                if (fileStream.Length > configurationResponse.StartPosition)
                {
                    fileStream.SetLength(configurationResponse.StartPosition);
                }

                fileStream.Seek(configurationResponse.StartPosition, SeekOrigin.Begin);

                fileStream.Write(configurationResponse.Stream, 0, configurationResponse.Stream.Length);

                //configurationResponse.Stream.CopyTo(fileStream);
            }

            if (configurationResponse.StartPosition == 0)
            {
                FileSystemHelper.WriteAllText(tempTokenPath, configurationResponse.Token);
            }

            if (configurationResponse.IsFinal)
            {
                var configPath = this.GetConfigFilePath(configurationResponse);
                var configTokenPath = configPath + tokenFileExt;
                var bakFile = configPath + ".bak";
                var bakTokenFile = configTokenPath + ".bak";

                if (File.Exists(configPath))
                {
                    FileSystemHelper.DeleteFile(bakFile);
                    FileSystemHelper.MoveFile(configPath, bakFile);
                }

                FileSystemHelper.DeleteFile(configPath);
                FileSystemHelper.MoveFile(tempFilePath, configPath);
                FileSystemHelper.DeleteFile(tempFilePath);

                if (File.Exists(configTokenPath))
                {
                    FileSystemHelper.DeleteFile(bakTokenFile);
                    FileSystemHelper.MoveFile(configTokenPath, bakTokenFile);
                }

                FileSystemHelper.DeleteFile(configTokenPath);
                FileSystemHelper.MoveFile(tempTokenPath, configTokenPath);
                FileSystemHelper.DeleteFile(tempTokenPath);

                // нотификации
                foreach (var subscriber in subscribers)
                {
                    if (subscriber.ProviderKey == configurationResponse.ProviderKey)
                    {
                        try
                        {
                            subscriber.OnUpdate(() => File.OpenRead(configPath));
                        }
                        catch (Exception)
                        {
                            // TODO instrumentation
                            // ignore ?
                            //throw;
                        }
                    }        
                }
            }
            else
            {

            }
        }

        public IList<ConfigurationRequestDataItem> GetRequestItems()
        {
            var results = new List<ConfigurationRequestDataItem>(4);
            var fullConfigsFilePaths = Directory.GetFiles(this.configsDir, "*" + configExt);
            foreach (var fullConfigsFilePath in fullConfigsFilePaths)
            {
                var tokenFilePath = fullConfigsFilePath + tokenFileExt;
                var token = File.Exists(tokenFilePath) 
                    ? FileSystemHelper.ReadAllText(tokenFilePath) 
                    : null;
                results.Add(new ConfigurationRequestDataItem
                {
                    ProviderKey = Path.GetFileNameWithoutExtension(fullConfigsFilePath),
                    Token = token,
                    IsCompleted = true,
                });
            }

            var tempConfigsFilePaths = Directory.GetFiles(this.tempConfigsDir, "*" + tempConfigExt);
            foreach (var tempConfigsFilePath in tempConfigsFilePaths)
            {
                var tokenFilePath = tempConfigsFilePath + tokenFileExt;
                var token = File.Exists(tokenFilePath)
                    ? FileSystemHelper.ReadAllText(tokenFilePath)
                    : null;

                results.Add(new ConfigurationRequestDataItem
                {
                    ProviderKey = Path.GetFileNameWithoutExtension(tempConfigsFilePath),
                    Token = token,
                    StartPosition = new FileInfo(tempConfigsFilePath).Length,
                    IsCompleted = false,
                });
            }
            return results;
        }

        public void Subscribe(IConfigurationUpdateSubscriber subscriber)
        {
            this.subscribers.Add(subscriber);
        }

        public void UnSubscribe(IConfigurationUpdateSubscriber subscriber)
        {
            this.subscribers.Remove(subscriber);
        }
    }
}
