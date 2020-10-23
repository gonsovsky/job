namespace ClassLibrary1.Transport
{
    public class ConfigurationRequestDataItem
    {
        //public long PacketId { get; set; }
        public string ProviderKey { get; set; }
        public string Token { get; set; }
        public long? StartPosition { get; set; }
        public bool IsCompleted { get; set; }

        public bool FillProperty(string key, string value)
        {
            bool isSet = false;
            if (key == nameof(this.ProviderKey))
            {
                this.ProviderKey = value;
                isSet = true;
            }
            else if (key == nameof(this.Token))
            {
                this.Token = value;
                isSet = true;
            }
            //if (key == nameof(this.PacketId))
            //{
            //    this.PacketId = long.Parse(value);
            //}
            else if(key == nameof(this.StartPosition))
            {
                this.StartPosition = long.Parse(value);
                isSet = true;
            }
            else if(key == nameof(this.IsCompleted))
            {
                this.IsCompleted = bool.Parse(value);
                isSet = true;
            }
            return isSet;
        }
    }
}
