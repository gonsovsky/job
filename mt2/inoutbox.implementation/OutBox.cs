using System;
using System.Collections;
using System.Data;
using System.IO;
using InOutBox.Contracts;
using System.Data.SQLite;

namespace InOutBox.Implementation
{
    public class OutBox: IOutBox, IOutBoxTransport
    {
        protected string StorageFolder;

        protected string DbFile;

        protected string ConStr;

        protected string Queue;

        protected SQLiteConnection SqlConn;

        public OutBox(string queue, string storageFolder, string conStr, string dbFile)
        {
            this.Queue = queue;
            this.StorageFolder = storageFolder;
            this.ConStr = conStr;
            this.DbFile = dbFile;

            if (File.Exists(DbFile))
                File.Delete(DbFile);
            SqlConn = new SQLiteConnection(ConStr);
            SqlConn.Open();

            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                    CREATE TABLE messages (
                       id INTEGER PRIMARY KEY AUTOINCREMENT,
                       queue TEXT NOT NULL,
                       topic TEXT NOT NULL,
                       commited real,
                       sent real,
                       delivered real
                    );
               ";
            cmd.ExecuteNonQuery();
        }

        public int Add(string topic)
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   Insert into messages (queue,topic) values  (@queue,@topic);
                   select last_insert_rowid();
               ";
            cmd.Parameters.Add("queue", DbType.String).Value = Queue;
            cmd.Parameters.Add("topic", DbType.String).Value = topic;
            var id = (int)(Int64)cmd.ExecuteScalar();
            return id;
        }

        private void SetFlag(int messageId, string flag)
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                $@"
                   Update messages set {flag} = julianday('now') where id =  @id
               ";
            cmd.Parameters.Add("id", DbType.UInt32).Value = messageId;
            cmd.ExecuteNonQuery();
        }

        public void Commit(int messageId)
        {
            SetFlag(messageId, "commited");
        }

        public void Rollback(int messageId)
        {
            var filename = MessageFile(messageId);
            if (File.Exists(filename))
                File.Delete(filename);

            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   delete from messages where id =  @id
               ";
            cmd.Parameters.Add("id", DbType.UInt32).Value = messageId;
            cmd.ExecuteNonQuery();
        }

        public string MessageFile(int messageId) =>
            Path.Combine(StorageFolder, messageId.ToString() + ".txt");

        public FileStream Write(int messageId)
        {
            var filename = MessageFile(messageId);
            if (File.Exists(filename))
                File.Delete(filename);
            return new FileStream(
                filename,
                FileMode.CreateNew
            );
        }

        public Stream Read(int messageId)
        {
            var filename = MessageFile(messageId);
            return new FileStream(
                filename,
                FileMode.Open
            );
        }

        public void Send(int messageId)
        {
            SetFlag(messageId, "sent");
        }

        public void Deliver(int messageId)
        {
            SetFlag(messageId, "delivered");
        }
    }
}
