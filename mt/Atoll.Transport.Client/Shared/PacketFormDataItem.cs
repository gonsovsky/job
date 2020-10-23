namespace Atoll.Transport.Contract
{

    public class PacketFormDataItem
    {
        public string ProviderKey { get; private set; }
        public string PacketId { get; private set; }
        public string FileKey { get; private set; }
        public int StartPosition { get; private set; }
        public int EndPosition { get; private set; }
        public bool IsFinal { get; private set; }

        public string PreviousPartStorageToken { get; private set; }
        public string PreviousPartId { get; private set; }
        //public string Hash { get; }
        // используется form-data и данные передаются как файл в form-data
        //public Stream Data { get; }

        public bool FillProperty(string key, string value)
        {
            var isSet = false;
            if (key == nameof(this.ProviderKey))
            {
                this.ProviderKey = value;
                isSet = true;
            }
            else if(key == nameof(this.PacketId))
            {
                this.PacketId = value;
                isSet = true;
            }
            else if(key == nameof(this.FileKey))
            {
                this.FileKey = value;
                isSet = true;
            }
            else if(key == nameof(this.StartPosition))
            {
                this.StartPosition = int.Parse(value);
                isSet = true;
            }
            else if(key == nameof(this.EndPosition))
            {
                this.EndPosition = int.Parse(value);
                isSet = true;
            }
            else if (key == nameof(this.IsFinal))
            {
                this.IsFinal = bool.Parse(value);
                isSet = true;
            }
            else if (key == nameof(this.PreviousPartStorageToken))
            {
                this.PreviousPartStorageToken = value;
                isSet = true;
            }
            else if (key == nameof(this.PreviousPartId))
            {
                this.PreviousPartId = value;
                isSet = true;
            }
            return isSet;
        }
    }
}
