using System;
using System.Collections.Generic;
using System.IO;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;

namespace Atoll.Transport.Client.Bundle
{
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

        private string GetConfigTempFilePath(ConfigurationPart configurationPart)
        {
            return Path.Combine(this.tempConfigsDir, configurationPart.ProviderKey + tempConfigExt);
        }

        private string GetConfigFilePath(ConfigurationPart configurationPart)
        {
            return Path.Combine(this.configsDir, configurationPart.ProviderKey + configExt);
        }

        public void Save(ConfigurationPart configurationPart)
        {
            var tempFilePath = this.GetConfigTempFilePath(configurationPart);
            var tempTokenPath = tempFilePath + tokenFileExt;
            using (var fileStream = File.OpenWrite(tempFilePath))
            {
                if (fileStream.Length > configurationPart.StartPosition)
                {
                    fileStream.SetLength(configurationPart.StartPosition);
                }

                fileStream.Seek(configurationPart.StartPosition, SeekOrigin.Begin);

                fileStream.Write(configurationPart.Stream, 0, configurationPart.Stream.Length);

                //configurationResponse.Stream.CopyTo(fileStream);
            }

            if (configurationPart.StartPosition == 0)
            {
                FileSystemHelper.WriteAllText(tempTokenPath, configurationPart.Token);
            }

            if (configurationPart.IsFinal)
            {
                var configPath = this.GetConfigFilePath(configurationPart);
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
                    if (subscriber.ProviderKey == configurationPart.ProviderKey)
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

        public IList<ConfigurationState> GetStateItems()
        {
            var results = new List<ConfigurationState>(4);
            var fullConfigsFilePaths = Directory.GetFiles(this.configsDir, "*" + configExt);
            foreach (var fullConfigsFilePath in fullConfigsFilePaths)
            {
                var tokenFilePath = fullConfigsFilePath + tokenFileExt;
                var token = File.Exists(tokenFilePath) 
                    ? FileSystemHelper.ReadAllText(tokenFilePath) 
                    : null;
                results.Add(new ConfigurationState
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

                results.Add(new ConfigurationState
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
